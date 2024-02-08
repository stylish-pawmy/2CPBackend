namespace Eventi.Server.Models;

using System.ComponentModel.DataAnnotations;

public class ForgotPasswordDto
{
    //Email
    [Required][EmailAddress] public string Email { get; set; } = null!;
}