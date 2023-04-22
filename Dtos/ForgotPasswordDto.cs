namespace _2cpbackend.Models;

using System.ComponentModel.DataAnnotations;

public class ForgotPasswordDto
{
    //Email
    [Required][EmailAddress] public string Email { get; set; } = null!;
}