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
            var emailCount = context.Users
                .Count(u => u.Status == "Active" && u.PreferredContact == "Email");

            var whatsappCount = context.Users
                .Count(u => u.Status == "Active" && u.PreferredContact == "WhatsApp");

            var vm = new AdminDashboardViewModel
            {
                EmailCount = emailCount,
                WhatsappCount = whatsappCount
            };

            return View(vm);
        }

        [Authorize]
        public IActionResult ManageUsers()
        {
            var owners = (from u in context.Users
                          join ur in context.UserRoles on u.Id equals ur.UserId
                          join r in context.Roles on ur.RoleId equals r.Id
                          where r.Name != "Admin" 
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

            var statuses = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                System.IO.File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Data", "statuses.json"))
            );

            var vm = new EditOwnerStatusViewModel
            {
                Id = owner.Id,
                UserName = owner.FullName,
                Email = owner.Email,
                Status = owner.Status,
                AvailableStatuses = statuses
            };

            return View(vm); // later you can map to a viewmodel if needed
        }

        [HttpPost]
        [Authorize]
        public IActionResult EditOwners(EditOwnerStatusViewModel model)
        {
            var owner = context.Users.FirstOrDefault(u => u.Id == model.Id);

            if (owner == null)
            {
                return NotFound();
            }

            // Update the status field
            owner.Status = model.Status;

            // If status was changed to Active, ensure role is updated
            if (model.Status == "Active")
            {
                // Get role IDs
                var ownerRole = context.Roles.FirstOrDefault(r => r.Name == "Owner");
                var pendingRole = context.Roles.FirstOrDefault(r => r.Name == "Owner-Pending");

                if (ownerRole != null)
                {
                    // Find current roles for this user
                    var userRoles = context.UserRoles.Where(ur => ur.UserId == owner.Id).ToList();

                    // Remove "Owner-Pending" if present
                    if (pendingRole != null)
                    {
                        var pendingRoleEntry = userRoles.FirstOrDefault(ur => ur.RoleId == pendingRole.Id);
                        if (pendingRoleEntry != null)
                        {
                            context.UserRoles.Remove(pendingRoleEntry);
                        }
                    }

                    // Add "Owner" role if not already assigned
                    if (!userRoles.Any(ur => ur.RoleId == ownerRole.Id))
                    {
                        context.UserRoles.Add(new IdentityUserRole<string>
                        {
                            UserId = owner.Id,
                            RoleId = ownerRole.Id
                        });
                    }
                }
            }

            context.SaveChanges();

            return RedirectToAction("ManageUsers");
        }

        //[Authorize]
        public IActionResult AddAdmin()
        {
            return View();
        }



    }
}
