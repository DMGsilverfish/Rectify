using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rectify.Controllers
{
    public class AdminController : Controller
    {
        [Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult ManageUsers()
        {
            return View();
        }
    }
}
