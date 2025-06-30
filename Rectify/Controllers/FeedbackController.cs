// Controller (FeedbackController.cs)

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rectify.Data;
using Rectify.Models;
using Rectify.Models.ViewModels;
using System;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.ComponentModel.Design;

public class FeedbackController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public FeedbackController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult ContactStep0()
    {
        
        var companies = _context.CompanyModel
            .Include(c => c.User)
            .OrderBy(c => c.CompanyName)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.CompanyName} - {c.BranchAddress}"
            }).ToList();

        companies.Add(new SelectListItem
        {
            Value = "-1",
            Text = "Other"
        });

        var viewModel = new CustomerFeedbackViewModel
        {
            CompanyBranchOptions = companies
        };

        if (TempData.ContainsKey("SelectedCompanyId"))
        {
            viewModel.SelectedCompanyId = (int?)TempData["SelectedCompanyId"];
            TempData.Keep("SelectedCompanyId");
        }

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult ContactStep0(CustomerFeedbackViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CompanyBranchOptions = _context.CompanyModel
                .OrderBy(c => c.CompanyName)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CompanyName + " - " + c.BranchAddress
                }).ToList();
            return View(model);
        }

        if (model.SelectedCompanyId == -1)
        {
            return RedirectToAction("ContactStepOther");
        }

        return RedirectToAction("ContactStep1", new { companyId = model.SelectedCompanyId });
    }

    [HttpGet]
    public IActionResult ContactStepOther()
    {
        return View();
    }

    [HttpPost]
    public IActionResult ContactStepOther(CustomerFeedbackViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        // Save the "Other" company information to TempData
        TempData["SelectedCompanyId"] = -1; // Indicating "Other"

        TempData["CompanyName"] = model.CompanyName;
        return RedirectToAction("ContactStep1", new { companyId = -1, companyName = model.CompanyName });
    }


    [HttpGet]
    public IActionResult ContactStep1(int? companyId, string? companyName)
    {
        //https://localhost:7192/Feedback/ContactStep1?companyId=5

        // First, try TempData if companyId isn't passed via query
        if (!companyId.HasValue && TempData.ContainsKey("SelectedCompanyId"))
        {
            companyId = (int?)TempData["SelectedCompanyId"];
        }

        var companies = _context.CompanyModel
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
        string? ownername = null;
        

        if (companyId.HasValue)
        {
            var selectedCompany = _context.CompanyModel
                .Include(c => c.User)
                .FirstOrDefault(c =>  c.Id == companyId.Value);

            if (selectedCompany?.User != null)
            {
                if (selectedCompany.LogoImage != null)
                    logoBase64 = $"data:image/png;base64,{Convert.ToBase64String(selectedCompany.LogoImage)}";

                if (selectedCompany.User.OwnerImage != null)
                    ownerBase64 = $"data:image/png;base64,{Convert.ToBase64String(selectedCompany.User.OwnerImage)}";
                
                ownername = selectedCompany.User.FullName;
                companyName = selectedCompany.CompanyName;
            }
        }

       

        var viewModel = new CustomerFeedbackViewModel
        {
            
            CompanyBranchOptions = companies,
            SelectedCompanyId = companyId,
            LogoImageBase64 = logoBase64,
            OwnerImageBase64 = ownerBase64,
            OwnerName = ownername,
            CompanyName = companyName,
            Message = TempData["Message"] as string

        };

        TempData.Keep("SelectedCompanyId");
        TempData.Keep("Message");
        TempData.Keep("OwnerName");
        TempData.Keep("CompanyName");


        return View(viewModel);
    }

    [HttpPost]
    public IActionResult ContactStep1(CustomerFeedbackViewModel model)
    {
        string? CompanyName = TempData["CompanyName"] as string;
        // If Back button was clicked, skip validation
        if (Request.Form["back"] == "true")
        {
            TempData["SelectedCompanyId"] = model.SelectedCompanyId;
            TempData["Message"] = model.Message;
            TempData["OwnerName"] = model.OwnerName;
            
            return RedirectToAction("ContactStep0");
        }
        else
        {
            TempData.Clear();
        }

        TempData["CompanyName"] = CompanyName;

        if (!ModelState.IsValid)
        {
            model.CompanyBranchOptions = _context.CompanyModel
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CompanyName + " - " + c.BranchAddress
                }).ToList();
            return View(model);
        }

        TempData["SelectedCompanyId"] = model.SelectedCompanyId;
        TempData["Message"] = model.Message;

        
        TempData.Keep("CompanyName");
        return RedirectToAction("ContactStep2");
    }

    [HttpGet]
    public IActionResult ContactStep2()
    {
        var companyId = TempData["SelectedCompanyId"] as int?; //added this in
        string? logoBase64 = null;
        string? ownerBase64 = null;
        string? ownerName = null;

        if (companyId.HasValue)
        {
            var selectedCompany = _context.CompanyModel
            .Include(c => c.User)
                .FirstOrDefault(c => c.Id == companyId.Value);

            if (selectedCompany?.User != null)
            {
                if (selectedCompany.LogoImage != null)
                    logoBase64 = $"data:image/png;base64,{Convert.ToBase64String(selectedCompany.LogoImage)}";

                if (selectedCompany.User.OwnerImage != null)
                    ownerBase64 = $"data:image/png;base64,{Convert.ToBase64String(selectedCompany.User.OwnerImage)}";

                ownerName = selectedCompany.User.FullName;
            }
        }

        var viewModel = new CustomerFeedbackViewModel
        {
            SelectedCompanyId = companyId,
            LogoImageBase64 = logoBase64,
            OwnerImageBase64 = ownerBase64,
            OwnerName = ownerName,
            Message = TempData["Message"] as string,
            CustomerName = TempData["CustomerName"] as string
        };

        // Keep for the next step or if the user goes back
        TempData.Keep("SelectedCompanyId");
        TempData.Keep("Message");
        TempData.Keep("CustomerName");
        TempData.Keep("OwnerName");
        TempData.Keep("CompanyName");

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult ContactStep2(CustomerFeedbackViewModel model)
    {

        // If Back button was clicked, skip validation
        if (Request.Form["back"] == "true")
        {
            TempData["SelectedCompanyId"] = model.SelectedCompanyId;
            TempData["Message"] = model.Message;
            TempData["OwnerName"] = model.OwnerName;

            return RedirectToAction("ContactStep1");
        }

        // No validation required since name is optional
        TempData["CustomerName"] = model.CustomerName;
        

        return RedirectToAction("ContactStep3");
    }


    [HttpGet]
    public IActionResult ContactStep3()
    {
        var companyId = TempData["SelectedCompanyId"] as int?;
        string? customCompanyName = TempData["CompanyName"] as string;

        string? logoBase64 = null;
        string? ownerBase64 = null;
        string? ownerName = null;

        if (companyId.HasValue)
        {
            var selectedCompany = _context.CompanyModel
                .Include(c => c.User)
                .FirstOrDefault(c => c.Id == companyId.Value);

            if (selectedCompany?.User != null)
            {
                if (selectedCompany.LogoImage != null)
                    logoBase64 = $"data:image/png;base64,{Convert.ToBase64String(selectedCompany.LogoImage)}";

                if (selectedCompany.User.OwnerImage != null)
                    ownerBase64 = $"data:image/png;base64,{Convert.ToBase64String(selectedCompany.User.OwnerImage)}";

                ownerName = selectedCompany.User.FullName;
            }
        }

        var viewModel = new CustomerFeedbackViewModel
        {
            SelectedCompanyId = companyId,
            LogoImageBase64 = logoBase64,
            OwnerImageBase64 = ownerBase64,
            OwnerName = ownerName,
            Message = TempData["Message"] as string,
            CustomerName = TempData["CustomerName"] as string,
            CompanyName = companyId == -1 ? customCompanyName : null
        };

        // Keep data for post or navigating back
        TempData.Keep("SelectedCompanyId");
        TempData.Keep("Message");
        TempData.Keep("CustomerName");
        TempData.Keep("CompanyName");


        return View(viewModel);
    }


    [HttpPost]
    public IActionResult ContactStep3(CustomerFeedbackViewModel model)
    {
        // Email and phone are optional, no need for ModelState check

        if (Request.Form["back"] == "true")
        {
            TempData["SelectedCompanyId"] = model.SelectedCompanyId;
            TempData["Message"] = model.Message;
            TempData["OwnerName"] = model.OwnerName;
            TempData["CustomerName"] = model.CustomerName;

            return RedirectToAction("ContactStep2");
        }

        string? companyName = TempData["CompanyName"] as string;

        int companyId = (int)TempData["SelectedCompanyId"]!;
        string message = TempData["Message"]!.ToString()!;
        string? customerName = TempData["CustomerName"]?.ToString();

        DateTime currentDate = DateTime.Now;
        string dateString = currentDate.ToString("yyyyMMdd");
        int ticketCountToday = _context.TicketModel.Count(t => t.DateOfMessage.Date == currentDate.Date);
        string ticketNumber = (ticketCountToday + 1).ToString("D5");
        string ticketID = $"{dateString}_{ticketNumber}";

        string? contact = model.Contact;

        if (!string.IsNullOrEmpty(contact) && contact.Contains('@'))
        {
            model.Email = contact; // If contact is an email, set Email field
        }
        else if (!string.IsNullOrEmpty(contact) && contact.All(char.IsDigit))
        {
            model.PhoneNumber = contact; // Otherwise, set PhoneNumber field
        }

        if (companyId == -1) //if Other is selected
        {
            ViewBag.ShowProcessingPopup = true;

            var viewModel = new CustomerFeedbackViewModel
            {
                SelectedCompanyId = companyId,
                CompanyName = companyName,
                Message = message,
                CustomerName = customerName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };

            // Preserve for future use if needed
            TempData.Keep("CompanyName");
            TempData.Keep("CustomerName");
            TempData.Keep("Message");

            //insert code for emailing
            var subject = $"{companyName} (Other) - Rectify";
            var body = $"Name: {customerName ?? "Anonymous"} \n" +
                       $"Email: {model.Email} \n" +
                       $"Phone: {model.PhoneNumber} \n" +
                       $"Message: \n\n{message}";

            try
            {
                // Replace with real recipient email
                SendEmail("rectify921@gmail.com", subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email for 'Other' company: " + ex.Message);
                // Optional: Add error feedback to ModelState
            }

            return View(viewModel); // Re-render the same ContactStep3 view
        }

        var ticket = new TicketModel
        {
            TicketID = ticketID,
            CompanyID = companyId,
            DateOfMessage = currentDate,
            Status = "Open",
            DateLastUpdated = currentDate
        };

            var customer = new CustomerModel
            {
                //Id = nextCustomer,
                CustomerName = customerName,
                CustomerEmail = model.Email,
                CustomerPhone = model.PhoneNumber,
                Message = message,
                TicketID = ticketID
            };

        _context.TicketModel.Add(ticket);
        _context.CustomerModel.Add(customer);
        _context.SaveChanges();


        //insert email and whatsapp code
        // Step 1: Get the UserId from the CompanyModel
        string? userId = _context.CompanyModel
            .Where(c => c.Id == companyId)
            .Select(c => c.UserId)
            .FirstOrDefault();

        string? preferredContact = null;

        // Step 2: Use the UserId to fetch PreferredContact from ApplicationUser
        if (!string.IsNullOrEmpty(userId))
        {
            preferredContact = _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.PreferredContact)
                .FirstOrDefault();
        }

        var company = _context.CompanyModel
            .Include(c => c.User) // Include the related ApplicationUser
            .FirstOrDefault(c => c.Id == companyId);

        if (preferredContact == "Email")
        {
            var subject = $"Customer Query ({company.CompanyName}) - Rectify";
            var body = $"Name: {customerName} \n" +
                       $"Email: {model.Email} \n" +
                       $"Phone: {model.PhoneNumber} \n" +
                       $"Message: \n\n{message}";

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
            var businessNumber = company.User.PhoneNumber;

            //insert code to send message
            // Compose the WhatsApp message text
            var waMessage = $"Customer Query from {customerName ?? "Anonymous"}:\n" +
                            $"Email: {model.Email}\n" +
                            $"Phone: {model.PhoneNumber}\n" +
                            $"Message:\n{message}";

            // Encode message for URL
            string encodedMessage = Uri.EscapeDataString(waMessage);

            // Clean number for wa.me link (digits only, no plus)
            string cleanNumber = new string(businessNumber.Where(char.IsDigit).ToArray());

            // WhatsApp deep link URL
            string waLink = $"https://wa.me/{cleanNumber}?text={encodedMessage}";

            // Store the WhatsApp link in TempData to show on confirmation page
            TempData["WhatsAppLink"] = waLink;
            //TempData["TicketID"] = ticketID;

            //return Redirect(waLink);
            //return RedirectToAction("ContactConfirmation");

        }
        else
        {
            //return error
        }

        //TempData.Clear();
        TempData["TicketID"] = ticketID;


        return RedirectToAction("ContactConfirmation");
    }


    [HttpGet]
    public IActionResult ContactConfirmation()
    {
        // Load ticket ID and WhatsApp link from TempData (if available)
        if (TempData.ContainsKey("TicketID"))
        {
            ViewBag.TicketID = TempData["TicketID"];
        }

        if (TempData.ContainsKey("WhatsAppLink"))
        {
            ViewBag.WhatsAppLink = TempData["WhatsAppLink"];
        }

        

        return View();
    }


    private void SendEmail(string toEmail, string subject, string body)
    {
        try
        {
            var smtpServer = _configuration["Gmail:SmtpServer"];
            var port = int.Parse(_configuration["Gmail:Port"]!);
            var fromEmail = _configuration["Gmail:Username"];
            var password = _configuration["Gmail:Password"];

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
