namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
    private readonly ISearchEngine _searchEngine;

    public EventsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IBlobStorage blobStorage,
        ISearchEngine searchEngine)
    {
        this._context = context;
        this._userManager = userManager;
        this._blobStorage = blobStorage;
        this._searchEngine = searchEngine;
    }

    [Authorize]
    [HttpPost("CreateEvent")]
    public async Task<ActionResult> CreateEventAsync([FromForm][FromBody]CreateEditEventDto data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));

        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

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
            Category = category,
            DateAdded = DateTime.Now.ToUniversalTime(),
            MaxAttendees = data.MaxAttendees
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

        //Add resource to search engine index
        _searchEngine.GetWriter();
        _searchEngine.AddToIndex(resource);
        _searchEngine.DisposeWriter();

        return CreatedAtAction(nameof(GetEventDetails), new {Id = resource.Id}, resource.Id);
    }

    [HttpGet("GetEventDetails")]
    public ActionResult<EventDetailsDto> GetEventDetails(Guid Id)
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
            CategoryName = resource.Category.Name,
            DateAdded = resource.DateAdded,
            OrganizerName = resource.Organizer.UserName,
            OrganizerProfilePicture = resource.Organizer.ProfilePicture,
            MaxAttendees = resource.MaxAttendees
        };

        return Ok(data);
    }

    [HttpGet("GetEventsList")]
    public ActionResult<IEnumerable<EventDetailsDto>> GetEventsList()
    {
        var resource = _context.Events
        .Include(e => e.Attendees)
        .Include(e => e.Organizer)
        .Include(e => e.Category).ToList();

        //Resource not found
        if (resource == null)
            return NotFound();
        
        //Resource found
        var data = new List<EventDetailsDto>();

        foreach(Event _event in resource)
        {
            //Event details object
            var result = new EventDetailsDto
            {
                Id = _event.Id,
                Title = _event.Title,
                DateAndTime = _event.DateAndTime,
                Description = _event.Description,
                Price = _event.Price,
                CoverUrl = _event.CoverPhoto,
                Location = new _2cpbackend.Models.Coordinates
                {
                    Longitude = _event.Location.X,
                    Latitude = _event.Location.Y
                },
                OrganizerId = _event.Organizer.Id,
                NumberOfSubscribers = _event.Attendees.Count(),
                CategoryId = _event.Category.Id,
                CategoryName = _event.Category.Name,
                DateAdded = _event.DateAdded,
                OrganizerName = _event.Organizer.UserName,
                OrganizerProfilePicture = _event.Organizer.ProfilePicture,
                MaxAttendees = _event.MaxAttendees
            };
            
            data.Add(result);
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
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

        //Check if user is organizer
        if ((resource != null) && (user.Id != resource.Organizer.Id))
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

        //Delete from search engine index
        _searchEngine.GetWriter();
        _searchEngine.RemoveFromIndex(resource);
        _searchEngine.DisposeWriter();

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

        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

        var newCategory = await _context.Categories.Include(c => c.Events).SingleOrDefaultAsync(c => c.Id == data.CategoryId);


        //User is organizer?
        if ((resource != null) && (user.Id != resource.Organizer.Id))
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
        
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

        //User is organizer?
        if ((resource != null) && (user.Id != resource.Organizer.Id))
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

    [HttpGet("CategoryList")]
    public ActionResult<List<EventCategory>> GetCategoryList()
    {
        return Ok(_context.Categories.Select(c => new {Id = c.Id, Name = c.Name}).ToList());
    }

    [HttpGet("EventsInCategoryList")]
    public ActionResult<List<EventDetailsDto>> GetEventsInCategoryList(int categoryId)
    {
        var category = _context.Categories.Include(c => c.Events).SingleOrDefault(c => c.Id == categoryId);

        if (category == null)
            return NotFound();
        
        for (int i = 0; i < category.Events.Count; i++)
        {
            //Get references to other tables
            var reference = _context.Events
            .Include(e => e.Organizer)
            .Include(e => e.Attendees)
            .Include(e => e.Category).SingleOrDefault(e => e.Id == category.Events[i].Id);

            if (reference == null) throw new NullReferenceException();

            category.Events[i] = reference;
        }
        
        var data = new List<EventDetailsDto>();

        foreach (Event _event in category.Events)
        {
            //Event details object
            var result = new EventDetailsDto
            {
                Id = _event.Id,
                Title = _event.Title,
                DateAndTime = _event.DateAndTime,
                Description = _event.Description,
                Price = _event.Price,
                CoverUrl = _event.CoverPhoto,
                Location = new _2cpbackend.Models.Coordinates
                {
                    Longitude = _event.Location.X,
                    Latitude = _event.Location.Y
                },
                OrganizerId = _event.Organizer.Id,
                NumberOfSubscribers = _event.Attendees.Count(),
                CategoryId = _event.Category.Id,
                CategoryName = _event.Category.Name,
                DateAdded = _event.DateAdded,
                OrganizerName = _event.Organizer.UserName,
                OrganizerProfilePicture = _event.Organizer.ProfilePicture,
                MaxAttendees = _event.MaxAttendees
            };
            
            data.Add(result);
        }

        return Ok(data);
    }


    [HttpGet("SearchEvent")]
    public async Task<ActionResult<IEnumerable<EventDetailsDto>>> SearchEvent(string query, int amount)
    {   
        _searchEngine.GetWriter();
        var hits = _searchEngine.SearchEvent(query, amount);
        _searchEngine.DisposeWriter();

        var data = new List<EventDetailsDto>();

        foreach (string eventId in hits)
        {
            //Event details object
            var _event = await _context.Events
            .Include(e => e.Organizer)
            .Include(e => e.Attendees)
            .Include(e => e.Category)
            .SingleOrDefaultAsync(e => e.Id.ToString() == eventId);

            if (_event == null) throw new NullReferenceException();

            var result = new EventDetailsDto
            {
                Id = _event.Id,
                Title = _event.Title,
                DateAndTime = _event.DateAndTime,
                Description = _event.Description,
                Price = _event.Price,
                CoverUrl = _event.CoverPhoto,
                Location = new _2cpbackend.Models.Coordinates
                {
                    Longitude = _event.Location.X,
                    Latitude = _event.Location.Y
                },
                OrganizerId = _event.Organizer.Id,
                NumberOfSubscribers = _event.Attendees.Count(),
                CategoryId = _event.Category.Id,
                CategoryName = _event.Category.Name,
                DateAdded = _event.DateAdded,
                OrganizerName = _event.Organizer.UserName,
                OrganizerProfilePicture = _event.Organizer.ProfilePicture,
                MaxAttendees = _event.MaxAttendees
            };
            
            data.Add(result);
        }

        return Ok(data);
    }
}