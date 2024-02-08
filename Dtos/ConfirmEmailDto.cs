namespace Eventi.Server.Models;

using System.ComponentModel.DataAnnotations;

public class ConfirmEmailDto
{
    //Email
    [Required][EmailAddress] public string Email { get; set; } = null!;

    //Code
    [Required]public string Code { get; set; } = null!;

}