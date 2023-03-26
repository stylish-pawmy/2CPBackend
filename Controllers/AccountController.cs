namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

using _2cpbackend.Dtos;
using _2cpbackend.Models;

[ApiController]
[Route("api/[Controller]")]
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("Register")]
    public async Task<ActionResult<RegisterDto>> Register(RegisterDto data)
    {
        if (!ModelState.IsValid) return BadRequest("Invalid registration data.");

        var user = new ApplicationUser
        {
            UserName = data.UserName,
            Email = data.Email,
            BirthDate = data.BirthDate.ToUniversalTime(),
            FirstName = data.FirstName,
            LastName = data.LastName
        };

        //Creating the user
        var result = await _userManager.CreateAsync(user, data.Password);

        //Sign in after creation
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, false);
            return Ok();
        }
        
        AddModelErrors(result);
        return data;

    }

    [HttpGet("LogOff")]
    public async Task<ActionResult> LogOff()
    {
        await _signInManager.SignOutAsync();
        return Ok();
    }

    [HttpPost("Login")]
    public async Task<ActionResult<LoginDto>> Login(LoginDto data)
    {
        if (!ModelState.IsValid) return BadRequest("Invalid login data.");
        //Finding the user
        var user = _userManager.Users.SingleOrDefault(u => u.Email == data.Identifier);
        if (user == null) user = _userManager.Users.SingleOrDefault(u => u.UserName == data.Identifier);
        if (user == null) user = _userManager.Users.SingleOrDefault(u => u.PhoneNumber == data.Identifier);

        //Signing in
        if (user == null) return NotFound("User not found"); 
        var result = await _signInManager.PasswordSignInAsync(user, data.Password, data.RememberMe, false);

        if (result.Succeeded) return Ok("User logged in.");
        return BadRequest("Invalid login attempt");
    }

    //Utilities
    [NonAction]
    public void AddModelErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}