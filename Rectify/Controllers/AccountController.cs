using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rectify.Data;
using Rectify.Models;
using Rectify.Models.ViewModels;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Rectify.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext context;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Email or Password is incorrect");
                    return View(model);
                }
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(CompanyRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Store Step 1 data in TempData (as JSON)
                TempData["RegisterData"] = JsonSerializer.Serialize(model);
                TempData.Keep("RegisterData");
                return RedirectToAction("RegisterNext");
            }

            return View(model);
        }

        public IActionResult RegisterNext()
        {
            if (!TempData.ContainsKey("RegisterData"))
                return RedirectToAction("Register");

            var model = JsonSerializer.Deserialize<CompanyRegisterViewModel>((string)TempData["RegisterData"]);
            TempData.Keep("RegisterData");
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> RegisterNext(CompanyRegisterViewModel model)
        {
            if (!TempData.ContainsKey("RegisterData"))
                return RedirectToAction("Register");

            var step1Data = JsonSerializer.Deserialize<CompanyRegisterViewModel>((string)TempData["RegisterData"]);

            // Copy step 1 data back into final model
            model.Name = step1Data!.Name;
            model.CompanyName = step1Data.CompanyName;
            model.Email = step1Data.Email;
            model.PhoneNumber = step1Data.PhoneNumber;
            model.Password = step1Data.Password;
            model.ConfirmPassword = step1Data.ConfirmPassword;

            if (!ModelState.IsValid)
            {
                TempData.Keep("RegisterData");
                return View(model);
            }

            // Check uniqueness: no duplicate CompanyName+BranchAddress
            var exists = context.Users.Any(u =>
                u.CompanyName == model.CompanyName &&
                u.BranchAddress == model.BranchAddress);

            if (exists)
            {
                ModelState.AddModelError("BranchAddress", "This company already exists in that location.");
                TempData.Keep("RegisterData");
                return View(model);
            }

            // Create user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.Name,
                CompanyName = model.CompanyName,
                PhoneNumber = model.PhoneNumber,
                PreferredContact = model.PrefferedContact,
                City = model.City,
                BranchAddress = model.BranchAddress,
                Reports = model.Reports
            };

            if (model.LogoImageFile != null)
            {
                using var ms = new MemoryStream();
                await model.LogoImageFile.CopyToAsync(ms);
                user.LogoImage = ms.ToArray();
            }

            if (model.OwnerImageFile != null)
            {
                using var ms = new MemoryStream();
                await model.OwnerImageFile.CopyToAsync(ms);
                user.OwnerImage = ms.ToArray();
            }

            var result = await userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            TempData.Keep("RegisterData");
            return View(model);
        }


        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task <IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Something is wrong!");
                    return View(model);
                }
                else
                {
                    return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
                }
            }


            return View(model);
        }

        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View (new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Email);
                if (user != null)
                {
                    var result = await userManager.RemovePasswordAsync(user);
                    if (result.Succeeded)
                    {
                        result = await userManager.AddPasswordAsync(user, model.NewPassword);
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }

                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Email not found!");
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", "Something went wrong. Try again.");
                return View(model);
            }
        } 

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
