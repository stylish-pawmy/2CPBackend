namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

using _2cpbackend.Data;
using _2cpbackend.Models;
using _2cpbackend.Utilities;

[ApiController]
[Route("api/[Controller]")]
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
    public async Task<ActionResult> CreateEventAsync([FromForm][FromBody]CreateEditEventDto data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));

        //Retrieve Logged-In user
        var user = await _userManager.GetUserAsync(HttpContext.User);

        //User will never be null, this method needs authorization
        if (user == null)
            return Unauthorized();

        //Creation
        var resource = new Event
        {
            Id = Guid.NewGuid(),
            Title = data.Title,
            DateAndTime = data.DateAndTime.ToUniversalTime(),
            Description = data.Description,
            Price = data.Price,
            Organizer = user,
            Attendees = new List<ApplicationUser>(),
            Location = new Point(data.Location.Longitude, data.Location.Latitude)
        };

        //Upload event cover
        if (data.CoverFile != null && data.CoverFile.Length > 0)
        {
            var coverName = resource.Id.ToString() + Path.GetExtension(data.CoverFile.FileName);
            var coverPath = Path.Combine("Data/EventCovers", coverName);
            var absoluteCoverPath = Path.GetFullPath(coverPath);

            using (var fileStream = new FileStream(absoluteCoverPath, FileMode.Create))
            {
                await data.CoverFile.CopyToAsync(fileStream);
            }

            resource.CoverPhoto = coverName;
        }
        
        //Add resource to database
        user.OrganizedByUser.Add(resource);
        await _context.Events.AddAsync(resource);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new {Id = resource.Id}, resource);
    }

    [HttpGet("GetEvent")]
    public ActionResult<EventDetailsDto> GetEvent(Guid Id)
    {
        var resource = _context.Events
        .Include(e => e.Organizer)
        .Include(e => e.Attendees)
        .SingleOrDefault(e => e.Id == Id);

        //Resource not found
        if (resource == null)
            return NotFound();
        
        //Resource found
        var data = new EventDetailsDto
        {
            Id = resource.Id,
            Title = resource.Title,
            DateAndTime = resource.DateAndTime,
            Description = resource.Description,
            Price = resource.Price,
            CoverUrl = Url.ActionLink(nameof(EventCover), "Events", new {coverName = resource.CoverPhoto}),
            Location = new _2cpbackend.Models.Coordinates
            {
                Longitude = resource.Location.X,
                Latitude = resource.Location.Y
            },
            OrganizerId = resource.Organizer.Id,
            NumberOfSubscribers = resource.Attendees.Count()
        };

        return Ok(data);
    }

    [HttpGet("GetEventsList")]
    public ActionResult<IEnumerable<Guid>> GetEventsList()
    {
        var resource = _context.Events.ToList();
        //Resource not found
        if (resource == null)
            return NotFound();
        
        //Resource found
        var data = new List<Guid>();

        foreach(Event _event in resource)
        {
            data.Add(_event.Id);
        }

        return Ok(data);
    }

    [HttpGet("GetEventsPage")]
    public ActionResult<IEnumerable<Guid>> GetEventsPage(int startIndex, int endIndex)
    {
        var resource = _context.Events.ToList();
        var limit = Math.Max(resource.Count - 1, 0);
        
        //Resource found
        var data = new List<Guid>();

        foreach(Event _event in resource.GetRange(Math.Min(startIndex, limit), Math.Min(endIndex, limit)))
        {
            data.Add(_event.Id);
        }

        return Ok(data);
    }

    [HttpGet("EventCover")]
    public ActionResult EventCover(string coverName)
    {
        //cover name provided?
        if (coverName == null)
            return BadRequest("Please include a cover name.");
        
        var coverPath = Path.Combine("Data/EventCovers", coverName);
        var absoluteCoverPath = Path.GetFullPath(coverPath);

        var mimeType = new FileExtensionContentTypeProvider().TryGetContentType(coverName, out var mime)
            ? mime
            : "image/octet-extract";

        //cover file exists?
        if (!System.IO.File.Exists(absoluteCoverPath))
            return NotFound();

        var fileStream = new FileStream(absoluteCoverPath, FileMode.Open ,FileAccess.Read);

        return File(fileStream, mimeType);
    }

    [Authorize]
    [HttpDelete("CancelEvent")]
    public async Task<ActionResult> CancelEventAsync(Guid Id)
    {
        var resource = _context.Events.Include(e => e.Organizer).SingleOrDefault(e => e.Id == Id);
        var user = await _userManager.GetUserAsync(HttpContext.User);

        //Check if user is organizer, user not null because requires authentication
        if ((user != null) && (resource != null) && (user.Id != resource.Organizer.Id))
            return Unauthorized("You do not own this resource.");

        //Resource not found
        if (resource == null)
            return NotFound();
        
        //Remove cover image
        if (resource.CoverPhoto != null)
        {
            var coverPath = Path.Combine("Data/EventCovers", resource.CoverPhoto);
            var absoluteCoverPath = Path.GetFullPath(coverPath);
            System.IO.File.Delete(absoluteCoverPath);
        }

        //Delete from tables
        _context.Events.Remove(resource);
        resource.Organizer.OrganizedByUser.Remove(resource);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpPut("EditEvent")]
    public async Task<ActionResult> EditEventAsync(Guid Id, [FromForm][FromBody] CreateEditEventDto data)
    {
        var resource = _context.Events.Include(e => e.Organizer).SingleOrDefault(e => e.Id == Id);
        var user = await _userManager.GetUserAsync(HttpContext.User);

        //User is organizer? (user not null because requires authentication)
        if ((user != null) && (resource != null) && (user.Id != resource.Organizer.Id))
            return Unauthorized("You do not own this resource.");

        //Resource not found
        if (resource == null)
            return NotFound();
        
        //New data is valid?
        if (!ModelState.IsValid)
            return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));
        
        //Edit event info
        resource.Title = data.Title;
        resource.Location = new Point(data.Location.Longitude, data.Location.Latitude);
        resource.DateAndTime = data.DateAndTime.ToUniversalTime();
        resource.Price = data.Price;
        resource.Description = data.Description;

        //Delete old cover if existent
        string coverName;
        string coverPath;
        string absoluteCoverPath;

        if (resource.CoverPhoto != null)
        {
            coverPath = Path.Combine("Data/EventCovers", resource.CoverPhoto);
            absoluteCoverPath = Path.GetFullPath(coverPath);
            System.IO.File.Delete(absoluteCoverPath);
            resource.CoverPhoto = null;
        }

        //Upload new cover if existent
        if (data.CoverFile != null && data.CoverFile.Length > 0)
        {
            coverName = resource.Id.ToString() + Path.GetExtension(data.CoverFile.FileName);
            coverPath = Path.Combine("Data/EventCovers", coverName);
            absoluteCoverPath = Path.GetFullPath(coverPath);

            using (var fileStream = new FileStream(absoluteCoverPath, FileMode.Create))
            {
                await data.CoverFile.CopyToAsync(fileStream);
            }

            resource.CoverPhoto = coverName;
        }

        await _context.SaveChangesAsync();

        return Ok(resource);
    }
}