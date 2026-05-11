namespace NPUALibraryCafe.DTOs.Auth;

public class SendCodeDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Role { get; set; }
}

public class RegisterDto
{
    public string Email { get; set; } = "";
    public string Code { get; set; } = "";
}

public class LoginDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class UpdateProfileDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
}

public class UserResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
}

public class PendingRegistration
{
    public string Code { get; set; } = "";
    public DateTime Expiry { get; set; }
    public string Name { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "user";
}