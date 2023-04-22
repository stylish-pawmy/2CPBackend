namespace _2cpbackend.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Biography { get; set; }
    public DateTime BirthDate { get; set; }

    //Organizing Events
    public List<Event> OrganizedByUser { get; set; } = new List<Event>();
    //Attending Events
    public List<Event> AttendedByUser {get; set; } = new List<Event>();
    public string? ProfilePicture {get; set;}
}