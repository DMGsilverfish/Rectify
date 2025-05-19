// Controller (FeedbackController.cs)

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rectify.Data;
using Rectify.Models;
using Rectify.Models.ViewModels;
using System;
using System.Linq;

public class FeedbackController : Controller
{
    private readonly ApplicationDbContext _context;

    public FeedbackController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult ContactStep1(int? companyId)
    {
        //https://localhost:7192/Feedback/ContactStep1?companyId=5
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
            }
        }

        var viewModel = new CustomerFeedbackViewModel
        {
            //CompanyBranchOptions = companyUsers,
            CompanyBranchOptions = companies,
            SelectedCompanyId = companyId,
            LogoImageBase64 = logoBase64,
            OwnerImageBase64 = ownerBase64

        };

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult ContactStep1(CustomerFeedbackViewModel model)
    {
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
        return RedirectToAction("ContactStep2");
    }

    [HttpGet]
    public IActionResult ContactStep2()
    {
        return View();
    }

    [HttpPost]
    public IActionResult ContactStep2(CustomerFeedbackViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Rehydrate TempData
        int companyId = (int)TempData["SelectedCompanyId"]!;
        string message = TempData["Message"]!.ToString()!;

        DateTime currentDate = DateTime.Now;
        string dateString = currentDate.ToString("yyyyMMdd");
        int ticketCountToday = _context.TicketModel.Count(t => t.DateOfMessage.Date == currentDate.Date);
        string ticketNumber = (ticketCountToday + 1).ToString("D5");
        string ticketID = $"{dateString}_{ticketNumber}";

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
            CustomerName = model.CustomerName,
            CustomerEmail = model.Email,
            CustomerPhone = model.PhoneNumber,
            Message = message,
            TicketID = ticketID
        };

        _context.TicketModel.Add(ticket);
        _context.CustomerModel.Add(customer);
        _context.SaveChanges();

        TempData.Clear();

        return RedirectToAction("ContactConfirmation", new { id = ticketID });
    }

    [HttpGet]
    public IActionResult ContactConfirmation(string id)
    {
        ViewBag.TicketID = id;
        return View();
    }
}
