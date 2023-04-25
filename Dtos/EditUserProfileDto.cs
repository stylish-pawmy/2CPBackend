namespace _2cpbackend.Models;

using System.ComponentModel.DataAnnotations;
public class EditUserProfileDto
{
    [DataType(DataType.Text)][StringLength(255)] public string UserName { get; set; } = null!;
    [DataType(DataType.Text)][StringLength(255)] public string FirstName { get; set; } = null!;
    [DataType(DataType.Text)][StringLength(255)] public string LastName { get; set; } = null!;
    [DataType(DataType.Text)][StringLength(2000)] public string Biography { get; set; } = null!;
    [DataType(DataType.Date)] public DateTime BirthDate { get; set; }
}