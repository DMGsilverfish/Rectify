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

using System.Net;
using System.Net.Mail;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;


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
            TempData.Clear();
            var user = await userManager.GetUserAsync(User);

            // Get all company IDs linked to the user (assumes such a relationship)
            var companies = await context.CompanyModel
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            var companyIds = companies.Select(c => c.Id).ToList();
            bool showCompanyInfo = companies.Count > 1;

            // Join Tickets and Customers
            var ticketData = await (from t in context.TicketModel
                                    join c in context.CustomerModel on t.TicketID equals c.TicketID
                                    join comp in context.CompanyModel on t.CompanyID equals comp.Id
                                    where companyIds.Contains(t.CompanyID)
                                    select new TicketDisplayModel
                                    {
                                        TicketID = t.TicketID,
                                        CompanyName = comp.CompanyName,
                                        BranchAddress = comp.BranchAddress,
                                        DateOfMessage = t.DateOfMessage,
                                        Status = t.Status,
                                        Message = c.Message
                                    }).ToListAsync();

            var viewModel = new TicketingViewModel
            {
                User = user,
                Tickets = ticketData,
                ShowCompanyInfo = showCompanyInfo
            };

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> FilterTickets(DateTime? startDate, DateTime? endDate, string sortOrder = "desc", [FromQuery] List<string> statuses = null)
        {
            var user = await userManager.GetUserAsync(User);

            var companies = await context.CompanyModel
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            


            var companyIds = companies.Select(c => c.Id).ToList();

            var ticketsQuery = from t in context.TicketModel
                               join c in context.CustomerModel on t.TicketID equals c.TicketID
                               join comp in context.CompanyModel on t.CompanyID equals comp.Id
                               where companyIds.Contains(t.CompanyID)
                               select new
                               {
                                   t.TicketID,
                                   comp.CompanyName,
                                   comp.BranchAddress,
                                   t.DateOfMessage,
                                   t.Status,
                                   c.Message
                               };

            if (statuses != null && statuses.Any())
                ticketsQuery = ticketsQuery.Where(t => statuses.Contains(t.Status));

            if (startDate.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.DateOfMessage >= startDate.Value);

            if (endDate.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.DateOfMessage < endDate.Value.AddDays(1));

            ticketsQuery = sortOrder == "asc"
                ? ticketsQuery.OrderBy(t => t.DateOfMessage)
                : ticketsQuery.OrderByDescending(t => t.DateOfMessage);

            var filteredTickets = await ticketsQuery.ToListAsync();

            return Json(filteredTickets);
        }

        [Authorize]
        [HttpPost]
        public IActionResult UpdateStatus(string ticketId, string status)
        {
            var ticket = context.TicketModel.FirstOrDefault(t => t.TicketID == ticketId);
            if (ticket == null)
            {
                TempData["ErrorMessage"] = $"Ticket {ticketId} was not found.";
                return RedirectToAction("Privacy");
            }


            if (ticket.Status != status)
            {
                ticket.Status = status;
                ticket.DateLastUpdated = DateTime.Now;

                context.SaveChanges();
                TempData["SuccessMessage"] = $"Status for Ticket {ticketId} updated successfully.";
            }
            else
            {
                TempData["WarningMessage"] = $"No changes made. Ticket {ticketId} is already marked as '{status}'.";
            }

            return RedirectToAction("Privacy");
        }

        [Authorize]
        public IActionResult GenerateQrCode(string companyId)
        {
            if (string.IsNullOrEmpty(companyId))
                return BadRequest("Company ID is missing.");

            var url = Url.Action("ContactStep1", "Feedback", new { companyId }, protocol: Request.Scheme);
            if (string.IsNullOrEmpty(url))
                return BadRequest("Could not generate URL.");

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20); // byte[]
            if (qrCodeBytes == null || qrCodeBytes.Length == 0)
                return StatusCode(500, "QR code generation failed.");

            // Load original bitmap (indexed pixel format)
            using var qrStream = new MemoryStream(qrCodeBytes);
            using var originalQrBitmap = new Bitmap(qrStream);

            // Create a new bitmap with pixel format that supports drawing
            using var qrBitmap = new Bitmap(originalQrBitmap.Width, originalQrBitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // Copy original bitmap into new bitmap
            using (var g = Graphics.FromImage(qrBitmap))
            {
                g.DrawImage(originalQrBitmap, 0, 0);
            }

            // Load logo
            string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "LogoPlaceholder.jpeg");
            using var logo = new Bitmap(logoPath);

            // Resize logo if needed
            int logoSize = qrBitmap.Width / 5;
            using var resizedLogo = new Bitmap(logo, new Size(logoSize, logoSize));

            // Overlay logo in center
            using var graphics = Graphics.FromImage(qrBitmap);
            int x = (qrBitmap.Width - logoSize) / 2;
            int y = (qrBitmap.Height - logoSize) / 2;
            graphics.DrawImage(resizedLogo, x, y, logoSize, logoSize);

            // Save final image to byte[]
            using var output = new MemoryStream();
            qrBitmap.Save(output, System.Drawing.Imaging.ImageFormat.Png);

            return File(output.ToArray(), "image/png","QRCode.png");
        }

        




        [Authorize]
        public IActionResult PrintQRCode(string companyId)
        {
            ViewBag.CompanyID = companyId;
            return View();
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

            //insert email and whatsapp code
            // Step 1: Get the UserId from the CompanyModel
            string? userId = context.CompanyModel
                .Where(c => c.Id == companyId)
                .Select(c => c.UserId)
                .FirstOrDefault();

            string? preferredContact = null;

            // Step 2: Use the UserId to fetch PreferredContact from ApplicationUser
            if (!string.IsNullOrEmpty(userId))
            {
                preferredContact = context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.PreferredContact)
                    .FirstOrDefault();
            }

            var company = context.CompanyModel
                .Include(c => c.User) // Include the related ApplicationUser
                .FirstOrDefault(c => c.Id == companyId);

            if (preferredContact == "Email")
            {
                var subject = $"Customer Query ({company.CompanyName}) - Rectify";
                var body = $"Name: {model.CustomerName} \n" +
                           $"Email: {model.Email} \n" +
                           $"Phone: {model.PhoneNumber} \n" +
                           $"Message: \n\n{model.Message}";

                try
                {
                    SendEmail(company.User.Email, subject, body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending email: " + ex.Message);
                    // Optional: Add error feedback to ModelState
                }
            }
            else if (preferredContact == "WhatsApp")
            {

            } else
            {
                //return error
            }


                return RedirectToAction("ThankYou");
        }

        public IActionResult ThankYou()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult Reports()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var companies = context.CompanyModel
                .Where(c => c.UserId == currentUserId)
                .ToList();

            if (!companies.Any())
            {
                return NotFound();
            }

            var companyIds = companies.Select(c => c.Id).ToList();
            var now = DateTime.Now;

            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);
            var endOfLastMonth = startOfThisMonth.AddDays(-1);

            // All tickets this user has access to
            var allTickets = context.TicketModel
                .Where(t => companyIds.Contains(t.CompanyID))
                .ToList();

            var currentMonthTickets = allTickets
                .Where(t => t.DateOfMessage >= startOfThisMonth)
                .ToList();

            var lastMonthTickets = allTickets
                .Where(t => t.DateOfMessage >= startOfLastMonth && t.DateOfMessage < startOfThisMonth)
                .ToList();

            var statuses = new[] { "Open", "Pending", "Closed" };
            var statCards = new List<TicketStatsCard>();

            foreach (var status in statuses)
            {
                var totalCurrent = currentMonthTickets.Count(t => t.Status == status);
                var totalPrevious = lastMonthTickets.Count(t => t.Status == status);

                double? average = companies.Count > 1
                    ? Math.Round((double)totalCurrent / companies.Count, 1)
                    : null;

                double percentChange;
                if (totalPrevious > 0)
                {
                    percentChange = ((double)(totalCurrent - totalPrevious) / totalPrevious) * 100;
                }
                else if (totalCurrent > 0)
                {
                    percentChange = 100; // All new
                }
                else
                {
                    percentChange = 0;
                }

                statCards.Add(new TicketStatsCard
                {
                    Title = $"{status} Tickets",
                    Total = totalCurrent,
                    Average = average,
                    PercentChange = Math.Round(percentChange, 1)
                });
            }

            var viewModel = new ReportsViewModel
            {
                UserId = currentUserId,
                Companies = companies,
                Company = companies.First(),
                StatCards = statCards
            };

            return View(viewModel);
        }






        private void SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var smtpServer = configuration["Gmail:SmtpServer"];
                var port = int.Parse(configuration["Gmail:Port"]!);
                var fromEmail = configuration["Gmail:Username"];
                var password = configuration["Gmail:Password"];

                using (var client = new SmtpClient(smtpServer, port))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(fromEmail, password);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail!),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false,
                    };
                    mailMessage.To.Add(toEmail);
                    mailMessage.Headers.Add("X-Priority", "3");          // 3 = Normal priority
                    mailMessage.Headers.Add("X-MSMail-Priority", "Normal");
                    mailMessage.Headers.Add("Importance", "Normal");




                    client.Send(mailMessage);
                }
            }
            catch (SmtpException ex)
            {
                Console.WriteLine("SMTP Error: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                }
            }

            catch (Exception ex)
            {
                // Handle any other exceptions
                Console.WriteLine("General Error: " + ex.Message);
            }
        }

    }
}
