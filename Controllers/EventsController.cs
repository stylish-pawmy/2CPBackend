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
using _2cpbackend.Services;

[ApiController]
[Route("api/[Controller]")]
[AllowAnonymous]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IBlobStorage _blobStorage;

    public EventsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IBlobStorage blobStorage)
    {
        this._context = context;
        this._userManager = userManager;
        this._blobStorage = blobStorage;
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

        var category = await _context.Categories
        .Include(c => c.Events)
        .SingleOrDefaultAsync(c => c.Id == data.CategoryId);

        if (category == null)
            return BadRequest("Invalid category");

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
            Location = new Point(data.Location.Longitude, data.Location.Latitude),
            Category = category
        };

        //Add to category index
        category.Events.Add(resource);

        //Upload event cover
        if (data.CoverFile != null && data.CoverFile.Length > 0)
        {
            var coverName = resource.Id.ToString() + Path.GetExtension(data.CoverFile.FileName);
            resource.CoverPhoto = await _blobStorage.UploadBlobAsync("eventcovers", coverName, data.CoverFile);
        }
        
        //Add resource to database
        user.OrganizedByUser.Add(resource);
        await _context.Events.AddAsync(resource);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new {Id = resource.Id}, resource.Id);
    }

    [HttpGet("GetEvent")]
    public ActionResult<EventDetailsDto> GetEvent(Guid Id)
    {
        var resource = _context.Events
        .Include(e => e.Organizer)
        .Include(e => e.Attendees)
        .Include(e => e.Category)
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
            CoverUrl = resource.CoverPhoto,
            Location = new _2cpbackend.Models.Coordinates
            {
                Longitude = resource.Location.X,
                Latitude = resource.Location.Y
            },
            OrganizerId = resource.Organizer.Id,
            NumberOfSubscribers = resource.Attendees.Count(),
            CategoryId = resource.Category.Id,
            CategoryName = resource.Category.Name
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

    [Authorize]
    [HttpDelete("CancelEvent")]
    public async Task<ActionResult> CancelEventAsync(Guid Id)
    {
        var resource = _context.Events
        .Include(e => e.Organizer)
        .Include(e => e.Category)
        .SingleOrDefault(e => e.Id == Id);
        var user = await _userManager.GetUserAsync(HttpContext.User);

        //Check if user is organizer, user not null because requires authentication
        if ((user != null) && (resource != null) && (user.Id != resource.Organizer.Id))
            return Unauthorized("You do not own this resource.");

        //Resource not found
        if (resource == null)
            return NotFound();

        //Remove cover image
        if (resource.CoverPhoto != null)
            await _blobStorage.DeleteBlobAsync("eventcovers", Path.GetFileName(resource.CoverPhoto));

        //Delete from tables
        _context.Events.Remove(resource);
        resource.Organizer.OrganizedByUser.Remove(resource);

        //Delete from category index
        var category = await _context.Categories
        .Include(c => c.Events)
        .SingleOrDefaultAsync(c => c.Id == resource.Category.Id);

        if (category == null) return StatusCode(500, "Category should not be null.");
        category.Events.Remove(resource);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpPut("EditEvent")]
    public async Task<ActionResult> EditEventAsync(Guid Id, [FromForm][FromBody] CreateEditEventDto data)
    {
        var resource = _context.Events
        .Include(e => e.Organizer)
        .Include(e => e.Category)
        .ThenInclude(c => c.Events)
        .SingleOrDefault(e => e.Id == Id);
        var user = await _userManager.GetUserAsync(HttpContext.User);
        var newCategory = await _context.Categories.Include(c => c.Events).SingleOrDefaultAsync(c => c.Id == data.CategoryId);


        //User is organizer? (user not null because requires authentication)
        if ((user != null) && (resource != null) && (user.Id != resource.Organizer.Id))
            return Unauthorized("You do not own this resource.");

        //Resource not found
        if (resource == null)
            return NotFound();

        //New category exists?
        if (newCategory == null)
            return BadRequest("Specified category not found");

        //New data is valid?
        if (!ModelState.IsValid)
            return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));

        //Edit category
        resource.Category.Events.Remove(resource);
        newCategory.Events.Add(resource);
        
        //Edit event info
        resource.Title = data.Title;
        resource.Location = new Point(data.Location.Longitude, data.Location.Latitude);
        resource.DateAndTime = data.DateAndTime.ToUniversalTime();
        resource.Price = data.Price;
        resource.Description = data.Description;
        resource.Category = newCategory;

        await _context.SaveChangesAsync();

        return Ok(resource);
    }

    [HttpPut("UpdateCover")]
    public async Task<ActionResult> UpdateCoverAsync(Guid eventId, IFormFile newCover)
    {
        var resource = _context.Events.Include(e => e.Organizer).SingleOrDefault(e => e.Id == eventId);
        var user = await _userManager.GetUserAsync(HttpContext.User);

        //User is organizer? (user not null because requires authentication)
        if ((user != null) && (resource != null) && (user.Id != resource.Organizer.Id))
            return Unauthorized("You do not own this resource.");

        //Resource not found
        if (resource == null)
            return NotFound();
        
        //Delete old cover if existent
        if (resource.CoverPhoto != null)
            await _blobStorage.DeleteBlobAsync("eventcovers", Path.GetFileName(resource.CoverPhoto));

        //Upload new cover if existent
        if (newCover != null && newCover.Length > 0)
        {
            var coverName = resource.Id.ToString() + Path.GetExtension(newCover.FileName);
            resource.CoverPhoto = await _blobStorage.UploadBlobAsync("eventcovers", coverName, newCover);
        }

        await _context.SaveChangesAsync();

        return Ok(resource.CoverPhoto);
    }
}