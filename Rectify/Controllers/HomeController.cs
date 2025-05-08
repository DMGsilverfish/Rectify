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
                return Error();
            }

            var model = new EditDetailsViewModel
            {
                UserId = userId,
                Name = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
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

            var user = context.Users.FirstOrDefault(u => u.Id == model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            user.FullName = model.Name;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            context.SaveChanges();

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

            var model = new CustomerFeedbackViewModel
            {
                CompanyBranchOptions = companyUsers,
                SelectedUserId = userId
            };

            return View(model);
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
