namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

using _2cpbackend.Models;
using _2cpbackend.Utilities;
using _2cpbackend.Data;
using _2cpbackend.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorage _blobStorage;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IBlobStorage blobStorage)
    {
        _userManager = userManager;
        _context = context;
        _blobStorage = blobStorage;
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
        else resource.ProfilePictureUrl = user.ProfilePicture;
        return Ok(resource);
    }


    [HttpPut("UpdateUserProfile")]
    public async Task<ActionResult> UpdateUserProfileAsync([FromForm][FromBody] EditUserProfileDto data)
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

    [HttpPut("UpdatePicture")]
    public async Task<ActionResult> UpdatePictureAsync(IFormFile newPicture)
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);

        if (!ModelState.IsValid) return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));
        if (user == null) return StatusCode(500, "User reference should not be null.");

        //Delete old picture
        if (user.ProfilePicture != null)
        {
            await _blobStorage.DeleteBlobAsync("profilepictures", Path.GetFileName(user.ProfilePicture));
        }

        //Upload new picture
        if (newPicture != null && newPicture.Length > 0)
        {
            var pictureName = user.Id + Path.GetExtension(newPicture.FileName);

            user.ProfilePicture = await _blobStorage.UploadBlobAsync("profilepictures", pictureName, newPicture);
            await _userManager.UpdateAsync(user);
        }

        return Ok(user.ProfilePicture);
    }

    [HttpDelete("DeletePicture")]
    public async Task<ActionResult> DeletePictureAsync()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);

        if (!ModelState.IsValid) return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));
        if (user == null) return StatusCode(500, "User reference should not be null.");

        if (user.ProfilePicture == null) return BadRequest("User does not have a profile picture.");

        //Delete picture
        await _blobStorage.DeleteBlobAsync("profilepictures", Path.GetFileName(user.ProfilePicture));
        user.ProfilePicture = null;
        await _userManager.UpdateAsync(user);

        return NoContent();
    }
}