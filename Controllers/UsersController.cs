namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

    [AllowAnonymous]
    [HttpGet("GetUserDetails")]
    public async Task<ActionResult> GetUserDetailsAsync(string subjectId)
    {
        var user = await _userManager.FindByIdAsync(subjectId);

        if (user == null) return NotFound();
        
        var resource = new UserDetailsDto
        {
            Id = user.Id,
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
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

        if (!ModelState.IsValid) return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));

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
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

        if (!ModelState.IsValid) return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));

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
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

        if (!ModelState.IsValid) return BadRequest(ModelUtils.GetModelErrors(ModelState.Values));

        if (user.ProfilePicture == null) return BadRequest("User does not have a profile picture.");

        //Delete picture
        await _blobStorage.DeleteBlobAsync("profilepictures", Path.GetFileName(user.ProfilePicture));
        user.ProfilePicture = null;
        await _userManager.UpdateAsync(user);

        return NoContent();
    }

    [HttpPost("FollowUser")]
    public async Task<ActionResult> FollowUserAsync(string subjectId)
    {
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

        user = await _userManager.Users
        .Include(u => u.Following)
        .SingleOrDefaultAsync(u => u.Id == user.Id);
        if (user == null) return StatusCode(500, "User reference not expected to be null.");

        //Get subject to follow
        var subject = await _userManager.Users
        .Include(s => s.Followers)
        .SingleOrDefaultAsync(s => s.Id == subjectId);
        if (subject == null) return NotFound();

        //Subject already followed?
        if (user.Following.Contains(subject)) return BadRequest("User followed before.");

        //User same as subject?
        if (user.Id == subject.Id) return BadRequest("Users cannot follow themselves");

        //Follow operation
        user.Following.Add(subject);
        subject.Followers.Add(user);

        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("UnfollowUser")]
    public async Task<ActionResult> UnfollowUserAsync(string subjectId)
    {
        //Getting current user
        var userName = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        if (userName == null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);

        if (user == null) return StatusCode(500, "User reference should not be null");

        user = await _userManager.Users
        .Include(u => u.Following)
        .SingleOrDefaultAsync(u => u.Id == user.Id);
        if (user == null) return StatusCode(500, "User reference not expected to be null.");

        //Get subject to follow
        var subject = await _userManager.Users
        .Include(s => s.Followers)
        .SingleOrDefaultAsync(s => s.Id == subjectId);
        if (subject == null) return NotFound();

        //Subject already followed?
        if (!user.Following.Contains(subject)) return BadRequest("User not followed before.");

        //Unfollow operation
        user.Following.Remove(subject);
        subject.Followers.Remove(user);

        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("FollowersList")]
    public async Task<ActionResult<List<UserDetailsDto>>> GetFollowersListAsync(string userId)
    {
        //Get user reference
        var user = await _context.ApplicationUsers
        .Include(u => u.Followers)
        .SingleOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        var data = new List<UserDetailsDto>();
        foreach (ApplicationUser follower in user.Followers)
        {
            var resource = new UserDetailsDto
            {
                Id = user.Id,
                FirstName = follower.FirstName,
                LastName = follower.LastName,
                Email = follower.Email,
                PhoneNumber = follower.PhoneNumber,
                Biography = follower.Biography,
                UserName = follower.UserName
            };

            //Adding profile picture link
            if (follower.ProfilePicture == null) resource.ProfilePictureUrl = null;
            else resource.ProfilePictureUrl = user.ProfilePicture;
            data.Add(resource);
        }

        return Ok(data);
    }
    

    [HttpGet("FollowingList")]
    public async Task<ActionResult<List<UserDetailsDto>>> GetFollowingListAsync(string userId)
    {
        //Get user reference
        var user = await _context.ApplicationUsers
        .Include(u => u.Following)
        .SingleOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        var data = new List<UserDetailsDto>();
        foreach (ApplicationUser follower in user.Following)
        {
            var resource = new UserDetailsDto
            {
                Id = user.Id,
                FirstName = follower.FirstName,
                LastName = follower.LastName,
                Email = follower.Email,
                PhoneNumber = follower.PhoneNumber,
                Biography = follower.Biography,
                UserName = follower.UserName
            };

            //Adding profile picture link
            if (follower.ProfilePicture == null) resource.ProfilePictureUrl = null;
            else resource.ProfilePictureUrl = user.ProfilePicture;
            data.Add(resource);
        }

        return Ok(data);
    }

}