namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using NetTopologySuite.Geometries;

using _2cpbackend.Dtos;
using _2cpbackend.Data;
using _2cpbackend.Models;

[ApiController]
[Route("[Controller]")]
[AllowAnonymous]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public EventsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        this._context = context;
        this._userManager = userManager;
    }

    [Authorize]
    [HttpPost("CreateEvent")]
    public async Task<ActionResult> CreateEventAsync([FromBody]CreateEventDto data)
    {
        if (!ModelState.IsValid)
            return BadRequest(GetModelErrors(ModelState.Values));

        //Retrieve Logged-In user
        var user = await _userManager.GetUserAsync(HttpContext.User);

        //User will never be null, this method needs authorization
        if (user == null)
            return Unauthorized();

        //Creation
        var resource = new Event
        {
            Title = data.Title,
            Date = data.Date.ToUniversalTime(),
            Description = data.Description,
            Price = data.Price,
            CoverPhoto = data.CoverPhoto,
            Organizer = user,
            Attendees = new List<ApplicationUser>(),
            Location = new Point(data.Location.Longitude, data.Location.Latitude)
        };
        
        //Add resource to database
        user.OrganizedByUser.Add(resource);
        await _context.Events.AddAsync(resource);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new {Id = resource.Id}, resource);
    }

    [HttpGet("GetEvent")]
    public ActionResult GetEvent(Guid Id)
    {
        return Ok();
    }

    //Utilities
    [NonAction]
    public string GetModelErrors(ModelStateDictionary.ValueEnumerable values)
    {
        string body = string.Empty;
        foreach (ModelStateEntry entry in values)
        {
            foreach(ModelError error in entry.Errors)
            {
                body += error.ErrorMessage + "\n";
            }
        }
        return body;
    }
}