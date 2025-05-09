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
        public IActionResult GenerateQrCode(string userId)
        {
            var url = Url.Action("Contact", "Home", new { userId }, protocol: Request.Scheme);

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20); // This returns a byte[]

            return File(qrCodeImage, "image/png");
        }

        [Authorize]
        [HttpGet]
        public IActionResult EditDetails(string userId)
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
                City = user.City,
                BranchAddress = user.BranchAddress,
                CompanyName = user.CompanyName,
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
            var duplicate = userManager.Users.Any(u =>
                u.Id != model.UserId &&
                u.CompanyName == model.CompanyName &&
                u.BranchAddress == model.BranchAddress);

            if (duplicate)
            {
                ModelState.AddModelError("", "A company with that name and branch address already exists.");
                return View(model);
            }

            var user = userManager.Users.FirstOrDefault(u => u.Id == model.UserId);
            if (user == null)
                return NotFound();

            user.FullName = model.Name;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.City = model.City;
            user.BranchAddress = model.BranchAddress;
            user.CompanyName = model.CompanyName;
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

        


        public IActionResult Contact(string? userId)
        {
            var companyUsers = userManager.Users
                .Where(u => u.CompanyName != null && u.BranchAddress != null)
                .OrderBy(u => u.CompanyName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.CompanyName} - {u.BranchAddress}"
                })
                
                .ToList();

            string? logoBase64 = null;
            string? ownerBase64 = null;

            if (!string.IsNullOrEmpty(userId))
            {
                var selectedUser = userManager.Users.FirstOrDefault(u => u.Id == userId);

                if (selectedUser != null)
                {
                    if (selectedUser.LogoImage != null)
                        logoBase64 = $"data:image/png;base64,{Convert.ToBase64String(selectedUser.LogoImage)}";

                    if (selectedUser.OwnerImage != null)
                        ownerBase64 = $"data:image/png;base64,{Convert.ToBase64String(selectedUser.OwnerImage)}";
                }
            }

            var model = new CustomerFeedbackViewModel
            {
                CompanyBranchOptions = companyUsers,
                SelectedUserId = userId,
                LogoImageBase64 = logoBase64,
                OwnerImageBase64 = ownerBase64
                
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult GetCompanyImages(string userId)
        {
            // Get the user from the database using the userId
            var company = userManager.Users.FirstOrDefault(u => u.Id == userId);

            Console.WriteLine($"LogoImage: {(company.LogoImage != null ? "Yes" : "No")}");
            Console.WriteLine($"OwnerImage: {(company.OwnerImage != null ? "Yes" : "No")}");


            if (company != null)
            {
                var model = new
                {
                    LogoImageBase64 = company.LogoImage != null ? "data:image/png;base64," + Convert.ToBase64String(company.LogoImage) : null,
                    OwnerImageBase64 = company.OwnerImage != null ? "data:image/png;base64," + Convert.ToBase64String(company.OwnerImage) : null
                };
                return Json(model);
            }

            return Json(new { LogoImageBase64 = (string)null, OwnerImageBase64 = (string)null });
        }


        [HttpPost]
        public IActionResult Contact(CustomerFeedbackViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdown on postback
                model.CompanyBranchOptions = userManager.Users
                    .Where(u => u.CompanyName != null && u.BranchAddress != null)
                    .OrderBy(u => u.CompanyName)
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = $"{u.CompanyName} - {u.BranchAddress}"
                    })
                    .ToList();

                return View(model);
            }


            string companyID = model.SelectedUserId!;
            string name = model.CustomerName;
            string email = model.Email;
            string phone = model.PhoneNumber;
            string message = model.Message;
            DateTime currentDate = DateTime.Now;

            string dateString = currentDate.ToString("yyyyMMdd");

            int ticketCountToday = context.TicketModel.Count(t => t.DateOfMessage.Date == currentDate.Date);
            string ticketNumber = (ticketCountToday + 1).ToString("D5"); // 5-digit padded
            string ticketID = $"{dateString}_{ticketNumber}";

            var ticket = new TicketModel
            {
                TicketID = ticketID,
                CompanyID = companyID,
                DateOfMessage = currentDate,
                Status = "Open",
                DateLastUpdated = currentDate
            };

            context.TicketModel.Add(ticket);

            var customer = new CustomerModel
            {
                TicketID = ticketID,
                CustomerName = name,
                CustomerEmail = email,
                CustomerPhone = phone,
                Message = message
            };

            context.CustomerModel.Add(customer);
            context.SaveChanges();


            return RedirectToAction("ThankYou");
        }

        public IActionResult ThankYou()
        {
            return View();
        }

    }
}
