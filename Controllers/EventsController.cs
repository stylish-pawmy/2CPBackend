namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

using _2cpbackend.Dtos;
using _2cpbackend.Data;
using _2cpbackend.Models;

[ApiController]
[Route("[Controller]")]
[AllowAnonymous]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public EventsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
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

        var user = await _userManager.GetUserAsync(HttpContext.User);

        if (user == null)
            return Unauthorized();

        var organizer = (ApplicationUser) user;

        //Creation
        var resource = new Event
        {
            Title = data.Title,
            Date = data.Date.ToUniversalTime(),
            Description = data.Description,
            Price = data.Price,
            CoverPhoto = data.CoverPhoto,
            Organizer = organizer
        };
        
        await _context.AddAsync(resource);

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