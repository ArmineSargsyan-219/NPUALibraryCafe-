using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using NPUALibraryCafe.Models;

namespace NPUALibraryCafe.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;
        private readonly IConfiguration _configuration;

        // Temporary in-memory store for verification codes
        // Key: email, Value: (code, expiry)
        private static readonly Dictionary<string, (string Code, DateTime Expiry, RegisterDto Data)> _pendingRegistrations = new();

        public AuthController(LibraryCafeDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Auth/send-code
        [HttpPost("send-code")]
        public async Task<ActionResult> SendVerificationCode([FromBody] RegisterDto dto)
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (existingUser != null)
                    return BadRequest(new { error = "User with this email already exists" });

                // Generate 6-digit code
                var code = new Random().Next(100000, 999999).ToString();
                var expiry = DateTime.UtcNow.AddMinutes(10);

                // Store pending registration
                _pendingRegistrations[dto.Email] = (code, expiry, dto);

                // Send email
                await SendEmailAsync(dto.Email, dto.Name, code);

                return Ok(new { message = "Verification code sent to your email" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to send code", details = ex.Message });
            }
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] VerifyDto dto)
        {
            try
            {
                if (!_pendingRegistrations.TryGetValue(dto.Email, out var pending))
                    return BadRequest(new { error = "No pending registration for this email. Please request a new code." });

                if (DateTime.UtcNow > pending.Expiry)
                {
                    _pendingRegistrations.Remove(dto.Email);
                    return BadRequest(new { error = "Code has expired. Please request a new one." });
                }

                if (pending.Code != dto.Code)
                    return BadRequest(new { error = "Invalid verification code" });

                // Code is correct - create the user
                _pendingRegistrations.Remove(dto.Email);

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingUser != null)
                    return BadRequest(new { error = "User with this email already exists" });

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(pending.Data.Password);

                var user = new User
                {
                    Fullname = pending.Data.Name,
                    Email = pending.Data.Email,
                    Passwordhash = hashedPassword,
                    Role = pending.Data.Role
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    message = "Registration successful",
                    token = token,
                    user = new
                    {
                        id = user.Userid,
                        name = user.Fullname,
                        email = user.Email,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Registration failed", details = ex.Message });
            }
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (user == null)
                    return Unauthorized(new { error = "Invalid email or password" });

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Passwordhash))
                    return Unauthorized(new { error = "Invalid email or password" });

                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    message = "Login successful",
                    token = token,
                    user = new
                    {
                        id = user.Userid,
                        name = user.Fullname,
                        email = user.Email,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Login failed", details = ex.Message });
            }
        }

        // GET: api/Auth/profile
        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult> GetProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                    return NotFound(new { error = "User not found" });

                return Ok(new
                {
                    id = user.Userid,
                    name = user.Fullname,
                    email = user.Email,
                    role = user.Role
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get profile", details = ex.Message });
            }
        }

        private async Task SendEmailAsync(string toEmail, string toName, string code)
        {
            var fromEmail = "arminesargsyan699@gmail.com";
            var appPassword = "qufeuwxnhibxqcie";

            var smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail, appPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, "NPUA Library Café"),
                Subject = "Your verification code",
                IsBodyHtml = true,
                Body = $@"
                <div style='font-family:sans-serif;max-width:480px;margin:0 auto;padding:2rem;'>
                    <h2 style='color:#3d5c3a;'>NPUA Library Café</h2>
                    <p>Hi {toName},</p>
                    <p>Your verification code is:</p>
                    <div style='font-size:2.5rem;font-weight:bold;letter-spacing:0.5rem;color:#141414;
                                background:#f5f0e8;padding:1.5rem;text-align:center;border-radius:8px;margin:1.5rem 0;'>
                        {code}
                    </div>
                    <p style='color:#6b6560;font-size:0.9rem;'>This code expires in 10 minutes.</p>
                </div>"
            };

            mail.To.Add(new MailAddress(toEmail, toName));
            await smtp.SendMailAsync(mail);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Userid.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name, user.Fullname)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(int.Parse(jwtSettings["ExpiryInDays"] ?? "7")),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RegisterDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "student";
    }

    public class VerifyDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}