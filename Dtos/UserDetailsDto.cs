namespace _2cpbackend.Models;

using System.ComponentModel.DataAnnotations;

public class UserDetailsDto
{
    //Id
    public string? Id { get; set; }
    //First and Last Name
    [Required] public string FirstName { get; set; } = null!;
    [Required] public string LastName { get; set; } = null!;

    //Phone Number
    [DataType(DataType.PhoneNumber)] public string? PhoneNumber { get; set; }

    //Email
    [Required][EmailAddress] public string? Email { get; set; }

    //User Name
    [Required][StringLength(255, MinimumLength = 6)] public string? UserName { get; set; }

    //Profile Picture
    public string? ProfilePictureUrl { get; set; }

    //Biography
    public string? Biography { get; set; }
}