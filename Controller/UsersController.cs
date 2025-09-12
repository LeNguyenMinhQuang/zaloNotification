using Microsoft.AspNetCore.Mvc;
using SigmaNotificationBackend.Models;

namespace SigmaNotificationBackend.Controllers
{
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("GetCurrentUser")]
    [Route("api/Users/GetCurrentUser()")]
    public IActionResult GetCurrentUser()
        {
            var user = HttpContext.Items["CurrentUser"] as SVN_Users;

            if (user == null)
                return Unauthorized("User not found");

            return Ok(new
            {
                user.id_user,
                user.svn_number,
                user.name_user,
                user.img_user,
                user.id_department,
                name_department = user.Department?.name_department
            });
        }
}


}