using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rectify.Data;
using Rectify.Models;
using Rectify.Models.ViewModels;

namespace Rectify.Controllers
{
    public class AdminController : Controller
    {

        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext context;
        private readonly IConfiguration configuration;
        private readonly IPasswordValidator<ApplicationUser> passwordValidator;

        public AdminController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration, IPasswordValidator<ApplicationUser> passwordValidator)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.context = context;
            this.configuration = configuration;
            this.passwordValidator = passwordValidator;
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }

        [Authorize]
        public IActionResult ManageUsers()
        {
            var owners = (from u in context.Users
                          join ur in context.UserRoles on u.Id equals ur.UserId
                          join r in context.Roles on ur.RoleId equals r.Id
                          where r.Name == "Owner"
                          select new OwnerUserViewModel
                          {
                              UserId = u.Id,
                              UserName = u.FullName,
                              Email = u.Email,
                              Status = u.Status,
                              BusinessCount = context.CompanyModel.Count(c => c.UserId == u.Id)
                          }).ToList();

            return View(owners);
        }

        [Authorize]
        public IActionResult EditOwners(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var owner = context.Users.FirstOrDefault(u => u.Id == id);

            if (owner == null)
            {
                return NotFound();
            }

            return View(owner); // later you can map to a viewmodel if needed
        }


    }
}
