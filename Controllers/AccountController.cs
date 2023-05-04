namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
    private readonly IBlobStorage _blobStorage;

    public AccountController(UserManager<ApplicationUser> userManager,
                            SignInManager<ApplicationUser> signInManager,
                            IEmailService emailService,
                            IBlobStorage blobStorage)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _blobStorage = blobStorage;
    }

    [HttpPost("Register")]
    public async Task<ActionResult<RegisterDto>> RegisterAsync([FromForm][FromBody] RegisterDto data)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(GetModelErrors(ModelState.Values));
        }

        if((await _userManager.FindByEmailAsync(data.Email)) != null) return Conflict("Email already taken.");
        if((await _userManager.FindByEmailAsync(data.UserName)) != null) return Conflict("UserName already taken.");

        var user = new ApplicationUser
        {
            UserName = data.UserName,
            Email = data.Email,
            BirthDate = data.BirthDate.ToUniversalTime(),
            FirstName = data.FirstName,
            LastName = data.LastName,
            Biography = String.Empty,
            AttendedByUser = new List<Event>(),
            OrganizedByUser = new List<Event>()
        };

        //Uploading profile picture
        if (data.ProfilePictureFile != null && data.ProfilePictureFile.Length > 0)
        {
            var pictureName = user.Id + Path.GetExtension(data.ProfilePictureFile.FileName);
            user.ProfilePicture = await _blobStorage.UploadBlobAsync("profilepictures", pictureName, data.ProfilePictureFile);
        }

        //Creating the user
        var result = await _userManager.CreateAsync(user, data.Password);

        //Sign in after creation
        if (result.Succeeded)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailService.SendEmail(data.Email, "Email Confirmation", code);
            await _signInManager.SignInAsync(user, false);
            return Created("Users/GetUser", new {userId = user.Id});
        }
        
        AddModelErrors(result);
        return StatusCode(500, result);

    }

    [HttpPost("ConfirmEmail")]
    public async Task<ActionResult<ConfirmEmailDto>> ConfirmEmail([FromForm][FromBody] ConfirmEmailDto data)
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
    public async Task<ActionResult<LoginDto>> Login([FromForm][FromBody] LoginDto data)
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
    public async Task<IActionResult> ForgotPassword([FromForm][FromBody] ForgotPasswordDto data)
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
    public async Task<ActionResult<ResetPasswordDto>> ResetPassword([FromForm][FromBody] ResetPasswordDto data)
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