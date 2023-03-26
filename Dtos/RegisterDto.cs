namespace _2cpbackend.Dtos;

using System.ComponentModel.DataAnnotations;

public class RegisterDto
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

    //Password
    [StringLength(100, MinimumLength = 6, ErrorMessage = "The {0} must be at least {2} character long.")]
    [Required][DataType(DataType.Password)] public string Password { get; set; } = null!;
    
    //Confirm Password
    [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
    [Required][DataType(DataType.Password)] public string ConfirmPassword { get; set; } = null!;

    //Birth Date
    [Required][DataType(DataType.DateTime)] public DateTime BirthDate { get; set; }

}