namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

using _2cpbackend.Models;
using _2cpbackend.Data;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("GetUserDetails")]
    public async Task<ActionResult> GetUserDetailsAsync(string subjectId)
    {
        var user = await _userManager.FindByIdAsync(subjectId);

        if (user == null) return NotFound();

        if (user.Email == null || user.UserName == null) return StatusCode(500, "Identifiers should not be null.");

        var resource = new UserDetailsDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Biography = user.Biography,
            UserName = user.UserName,
        };

        //Adding profile picture link
        resource.ProfilePictureUrl = Url.ActionLink("ProfilePicture", "Users", new {pictureName = user.ProfilePicture});
        return Ok(resource);
    }

    [HttpGet("ProfilePicture")]
    public ActionResult ProfilePicture(string pictureName)
    {
        if (pictureName == null) return BadRequest("Include a profile picture name.");
        var picturePath = Path.Combine("Data/ProfilePictures", pictureName);
        var absolutePicturePath = Path.GetFullPath(picturePath);

        if (!System.IO.File.Exists(absolutePicturePath)) return NotFound();

        var mimeType = new FileExtensionContentTypeProvider().TryGetContentType(pictureName, out var mime)
        ? mime
        : "image/octet-extract";

        var fileStream = new FileStream(absolutePicturePath, FileMode.Open, FileAccess.Read);
        return File(fileStream, mimeType);
    }

}