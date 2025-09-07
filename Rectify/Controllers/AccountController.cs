using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rectify.Data;
using Rectify.Models;
using Rectify.Models.ViewModels;
using System.Security.Claims;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Rectify.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext context;
        private readonly IConfiguration configuration;
        private readonly IPasswordValidator<ApplicationUser> passwordValidator;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration, IPasswordValidator<ApplicationUser> passwordValidator)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.context = context;
            this.configuration = configuration;
            this.passwordValidator = passwordValidator;
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
                    // get the logged-in user
                    var user = await userManager.FindByEmailAsync(model.Email);

                    // check if user is in Admin role
                    if (await userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }

                    // default redirect for non-admins
                    return RedirectToAction("Privacy", "Home");
                }          
                ModelState.AddModelError("", "Email or Password is incorrect");     
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/")
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/", string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return RedirectToAction("Login");
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            // If the user does not have an account, we create one
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email != null)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email
                    };
                    var createResult = await userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        ModelState.AddModelError(string.Empty, "Failed to create user.");
                        return RedirectToAction("Login");
                    }

                    await userManager.AddLoginAsync(user, info);
                }

                await signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            // If email not found
            return RedirectToAction("Login");
        }


        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Privacy", "Home");
            }
        }


        public IActionResult Register()
        {
            TempData["AdminPassword"] = "@RectifyAccounts1234";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(CompanyRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Validate password with Identity rules
            var dummyUser = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var passwordValidationResult = await passwordValidator.ValidateAsync(userManager, dummyUser, model.Password!);

            if (!passwordValidationResult.Succeeded)
            {
                foreach (var error in passwordValidationResult.Errors)
                {
                    ModelState.AddModelError("Password", error.Description);
                }
                return View(model);
            }

            // Store Step 1 data
            TempData["RegisterData"] = JsonSerializer.Serialize(model);
            TempData.Keep("RegisterData");
            return RedirectToAction("RegisterNext");
        }


        public IActionResult RegisterNext()
        {
            if (!TempData.ContainsKey("RegisterData"))
                return RedirectToAction("Register");

            var model = JsonSerializer.Deserialize<CompanyRegisterViewModel>((string)TempData["RegisterData"]!);
            TempData.Keep("RegisterData");
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> RegisterNext(CompanyRegisterViewModel model)
        {
            if (!TempData.ContainsKey("RegisterData"))
                return RedirectToAction("Register");

            var step1Data = JsonSerializer.Deserialize<CompanyRegisterViewModel>((string)TempData["RegisterData"]!);

            // Copy step 1 data back into final model
            model.Name = step1Data!.Name;
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
            var exists = context.CompanyModel.Any(c =>
                c.CompanyName == model.CompanyName &&
                c.BranchAddress == model.BranchAddress);

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
                FullName = model.Name!,
                PhoneNumber = model.PhoneNumber,
                PreferredContact = model.PrefferedContact,
                Reports = model.Reports,
                Status = "Pending",

            };
            var result = await userManager.CreateAsync(user, model.Password!);

            var company = new CompanyModel
            {
                //Id = nextCompany,
                CompanyName = model.CompanyName!,
                City = model.City!,
                BranchAddress = model.BranchAddress!,
                UserId = user.Id,
            };

            if (model.LogoImageFile != null)
            {
                using var ms = new MemoryStream();
                await model.LogoImageFile.CopyToAsync(ms);
                company.LogoImage = ms.ToArray();
            }

            if (model.OwnerImageFile != null)
            {
                using var ms = new MemoryStream();
                await model.OwnerImageFile.CopyToAsync(ms);
                user.OwnerImage = ms.ToArray();
            }

            
            if (result.Succeeded)
            {
                var roleName = "Owner-Pending";
                await userManager.AddToRoleAsync(user, roleName);
                
                
                context.CompanyModel.Add(company);
                await context.SaveChangesAsync();


                await signInManager.SignInAsync(user, false);
                return RedirectToAction("Privacy", "Home");
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

        // Show empty form
        [HttpGet]
        public IActionResult AddCompany()
        {
            return View();
        }

        // Handle form post
        [HttpPost]
        public async Task<IActionResult> AddCompany(CompanyViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var company = new CompanyModel
            {
                CompanyName = model.CompanyName,
                City = model.City,
                BranchAddress = model.BranchAddress,
                UserId = user.Id
            };

            if (model.LogoImageFile != null)
            {
                using var ms = new MemoryStream();
                await model.LogoImageFile.CopyToAsync(ms);
                company.LogoImage = ms.ToArray();
            }

            context.CompanyModel.Add(company);
            await context.SaveChangesAsync();

            return RedirectToAction("Privacy", "Home");
        }


    }
}
