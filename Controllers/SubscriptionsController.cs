namespace _2cpbackend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    [HttpGet]
    public ActionResult Index()
    {
        return Ok();
    }
}