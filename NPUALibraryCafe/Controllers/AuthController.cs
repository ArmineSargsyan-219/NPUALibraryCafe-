using LibCafe.Domain.Entities;
using LibCafe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace NPUALibraryCafe.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;
        private readonly IConfiguration _configuration;

        // In-memory store: email -> (code, expiry, pendingUserData)
        private static readonly ConcurrentDictionary<string, PendingRegistration> _pendingRegistrations = new();

        public AuthController(LibraryCafeDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Auth/send-code
        [HttpPost("send-code")]
        public async Task<ActionResult> SendVerificationCode([FromBody] SendCodeDto dto)
        {
            try
            {
                // Validate polytechnic email
                if (!dto.Email.EndsWith("@polytechnic.am", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { error = "Գրանցվելու համար անհրաժեշտ է @polytechnic.am էլ. հասցե" });

                // Check if email already registered
                var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existing != null)
                    return BadRequest(new { error = "Այս էլ. հասցեն արդեն գրանցված է" });

                // Generate 6-digit code
                var code = new Random().Next(100000, 999999).ToString();
                var expiry = DateTime.UtcNow.AddMinutes(10);

                // Store pending registration
                _pendingRegistrations[dto.Email] = new PendingRegistration
                {
                    Code = code,
                    Expiry = expiry,
                    Name = dto.Name,
                    Password = dto.Password,
                    Role = dto.Role ?? "user"
                };

                // Send email via Gmail SMTP
                await SendEmailAsync(dto.Email, dto.Name, code);

                return Ok(new { message = "Հաստատման կոդ ուղարկվեց" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Կոդ ուղարկելը ձախողվեց", details = ex.Message });
            }
        }

        // POST: api/Auth/register  (now expects email + code)
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                // Check pending registration exists
                if (!_pendingRegistrations.TryGetValue(dto.Email, out var pending))
                    return BadRequest(new { error = "Նախ ուղարկեք հաստատման կոդ" });

                // Check code expiry
                if (DateTime.UtcNow > pending.Expiry)
                {
                    _pendingRegistrations.TryRemove(dto.Email, out _);
                    return BadRequest(new { error = "Կոդի ժամկետը լրացել է: Կրկին ուղարկեք" });
                }

                // Verify code
                if (pending.Code != dto.Code)
                    return BadRequest(new { error = "Սխալ կոդ: Կրկին փորձեք" });

                // Double-check email not already taken
                var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existing != null)
                    return BadRequest(new { error = "Այս էլ. հասցեն արդեն գրանցված է" });

                // Create user
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(pending.Password);
                var user = new User
                {
                    Fullname = pending.Name,
                    Email = dto.Email,
                    Passwordhash = hashedPassword,
                    Role = pending.Role
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Clean up pending
                _pendingRegistrations.TryRemove(dto.Email, out _);

                var token = GenerateJwtToken(user);
                return Ok(new
                {
                    message = "Գրանցումը հաջողվեց",
                    token,
                    user = new { id = user.Userid, name = user.Fullname, email = user.Email, role = user.Role }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Գրանցումը ձախողվեց", details = ex.Message });
            }
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Passwordhash))
                    return Unauthorized(new { error = "Սխալ էլ. հասցե կամ գաղտնաբառ" });

                var token = GenerateJwtToken(user);
                return Ok(new
                {
                    message = "Մուտքը հաջողվեց",
                    token,
                    user = new { id = user.Userid, name = user.Fullname, email = user.Email, role = user.Role }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Մուտքը ձախողվեց", details = ex.Message });
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
                if (user == null) return NotFound(new { error = "Օգտատերը չի գտնվել" });

                var phones = await _context.Database
                    .SqlQueryRaw<PhoneRow>("SELECT phone FROM users WHERE id = {0}", userId)
                    .ToListAsync();
                var phone = phones.FirstOrDefault()?.Phone;

                return Ok(new { id = user.Userid, name = user.Fullname, email = user.Email, role = user.Role, phone });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Չհաջողվեց ստանալ պրոֆիլը", details = ex.Message });
            }
        }

        // PUT: api/Auth/profile
        [Authorize]
        [HttpPut("profile")]
        public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (!string.IsNullOrEmpty(dto.Name))
                    await _context.Database.ExecuteSqlRawAsync("UPDATE users SET name = {0} WHERE id = {1}", dto.Name, userId);
                if (dto.Phone != null)
                    await _context.Database.ExecuteSqlRawAsync("UPDATE users SET phone = {0} WHERE id = {1}", dto.Phone, userId);
                return Ok(new { message = "Պրոֆիլը թարմացված է" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Չհաջողվեց թարմացնել պրոֆիլը", details = ex.Message });
            }
        }

        // ===== HELPERS =====

        private async Task SendEmailAsync(string toEmail, string toName, string code)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var fromEmail = smtpSettings["FromEmail"] ?? "arminesargsyan699@gmail.com";
            var appPassword = smtpSettings["AppPassword"] ?? "nblizeeiuptfltow";
            var fromName = smtpSettings["FromName"] ?? "ՀԱՊՀ Գրադարան-Սրճարան";

            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(fromEmail, appPassword)
            };

            var body = $@"
<div style=""font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:2rem;border:1px solid #e0dbd4;border-radius:12px;"">
  <h2 style=""font-size:1.4rem;color:#141414;margin-bottom:0.5rem;"">ՀԱՊՀ Գրադարան-Սրճարան</h2>
  <p style=""color:#6b6560;margin-bottom:1.5rem;"">Բարև, {toName}!</p>
  <p style=""color:#2a2a2a;"">Ձեր հաստատման կոդն է.</p>
  <div style=""font-size:2.5rem;font-weight:700;letter-spacing:0.5rem;text-align:center;padding:1.5rem;background:#f5f0e8;border-radius:8px;margin:1.5rem 0;color:#141414;"">
    {code}
  </div>
  <p style=""color:#6b6560;font-size:0.85rem;"">Կոդը վավեր է 10 րոպե: Եթե Դուք չեք հայտ ներկայացրել, անտեսեք այս նամակը։</p>
</div>";

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = $"Ձեր հաստատման կոդը՝ {code}",
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(message);
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

    // ===== DTOs =====
    public class SendCodeDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    public class RegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class PendingRegistration
    {
        public string Code { get; set; } = string.Empty;
        public DateTime Expiry { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "user";
    }

    public class PhoneRow { public string? Phone { get; set; } }
    public class UpdateProfileDto { public string? Name { get; set; } public string? Phone { get; set; } }
}