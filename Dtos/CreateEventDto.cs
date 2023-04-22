namespace _2cpbackend.Models;

using System.ComponentModel.DataAnnotations;

public class CreateEventDto
{
    //Title
    [Required] public string Title { get; set; } = null!;
    //Date
    [Required][DataType(DataType.DateTime)] public DateTime Date { get; set; }
    //Description
    public string? Description { get; set; }
    //Price
    [Required][DataType(DataType.Currency)] public Double Price { get; set; }
    //Cover
    [DataType(DataType.Url)] public string? CoverPhoto { get; set; }
}