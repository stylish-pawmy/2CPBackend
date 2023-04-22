namespace _2cpbackend.Models;

using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

public class Event
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
    //Organizer
    public ApplicationUser Organizer { get; set; } = null!;
    //Attendees
    public List<ApplicationUser> Attendees { get; set; } = new List<ApplicationUser>();
    public List<ApplicationUser> BanList { get; set; } = new List<ApplicationUser>();
    [DataType(DataType.Url)] public string? CoverPhoto { get; set; }
    //Location
    [Required] public Point Location { get; set; } = null!;
}