using System.ComponentModel.DataAnnotations;

namespace Zenvi.Shared;

public class RegisterUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; }

    [Required]
    [StringLength(50)]
    public string Surname { get; set; }
}

public class LoginUserDto
{
    [Required]
    public string Email { get; init; }

    [Required]
    public string Password { get; init; }

    public string? TwoFactorCode { get; init; }

    public string? TwoFactorRecoveryCode { get; init; }
}

public class UpdateUserDto
{
    [EmailAddress]
    public string? NewEmail { get; set; }
    public bool IsEmailConfirmed { get; set; }

    public string? NewPassword { get; set; }
    public string? OldPassword { get; set; }

    [StringLength(50)]
    public string? Name { get; set; }

    [StringLength(50)]
    public string? Surname { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    public string? DateOfBirth { get; set; }

    public bool Banned { get; set; }

    public string? ProfilePictureName { get; set; }
    public string? BannerPictureName { get; set; }
}

public class AboutUserDto
{
    public string? Email { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Bio { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool Banned { get; set; }
    public string? ProfilePictureName { get; set; }
    public string? BannerPictureName { get; set; }
}

public class RefreshUserDto
{ 
    [Required]
    public string RefreshToken { get; init; }
}

public class ResendConfirmationEmailUserDto
{
    [Required]
    public string Email { get; init; }
}

public class ForgotPasswordUserDto
{
    [Required]
    public string Email { get; init; }
}

public class ResetPasswordUserDto
{
    [Required]
    public string Email { get; init; }

    [Required]
    public string ResetCode { get; init; }

    [Required]
    public string NewPassword { get; init; }
}

public class TwoFactorUserDto
{
    public bool? Enable { get; init; }

    public string? TwoFactorCode { get; init; }

    public bool ResetSharedKey { get; init; }

    public bool ResetRecoveryCodes { get; init; }

    public bool ForgetMachine { get; init; }
}
