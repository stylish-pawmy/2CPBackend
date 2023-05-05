namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.Text;

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
    private readonly IConfiguration _configuration;

    public AccountController(UserManager<ApplicationUser> userManager,
                            SignInManager<ApplicationUser> signInManager,
                            IEmailService emailService,
                            IBlobStorage blobStorage,
                            IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _blobStorage = blobStorage;
        _configuration = configuration;
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
    

    [HttpPost("Authenticate")]
    public async Task<ActionResult<string>> Authenticate([FromForm][FromBody] LoginDto data)
    {
        if (!ModelState.IsValid) return BadRequest("Invalid login data.");
        //Finding the user
        var user = _userManager.Users.SingleOrDefault(u => u.Email == data.Identifier);
        if (user == null) user = _userManager.Users.SingleOrDefault(u => u.UserName == data.Identifier);
        if (user == null) user = _userManager.Users.SingleOrDefault(u => u.PhoneNumber == data.Identifier);

        if (user == null) return NotFound("User not found"); 
        if (!await _userManager.CheckPasswordAsync(user, data.Password)) return Unauthorized("Wrong user credentials.");
        
        //Getting Signing credentials
        var keyString = _configuration["Jwt:Key"];

        if (keyString == null) keyString = "";

        var key = Encoding.UTF8.GetBytes(keyString);
        var secret = new SymmetricSecurityKey(key);
        var credentials = new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);

        //Getting user claims
        if (user.UserName == null) return StatusCode(500, "Username could not be resolved.");
        var tokenClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName)
        };

        var tokenOption = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["jwt:Audience"],
            claims: tokenClaims,
            expires: DateTime.Now.AddDays(3),
            signingCredentials: credentials
        );

        return Ok(new JwtSecurityTokenHandler().WriteToken(tokenOption));

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