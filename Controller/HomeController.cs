using Microsoft.AspNetCore.Mvc;

namespace SigmaNotificationBackend.Controllers
{
    public class HomeController : Controller
    {
        // GET: /HelloWorld/
        public IActionResult Index()
        {
            return View();
        }
    }
}