namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using _2cpbackend.Models;
using _2cpbackend.Data;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public SubscriptionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet("SubscribersList")]
    public ActionResult<IEnumerable<UserDetailsDto>> GetSubscribers(Guid eventId)
    {
        var _event = _context.Events.Include(e => e.Attendees).SingleOrDefault(e => e.Id == eventId);

        if (_event == null) return NotFound();

        var data = new List<UserDetailsDto>();
        foreach (ApplicationUser subscriber in _event.Attendees)
        {
            var resource = new UserDetailsDto
            {
                FirstName = subscriber.FirstName,
                LastName = subscriber.LastName,
                Email = subscriber.Email,
                PhoneNumber = subscriber.PhoneNumber,
                Biography = subscriber.Biography,
                UserName = subscriber.UserName
            };

            //Adding profile picture link
            if (subscriber.ProfilePicture == null) resource.ProfilePictureUrl = null;
            else resource.ProfilePictureUrl = subscriber.ProfilePicture;
            data.Add(resource);
        }
        
        return Ok(data);
    }


    [HttpPost("Subscribe")]
    public async Task<ActionResult> SubscribeAsync(Guid eventId)
    {
        var _event = await _context.Events.Include(e => e.Attendees).SingleOrDefaultAsync(e => e.Id == eventId);
        
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        //user is not null because method requires authentication
        if (user == null) return StatusCode(500, "User authenticated but server unable to retrieve user reference.");
        
        if (_event == null) return NotFound();

        if (_event.BanList.Contains(user)) return Unauthorized("User is banned from attending this event.");

        if (user.AttendedByUser.Contains(_event)) return BadRequest("User already subscribed to event.");
        if (_event.Attendees.Count == _event.MaxAttendees) return BadRequest("Event already at maximum capacity.");

        _event.Attendees.Add(user);
        user.AttendedByUser.Add(_event);

        await _context.SaveChangesAsync();
        
        return Ok("User subscribed to event.");
    }

    [HttpPost("Unsbscribe")]
    public async Task<ActionResult> UnsubscribeAsync(Guid eventId)
    {
        var _event = _context.Events.Include(e => e.Attendees).SingleOrDefault(e => e.Id == eventId);
        
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        //user is not null because method requires authentication
        if (user == null) return StatusCode(500, "User authenticated but server unable to retrieve user reference.");
        
        if (_event == null) return NotFound();

        if (!user.AttendedByUser.Contains(_event)) return BadRequest("User not subscribed to event.");

        _event.Attendees.Remove(user);
        user.AttendedByUser.Remove(_event);

        await _context.SaveChangesAsync();
        
        return Ok("User unsubscribed from event.");
    }

    [HttpPost("KickUser")]
    public async Task<ActionResult> KickUserAsync(String subjectId, Guid eventId)
    {
        var _event = _context.Events.Include(e => e.Organizer).SingleOrDefault(e => e.Id == eventId);
        
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        //The user subjected to operation
        var subject = _context.ApplicationUsers.Include(u => u.AttendedByUser).SingleOrDefault(u => u.Id == subjectId);

        //user is not null because method requires authentication
        if (user == null) return StatusCode(500, "User authenticated but server unable to retrieve user reference.");
        
        if ((_event != null) && (user.Id != _event.Organizer.Id)) return Unauthorized("You do not own this event.");

        if (_event == null) return NotFound("Event not found.");

        if (subject == null) return NotFound("User not found.");

        if (!subject.AttendedByUser.Contains(_event)) return BadRequest("User not subscribed to event.");

        _event.Attendees.Remove(user);
        subject.AttendedByUser.Remove(_event);

        await _context.SaveChangesAsync();
        
        return Ok("User kicked from event.");
    }

    [HttpPost("BanUser")]
    public async Task<ActionResult> BanUserAsync(String subjectId, Guid eventId)
    {
        var _event = _context.Events
        .Include(e => e.Organizer)
        .Include(e => e.BanList)
        .SingleOrDefault(e => e.Id == eventId);

        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        //The user subjected to operation
        var subject = _context.ApplicationUsers.Include(u => u.AttendedByUser).SingleOrDefault(u => u.Id == subjectId);

        //user is not null because method requires authentication
        if (user == null) return StatusCode(500, "User authenticated but server unable to retrieve user reference.");
        
        if ((_event != null) && (user.Id != _event.Organizer.Id)) return Unauthorized("You do not own this event.");

        if (_event == null) return NotFound("Event not found.");

        if (subject == null) return NotFound("User not found.");

        if (user.Id == subject.Id) return BadRequest("Organizer cannot be banned from event.");

        if (_event.BanList.Contains(subject)) return BadRequest("User already banned from event.");

        //Kick from event if already attending
        if (subject.AttendedByUser.Contains(_event))
        {
            _event.Attendees.Remove(subject);
            subject.AttendedByUser.Remove(_event);
        }

        _event.BanList.Add(subject);

        await _context.SaveChangesAsync();
        
        return Ok("User banned from event.");
    }

    [HttpPost("UnbanUser")]
    public async Task<ActionResult> UnbanUserAsync(String subjectId, Guid eventId)
    {
        var _event = _context.Events
        .Include(e => e.Organizer)
        .Include(e => e.BanList)
        .SingleOrDefault(e => e.Id == eventId);

        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        //User subjected to operation
        var subject = _context.ApplicationUsers.Include(u => u.AttendedByUser).SingleOrDefault(u => u.Id == subjectId);

        //user is not null because method requires authentication
        if (user == null) return StatusCode(500, "User authenticated but server unable to retrieve user reference.");
        
        if ((_event != null) && (user.Id != _event.Organizer.Id)) return Unauthorized("You do not own this event.");

        if (_event == null) return NotFound("Event not found.");

        if (subject == null) return NotFound("User not found.");

        if (!_event.BanList.Contains(subject)) return BadRequest("User is not banned from event.");

        _event.BanList.Remove(subject);

        await _context.SaveChangesAsync();
        
        return Ok("User Unbanned from event.");
    }

    [AllowAnonymous]
    [HttpGet("OrganizedEventList")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetOrganizedEventAsync(string Id)
    {
        var user = await  _context.ApplicationUsers.Include(u => u.OrganizedByUser).SingleOrDefaultAsync(u => u.Id == Id);

        if (user == null) return NotFound();

        //Include references to events
        for (int i = 0; i < user.OrganizedByUser.Count; i++)
        {
            var _event = await _context.Events
            .Include(e => e.Attendees)
            .Include(e => e.Organizer)
            .Include(e => e.Category)
            .SingleOrDefaultAsync(e => e.Id == user.OrganizedByUser[i].Id);

            if (_event == null) return StatusCode(500); 

            user.OrganizedByUser[i] = _event;
        }

        var data = new List<EventDetailsDto>();

        foreach (Event _event in user.OrganizedByUser)
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
                MaxAttendees = _event.MaxAttendees,
                TimeSpan = new Duration(_event.TimeSpan),
                Status = _event.Status
            };
            
            data.Add(result);
        }

        return Ok(data);
    }

    [HttpGet("SubscribedToEventList")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetSubscribedToEventListAsync()
    {
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return NotFound();

        user = await _userManager.Users
        .Include(u => u.AttendedByUser)
        .SingleOrDefaultAsync(u => u.Id == user.Id);
        
        if (user == null) return StatusCode(500, "User reference should not be null.");

        //Include references to events
        for (int i = 0; i < user.AttendedByUser.Count; i++)
        {
            var _event = await _context.Events
            .Include(e => e.Attendees)
            .Include(e => e.Organizer)
            .Include(e => e.Category)
            .SingleOrDefaultAsync(e => e.Id == user.AttendedByUser[i].Id);

            if (_event == null) return StatusCode(500); 

            user.AttendedByUser[i] = _event;
        }

        var data = new List<EventDetailsDto>();

        foreach (Event _event in user.AttendedByUser)
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
                MaxAttendees = _event.MaxAttendees,
                TimeSpan = new Duration(_event.TimeSpan),
                Status = _event.Status
            };
            
            data.Add(result);
        }

        return Ok(data);
    }
    
    [HttpPost("SaveEvent")]
    public async Task<ActionResult> SaveEventAsync(Guid eventId)
    {
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

        user = await _userManager.Users
        .Include(u => u.SavedEvents)
        .SingleOrDefaultAsync(u => u.Id == user.Id);

        if (user == null) return StatusCode(500, "User reference should not be null.");

        //Save event
        var resource = await _context.Events.SingleOrDefaultAsync(e => e.Id == eventId);
        
        if (resource == null) return NotFound();

        if (user.SavedEvents.Contains(resource)) return BadRequest("Event already saved by user.");

        user.SavedEvents.Add(resource);
        await _userManager.UpdateAsync(user);

        return Ok("Event saved.");
    }

    [HttpPost("UnsaveEvent")]
    public async Task<ActionResult> UnsaveEventAsync(Guid eventId)
    {
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

        user = await _userManager.Users
        .Include(u => u.SavedEvents)
        .SingleOrDefaultAsync(u => u.Id == user.Id);

        if (user == null) return StatusCode(500, "User reference should not be null.");

        //Unsave event
        var resource = await _context.Events.SingleOrDefaultAsync(e => e.Id == eventId);
        
        if (resource == null) return NotFound();

        if (!user.SavedEvents.Contains(resource)) return BadRequest("Event not saved by user before.");

        user.SavedEvents.Remove(resource);
        await _userManager.UpdateAsync(user);

        return Ok("Event removed from user saved events list.");
    }

    [HttpGet("SavedEventsList")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetSavedEventsList()
    {
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

        user = await _userManager.Users
        .Include(u => u.SavedEvents)
        .SingleOrDefaultAsync(u => u.Id == user.Id);

        if (user == null) return StatusCode(500, "User reference should not be null.");

        //Include references to events
        for (int i = 0; i < user.SavedEvents.Count; i++)
        {
            var _event = await _context.Events
            .Include(e => e.Attendees)
            .Include(e => e.Organizer)
            .Include(e => e.Category)
            .SingleOrDefaultAsync(e => e.Id == user.SavedEvents[i].Id);

            if (_event == null) return StatusCode(500); 

            user.SavedEvents[i] = _event;
        }

        var data = new List<EventDetailsDto>();

        foreach (Event _event in user.SavedEvents)
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
                MaxAttendees = _event.MaxAttendees,
                TimeSpan = new Duration(_event.TimeSpan),
                Status = _event.Status
            };
            
            data.Add(result);
        }

        return Ok(data);;
    }
}