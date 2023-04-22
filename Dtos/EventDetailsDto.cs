namespace _2cpbackend.Models;

using System.ComponentModel.DataAnnotations;

public class EventDetailsDto
{
    //Id
    [Required] public Guid Id { get; set; }
    //Title
    [Required] public string Title { get; set; } = null!;
    //Date
    [Required][DataType(DataType.DateTime)] public DateTime DateAndTime { get; set; }
    //Description
    public string? Description { get; set; }
    //Price
    [Required][DataType(DataType.Currency)] public Double Price { get; set; }
    //Cover File
    public string? CoverUrl { get; set; }
    //Location
    [Required] public Coordinates Location { get; set; } = null!;
    //OrganizerDetails
    [Required] public string? OrganizerUserName { get; set; } = null!;
    [Required] public string? OrganizerId { get; set; } = null!;
    [Required] public int NumberOfSubscribers { get; set; }
}