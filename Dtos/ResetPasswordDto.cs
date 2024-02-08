namespace Eventi.Server.Models;

using System.ComponentModel.DataAnnotations;

public class ResetPasswordDto
{
    //Email
    [Required][EmailAddress] public string Email { get; set; } = null!;

    //Password
    [StringLength(100, MinimumLength = 6, ErrorMessage = "The {0} must be at least {2} character long.")]
    [Required][DataType(DataType.Password)] public string Password { get; set; } = null!;
    
    //Confirm Password
    [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
    [Required][DataType(DataType.Password)] public string ConfirmPassword { get; set; } = null!;

    [Required] public string Code { get; set; } = null!;
}