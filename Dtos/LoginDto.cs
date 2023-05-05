namespace _2cpbackend.Models;

using System.ComponentModel.DataAnnotations;

public class LoginDto
{
    //Identifier
    [Required] public string Identifier { get; set; } = null!;

    //Password
    [Required][DataType(DataType.Password)] public string Password { get; set; } = null!;
}