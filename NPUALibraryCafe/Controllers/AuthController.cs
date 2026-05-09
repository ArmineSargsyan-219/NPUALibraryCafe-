using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NPUALibraryCafe.DTOs.Auth;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace NPUALibraryCafe.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    private static readonly ConcurrentDictionary<string, PendingRegistration> _pendingRegistrations = new();

    private static readonly ConcurrentDictionary<string, PasswordResetRequest> _passwordResets = new();
    private record PasswordResetRequest(string Code, DateTime Expiry);
    public AuthController(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    [HttpPost("send-code")]
    public async Task<IActionResult> SendVerificationCode([FromBody] SendCodeDto dto)
    {
        if (!dto.Email.EndsWith("@polytechnic.am", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Գրանցվելու համար անհրաժեշտ է @polytechnic.am էլ. հասցե" });


        var (isValid, pwError) = ValidatePassword(dto.Password);
        if (!isValid) return BadRequest(new { error = pwError });

        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
            return BadRequest(new { error = "Այս էլ. հասցեն արդեն գրանցված է" });

        var code = new Random().Next(100000, 999999).ToString();

        _pendingRegistrations[dto.Email] = new PendingRegistration
        {
            Code = code,
            Expiry = DateTime.UtcNow.AddMinutes(10),
            Name = dto.Name,
            Password = dto.Password,
            Role = dto.Role ?? "user"
        };

        await SendEmailAsync(dto.Email, dto.Name, code);
        return Ok(new { message = "Հաստատման կոդ ուղարկվեց" });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!_pendingRegistrations.TryGetValue(dto.Email, out var pending))
            return BadRequest(new { error = "Նախ ուղարկեք հաստատման կոդ" });

        if (DateTime.UtcNow > pending.Expiry)
        {
            _pendingRegistrations.TryRemove(dto.Email, out _);
            return BadRequest(new { error = "Կոդի ժամկետը լրացել է: Կրկին ուղարկեք" });
        }

        if (pending.Code != dto.Code)
            return BadRequest(new { error = "Սխալ կոդ: Կրկին փորձեք" });

        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
            return BadRequest(new { error = "Այս էլ. հասցեն արդեն գրանցված է" });

        var user = new User
        {
            Fullname = pending.Name,
            Email = dto.Email,
            Passwordhash = BCrypt.Net.BCrypt.HashPassword(pending.Password),
            Role = pending.Role
        };

        await _userRepository.AddAsync(user);
        _pendingRegistrations.TryRemove(dto.Email, out _);

        return Ok(new
        {
            message = "Գրանցումը հաջողվեց",
            token = GenerateJwtToken(user),
            user = new UserResponseDto
            {
                Id = user.Userid,
                Name = user.Fullname,
                Email = user.Email,
                Role = user.Role
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Passwordhash))
            return Unauthorized(new { error = "Սխալ էլ. հասցե կամ գաղտնաբառ" });

        return Ok(new
        {
            message = "Մուտքը հաջողվեց",
            token = GenerateJwtToken(user),
            user = new UserResponseDto
            {
                Id = user.Userid,
                Name = user.Fullname,
                Email = user.Email,
                Role = user.Role
            }
        });
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
            return Ok(new { message = "Եթե հասցեն գոյություն ունի, կոդ կուղարկվի" }); // don't reveal if email exists

        var code = new Random().Next(100000, 999999).ToString();
        _passwordResets[dto.Email] = new PasswordResetRequest(code, DateTime.UtcNow.AddMinutes(10));

        await SendPasswordResetEmailAsync(dto.Email, user.Fullname, code);
        return Ok(new { message = "Կոդ ուղարկվեց" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!_passwordResets.TryGetValue(dto.Email, out var reset))
            return BadRequest(new { error = "Նախ հարցրեք վերականգնման կոդ" });

        if (DateTime.UtcNow > reset.Expiry)
        {
            _passwordResets.TryRemove(dto.Email, out _);
            return BadRequest(new { error = "Կոդի ժամկետը լրացել է: Կրկին փորձեք" });
        }

        if (reset.Code != dto.Code)
            return BadRequest(new { error = "Սխալ կոդ" });

        var (isValid, pwError) = ValidatePassword(dto.NewPassword);
        if (!isValid) return BadRequest(new { error = pwError });

        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null) return NotFound();

        await _userRepository.UpdatePasswordAsync(user.Userid, BCrypt.Net.BCrypt.HashPassword(dto.NewPassword));
        _passwordResets.TryRemove(dto.Email, out _);

        return Ok(new { message = "Գաղտնաբառը հաջողությամբ փոխվեց" });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound(new { error = "Օգտատերը չի գտնվել" });

        var phone = await _userRepository.GetPhoneAsync(userId);

        return Ok(new UserResponseDto
        {
            Id = user.Userid,
            Name = user.Fullname,
            Email = user.Email,
            Role = user.Role,
            Phone = phone
        });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (!string.IsNullOrEmpty(dto.Name))
            await _userRepository.UpdateNameAsync(userId, dto.Name);

        if (dto.Phone != null)
            await _userRepository.UpdatePhoneAsync(userId, dto.Phone);

        return Ok(new { message = "Պրոֆիլը թարմացված է" });
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
            new Claim("userId", user.Userid.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("role", user.Role),
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

    private async Task SendEmailAsync(string toEmail, string toName, string code)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var fromEmail = smtpSettings["FromEmail"]!;
        var appPassword = smtpSettings["AppPassword"]!;
        var fromName = smtpSettings["FromName"] ?? "ՀԱՊՀ Գրադարան-Սրճարան";

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(fromEmail, appPassword)
        };

        var body = $@"
<div style=""font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:2rem;border:1px solid #e0dbd4;border-radius:12px;"">
  <h2 style=""color:#141414;"">ՀԱՊՀ Գրադարան-Սրճարան</h2>
  <p style=""color:#6b6560;"">Բարև, {toName}!</p>
  <p>Ձեր հաստատման կոդն է.</p>
  <div style=""font-size:2.5rem;font-weight:700;letter-spacing:0.5rem;text-align:center;padding:1.5rem;background:#f5f0e8;border-radius:8px;margin:1.5rem 0;color:#141414;"">
    {code}
  </div>
  <p style=""color:#6b6560;font-size:0.85rem;"">Կոդը վավեր է 10 րոպե:</p>
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

    private (bool IsValid, string Error) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Գաղտնաբառը չի կարող դատարկ լինել");
        if (password.Length < 8)
            return (false, "Գաղտնաբառը պետք է լինի առնվազն 8 նիշ");
        if (!password.Any(char.IsLetter))
            return (false, "Գաղտնաբառը պետք է պարունակի առնվազն 1 տառ");
        if (!password.Any(char.IsDigit))
            return (false, "Գաղտնաբառը պետք է պարունակի առնվազն 1 թիվ");
        if (!password.Any(c => "!@#$%^&*".Contains(c)))
            return (false, "Գաղտնաբառը պետք է պարունակի առնվազն 1 հատուկ նիշ (!@#$%^&*)");
        return (true, "");
    }

    private async Task SendPasswordResetEmailAsync(string toEmail, string toName, string code)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var fromEmail = smtpSettings["FromEmail"]!;
        var appPassword = smtpSettings["AppPassword"]!;
        var fromName = smtpSettings["FromName"] ?? "ՀԱՊՀ Գրադարան-Սրճարան";

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(fromEmail, appPassword)
        };

        var body = $@"
<div style=""font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:2rem;border:1px solid #e0dbd4;border-radius:12px;"">
  <h2 style=""color:#141414;"">ՀԱՊՀ Գրադարան-Սրճարան</h2>
  <p style=""color:#6b6560;"">Բարև, {toName}!</p>
  <p>Գաղտնաբառի վերականգնման կոդն է.</p>
  <div style=""font-size:2.5rem;font-weight:700;letter-spacing:0.5rem;text-align:center;padding:1.5rem;background:#f5f0e8;border-radius:8px;margin:1.5rem 0;color:#141414;"">
    {code}
  </div>
  <p style=""color:#6b6560;font-size:0.85rem;"">Կոդը վավեր է 10 րոպե: Եթե դուք չեք հարցրել, անտեսեք այս նամակը:</p>
</div>";

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = $"Գաղտնաբառի վերականգնում՝ {code}",
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail, toName));
        await client.SendMailAsync(message);
    }
}