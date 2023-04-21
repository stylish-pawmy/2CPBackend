namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Authorization;

using _2cpbackend.Dtos;
using _2cpbackend.Data;
using _2cpbackend.Models;

[ApiController]
[Route("[Controller]")]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EventsController(ApplicationDbContext context)
    {
        this._context = context;
    }

    [HttpPost("CreateEvent")]
    public ActionResult CreateEventAsync([FromBody]EventDto data)
    {
        if (ModelState.IsValid!)
            return BadRequest(GetModelErrors(ModelState.Values));

        return CreatedAtAction(nameof(CreateEventAsync), new {Id = "1"}, null);
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