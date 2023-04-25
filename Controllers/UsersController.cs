namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

using _2cpbackend.Models;
using _2cpbackend.Utilities;
using _2cpbackend.Data;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
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
            UserName = user.UserName
        };

        //Adding profile picture link
        if (user.ProfilePicture == null) resource.ProfilePictureUrl = null;
        else resource.ProfilePictureUrl = Url.ActionLink("ProfilePicture", "Users", new {picture = user.ProfilePicture});
        return Ok(resource);
    }

    [HttpGet("ProfilePicture")]
    public ActionResult ProfilePicture(string picture)
    {
        if (picture == null) return BadRequest("Include a profile picture name.");
        var picturePath = Path.Combine("Data/ProfilePictures", picture);
        var absolutePicturePath = Path.GetFullPath(picturePath);

        if (!System.IO.File.Exists(absolutePicturePath)) return NotFound();

        var mimeType = new FileExtensionContentTypeProvider().TryGetContentType(picture, out var mime)
        ? mime
        : "image/octet-extract";

        var fileStream = new FileStream(absolutePicturePath, FileMode.Open, FileAccess.Read);
        return File(fileStream, mimeType);
    }

    [HttpPut("EditUserProfile")]
    public async Task<ActionResult> EditUserProfileAsync([FromForm][FromBody] EditUserProfileDto data)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);

        if (!ModelState.IsValid) return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));
        
        if (user == null) return StatusCode(500, "User reference should not be null.");

        user.FirstName = data.FirstName;
        user.LastName = data.LastName;
        user.BirthDate = data.BirthDate.ToUniversalTime();
        user.Biography = data.Biography;

        if (user.UserName != data.UserName && await _userManager.FindByNameAsync(data.UserName) != null)
            return BadRequest("UserName already taken.");
        
        await _userManager.SetUserNameAsync(user, data.UserName);

        return Ok(data);
    }

    [HttpPut("ChangePicture")]
    public async Task<ActionResult> ChangePictureAsync(IFormFile newPicture)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);

        if (!ModelState.IsValid) return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));
        if (user == null) return StatusCode(500, "User reference should not be null.");

        //Upload new picture
        if (newPicture != null && newPicture.Length > 0)
        {
            string pictureName;
            string picturePath;
            string absolutePicturePath;

            pictureName = user.Id + Path.GetExtension(newPicture.FileName);

            if (user.ProfilePicture != null)
            {
                picturePath = "Data/ProfilePictures/" + user.ProfilePicture;
                absolutePicturePath = Path.GetFullPath(picturePath);

                //Delete old profile picture
                System.IO.File.Delete(absolutePicturePath);
            }

            picturePath = "Data/ProfilePictures/" + pictureName;
            absolutePicturePath = Path.GetFullPath(picturePath);

            using (FileStream fileStream = new FileStream(absolutePicturePath, FileMode.Create))
            {
                await newPicture.CopyToAsync(fileStream);
            }

            user.ProfilePicture = pictureName;
            await _userManager.UpdateAsync(user);
        }

        return Ok(Url.ActionLink("ProfilePicture", "Users", new {picture = user.ProfilePicture}));
    }

    [HttpDelete("RemovePicture")]
    public async Task<ActionResult> RemovePictureAsync()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);

        if (!ModelState.IsValid) return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));
        if (user == null) return StatusCode(500, "User reference should not be null.");

        if (user.ProfilePicture == null) return BadRequest("User does not have a profile picture.");

        //Get picture path
        var picturePath = "Data/ProfilePictures/" + user.ProfilePicture;
        var absolutePicturePath = Path.GetFullPath(picturePath);

        //Delete picture
        System.IO.File.Delete(absolutePicturePath);
        user.ProfilePicture = null;
        await _userManager.UpdateAsync(user);

        return NoContent();
    }
}