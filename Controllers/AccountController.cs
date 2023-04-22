namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

using _2cpbackend.Dtos;
using _2cpbackend.Models;
using _2cpbackend.Services;

[ApiController]
[Route("api/[Controller]")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly Random random = new Random();

    public AccountController(UserManager<ApplicationUser> userManager,
                            SignInManager<ApplicationUser> signInManager,
                            IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
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
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailService.SendEmail(data.Email, "Email Confirmation", code);
            await _signInManager.SignInAsync(user, false);
            return Ok();
        }
        
        AddModelErrors(result);
        return data;

    }

    [HttpPost("ConfirmEmail")]
    public async Task<ActionResult<ConfirmEmailDto>> ConfirmEmail(ConfirmEmailDto data)
    {
        if(!ModelState.IsValid) return BadRequest("Invalid email confirmation data.");
        var user = await _userManager.FindByEmailAsync(data.Email);

        if (user == null) return NotFound($"No user with email {data.Email} could be found.");

        var result = await _userManager.ConfirmEmailAsync(user, data.Code);

        if (result.Succeeded) return Ok("email confirmed.");
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

    [HttpPost("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto data)
    {
        var user =  await _userManager.FindByEmailAsync(data.Email);
        if (user != null)
        {
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            //var callbackurl = Url.Action("ResetPassword", "Account", new {userId = user.Id, code = code}, protocol: HttpContext.Request.Scheme);
            await _emailService.SendEmail(data.Email, "Password Reset Code", code);
        }

        return Ok();
    }

    [HttpPost("ResetPassword")]
    public async Task<ActionResult<ResetPasswordDto>> ResetPassword(ResetPasswordDto data)
    {
        if(!ModelState.IsValid) return BadRequest("Invalid password format.");

        var user = await _userManager.FindByEmailAsync(data.Email);

        if (user == null) return NotFound("User not found");

        var result = await _userManager.ResetPasswordAsync(user, data.Code, data.Password);

        if (result.Succeeded) return Ok("Password changed.");
        AddModelErrors(result);
        return data;
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