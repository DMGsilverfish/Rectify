using Microsoft.AspNetCore.Mvc;

namespace Rectify.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
