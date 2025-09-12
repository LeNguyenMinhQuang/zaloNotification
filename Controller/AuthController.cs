using Microsoft.AspNetCore.Mvc;
using SigmaNotificationBackend.Models;
using SigmaNotificationBackend.Services;

namespace SigmaNotificationBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [Route("api/Auth/Login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var (token, user) = _authService.Authenticate(request);

            if (token == null || user == null)
            {
                return Unauthorized("Invalid credentials");
            }

            return Ok(new
            {
                access_token = token
            });
        }
    }
}
