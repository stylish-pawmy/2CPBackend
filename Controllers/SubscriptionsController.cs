namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
    public ActionResult<IEnumerable<string>> GetSubscribers(Guid eventId)
    {
        var _event = _context.Events.Include(e => e.Attendees).SingleOrDefault(e => e.Id == eventId);

        if (_event == null) return NotFound();

        var subscribersList = new List<string>();

        foreach (ApplicationUser user in _event.Attendees)
            subscribersList.Add(user.Id);
        
        return Ok(subscribersList);
    }

    [HttpGet("SubscribersPage")]
    public ActionResult<IEnumerable<string>> GetSubscribers(Guid eventId, int startIndex, int endIndex)
    {
        var _event = _context.Events.Include(e => e.Attendees).SingleOrDefault(e => e.Id == eventId);

        if (_event == null) return NotFound();

        var limit = Math.Max(_event.Attendees.Count - 1, 0);

        var subscribersList = new List<string>();

        foreach (ApplicationUser user in _event.Attendees.GetRange(Math.Min(startIndex, limit), Math.Min(endIndex, limit)))
            subscribersList.Add(user.Id);
        
        return Ok(subscribersList);
    }

    [HttpPost("Subscribe")]
    public async Task<ActionResult> SubscribeAsync(Guid eventId)
    {
        var _event = _context.Events.Include(e => e.Attendees).SingleOrDefault(e => e.Id == eventId);
        var user = await  _userManager.GetUserAsync(HttpContext.User);

        //user is not null because method requires authentication
        if (user == null) return StatusCode(500, "User authenticated but server unable to retrieve user reference.");
        
        if (_event == null) return NotFound();

        if (user.AttendedByUser.Contains(_event)) return BadRequest("User already subscribed to event.");

        _event.Attendees.Add(user);
        user.AttendedByUser.Add(_event);

        await _context.SaveChangesAsync();
        
        return Ok("User subscribed to event.");
    }

    [HttpPost("Unsbscribe")]
    public async Task<ActionResult> UnsubscribeAsync(Guid eventId)
    {
        var _event = _context.Events.Include(e => e.Attendees).SingleOrDefault(e => e.Id == eventId);
        var user = await  _userManager.GetUserAsync(HttpContext.User);

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
        var user = await  _userManager.GetUserAsync(HttpContext.User);
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
}