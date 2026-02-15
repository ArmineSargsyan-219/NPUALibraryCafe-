using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
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

        public AuthController(LibraryCafeDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (existingUser != null)
                {
                    return BadRequest(new { error = "User with this email already exists" });
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var user = new User
                {
                    Fullname = dto.Name,
                    Email = dto.Email,
                    Passwordhash = hashedPassword,
                    Role = dto.Role
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
                {
                    return Unauthorized(new { error = "Invalid email or password" });
                }

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Passwordhash))
                {
                    return Unauthorized(new { error = "Invalid email or password" });
                }

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
                {
                    return NotFound(new { error = "User not found" });
                }

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

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}