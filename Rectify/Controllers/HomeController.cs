using System.Diagnostics;
using System.Drawing.Imaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using Rectify.Data;
using Rectify.Models;
using Rectify.Models.ViewModels;

using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Claims;
using System.ComponentModel.Design;


namespace Rectify.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;


        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.context = context;
            this.userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Privacy()
        {
            var user = await userManager.GetUserAsync(User);
            return View(user);
        }

        [Authorize]
        public IActionResult GenerateQrCode(string companyId)
        {
            var url = Url.Action("Contact", "Home", new { companyId }, protocol: Request.Scheme);

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20); // This returns a byte[]

            return File(qrCodeImage, "image/png");
        }

        [Authorize]
        [HttpGet]
        public IActionResult EditDetails(string userId) //ApplicationUser ID
        {
            var user = context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditDetailsViewModel
            {
                UserId = userId,
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                PreferredContact = user.PreferredContact,
                Reports = (bool)user.Reports
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDetails(EditDetailsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            

            var user = userManager.Users.FirstOrDefault(u => u.Id == model.UserId);
            if (user == null)
                return NotFound();

            user.FullName = model.Name;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.PreferredContact = model.PreferredContact;
            user.Reports = model.Reports;

            context.SaveChanges();

            //add in the option to change the image

            return RedirectToAction("Privacy");
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize]
        [HttpGet]
        public IActionResult EditCompanyDetails(int companyID, string userId)
        {
            var company = context.CompanyModel.FirstOrDefault(c => c.Id == companyID);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (company == null)
            {
                return NotFound();
            }
            var viewModel = new CompanyDetailsViewModel
            {
                UserId = currentUserId,
                Company = company
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCompanyDetails(CompanyDetailsViewModel model, IFormFile? logoFile)
        {
            ModelState.Remove("Company.User");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check for duplicate company name AND branch address
            bool isDuplicate = context.CompanyModel
                .Any(c =>
                    c.Id != model.Company.Id && // Exclude the current company
                    c.CompanyName == model.Company.CompanyName &&
                    c.BranchAddress == model.Company.BranchAddress);



            if (isDuplicate)
            {
                TempData["ErrorMessage"] = "A company with the same name and branch address already exists.";
                return View(model);
            }

            var company = context.CompanyModel.FirstOrDefault(c => c.Id == model.Company.Id);
            if (company == null)
                return NotFound();

            company.CompanyName = model.Company.CompanyName;
            company.City = model.Company.City;
            company.BranchAddress = model.Company.BranchAddress;

            if (logoFile != null && logoFile.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    logoFile.CopyTo(ms);
                    company.LogoImage = ms.ToArray();
                }
            }

            context.SaveChanges();

            TempData["SuccessMessage"] = "Company details updated successfully!";

            return RedirectToAction("EditCompanyDetails", new { companyID = company.Id, userId = model.UserId });
        }


        [HttpGet]
        public IActionResult GetUserCompanies(string userId)
        {
            var companies = context.CompanyModel
                .Where(c => c.UserId == userId)
                .Select(c => new { c.Id, c.CompanyName, c.BranchAddress })
                .ToList();

            return Json(companies);
        }



        public IActionResult Contact(int? companyId)
        {
            var companies = context.CompanyModel
                .Include(c => c.User) // Assuming navigation property exists
                .OrderBy(c => c.CompanyName)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.CompanyName} - {c.BranchAddress}"
                })
                .ToList();

            string? logoBase64 = null;
            string? ownerBase64 = null;

            if (companyId.HasValue)
            {
                var selectedCompany = context.CompanyModel
                    .Include(c => c.User)
                    .FirstOrDefault(c => c.Id == companyId.Value);

                if (selectedCompany?.User != null)
                {
                    if (selectedCompany.LogoImage != null)
                        logoBase64 = $"data:image/png;base64,{Convert.ToBase64String(selectedCompany.LogoImage)}";

                    if (selectedCompany.User.OwnerImage != null)
                        ownerBase64 = $"data:image/png;base64,{Convert.ToBase64String(selectedCompany.User.OwnerImage)}";
                }
            }

            var model = new CustomerFeedbackViewModel
            {
                //CompanyBranchOptions = companyUsers,
                CompanyBranchOptions = companies,
                SelectedCompanyId = companyId,
                LogoImageBase64 = logoBase64,
                OwnerImageBase64 = ownerBase64
                
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult GetCompanyImages(int companyId)
        {
            var company = context.CompanyModel
                .Include(c => c.User)
                .FirstOrDefault(c => c.Id == companyId);

            Console.WriteLine($"LogoImage: {(company?.LogoImage != null ? "Yes" : "No")}");
            Console.WriteLine($"OwnerImage: {(company?.User?.OwnerImage != null ? "Yes" : "No")}");

            if (company != null)
            {
                var model = new
                {
                    LogoImageBase64 = company.LogoImage != null ? "data:image/png;base64," + Convert.ToBase64String(company.LogoImage) : null,
                    OwnerImageBase64 = company.User?.OwnerImage != null ? "data:image/png;base64," + Convert.ToBase64String(company.User.OwnerImage) : null
                };

                return Json(model);
            }

            return Json(new { LogoImageBase64 = (string?)null, OwnerImageBase64 = (string?)null });
        }



        [HttpPost]
        public IActionResult Contact(CustomerFeedbackViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdown on postback
                model.CompanyBranchOptions = context.CompanyModel
                    .OrderBy(c => c.CompanyName)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = $"{c.CompanyName} - {c.BranchAddress}"
                    })
                    .ToList();

                return View(model);
            }


            int companyId = model.SelectedCompanyId!.Value;
            DateTime currentDate = DateTime.Now;

            string dateString = currentDate.ToString("yyyyMMdd");
            int ticketCountToday = context.TicketModel.Count(t => t.DateOfMessage.Date == currentDate.Date);
            string ticketNumber = (ticketCountToday + 1).ToString("D5");
            string ticketID = $"{dateString}_{ticketNumber}";

            var ticket = new TicketModel
            {
                TicketID = ticketID,
                CompanyID = companyId, // Or use int if your CompanyID is now int
                DateOfMessage = currentDate,
                Status = "Open",
                DateLastUpdated = currentDate
            };

            context.TicketModel.Add(ticket);

            var customer = new CustomerModel
            {
                TicketID = ticketID,
                CustomerName = model.CustomerName,
                CustomerEmail = model.Email,
                CustomerPhone = model.PhoneNumber,
                Message = model.Message
            };

            context.CustomerModel.Add(customer);
            context.SaveChanges();

            return RedirectToAction("ThankYou");
        }

        public IActionResult ThankYou()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult Reports(int companyID, string userId)
        {

            var company = context.CompanyModel.FirstOrDefault(c => c.Id == companyID);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (company == null)
            {
                return NotFound();
            }
            var viewModel = new CompanyDetailsViewModel
            {
                UserId = currentUserId,
                Company = company
            };

            return View(viewModel);
        }

    }
}
