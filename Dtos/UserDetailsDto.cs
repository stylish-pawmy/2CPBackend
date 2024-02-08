namespace Eventi.Server.Models;

using System.ComponentModel.DataAnnotations;

public class UserDetailsDto
{
    //First and Last Name
    [Required] public string FirstName { get; set; } = null!;
    [Required] public string LastName { get; set; } = null!;

    //Phone Number
    [DataType(DataType.PhoneNumber)] public string? PhoneNumber { get; set; }

    //Email
    [Required][EmailAddress] public string Email { get; set; } = null!;

    //User Name
    [Required][StringLength(255, MinimumLength = 6)] public string UserName { get; set; } = null!;

    //Profile Picture
    public string? ProfilePictureUrl { get; set; }

    //Biography
    public string? Biography { get; set; }
}