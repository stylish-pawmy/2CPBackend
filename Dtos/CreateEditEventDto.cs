namespace Eventi.Server.Models;

using System.ComponentModel.DataAnnotations;

public class CreateEditEventDto
{
    //Title
    [Required] public string Title { get; set; } = null!;
    //Date
    [Required][DataType(DataType.DateTime)] public DateTime DateAndTime { get; set; }
    //Description
    public string? Description { get; set; }
    //Price
    [Required][DataType(DataType.Currency)] public Double Price { get; set; }
    //Cover File
    public IFormFile? CoverFile { get; set; }
    //Location
    [Required] public Coordinates Location { get; set; } = null!;
    //Category
    [Required] public int CategoryId { get; set; }
}