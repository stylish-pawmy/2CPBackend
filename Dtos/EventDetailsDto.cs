namespace _2cpbackend.Models;

using System.ComponentModel.DataAnnotations;

public class EventDetailsDto
{
    //Id
    public Guid Id { get; set; }
    //Title
    public string Title { get; set; } = null!;
    //Date & Time
    [DataType(DataType.DateTime)] public DateTime DateAndTime { get; set; }
    [DataType(DataType.Duration)] public Duration TimeSpan { get; set; } = null!;
    //Description
    public string? Description { get; set; }
    //Price
    [DataType(DataType.Currency)] public Double Price { get; set; }
    //Cover File
    public string? CoverUrl { get; set; }
    //Location
    public Coordinates Location { get; set; } = null!;
    //Organizer
    public string OrganizerId { get; set; } = null!;
    public string? OrganizerProfilePicture { get; set; }
    public string? OrganizerName { get; set; }
    public int NumberOfSubscribers { get; set; }
    //Category
    public string CategoryName { get; set; } = null!;
    public int CategoryId { get; set; }
    //Date Added
    public DateTime DateAdded { get; set; }
    //Max Attendees
    public int MaxAttendees { get; set; }

}