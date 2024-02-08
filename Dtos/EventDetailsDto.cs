namespace Eventi.Server.Models;

using System.ComponentModel.DataAnnotations;

public class EventDetailsDto
{
    //Id
    public Guid Id { get; set; }
    //Title
    public string Title { get; set; } = null!;
    //Date
    [DataType(DataType.DateTime)] public DateTime DateAndTime { get; set; }
    //Description
    public string? Description { get; set; }
    //Price
    [DataType(DataType.Currency)] public Double Price { get; set; }
    //Cover File
    public string? CoverUrl { get; set; }
    //Location
    public Coordinates Location { get; set; } = null!;
    public string? OrganizerId { get; set; } = null!;
    public int NumberOfSubscribers { get; set; }
    //Category
    public string CategoryName { get; set; } = null!;
    public int CategoryId { get; set; }
    //Date Added
    public DateTime DateAdded { get; set; }
}