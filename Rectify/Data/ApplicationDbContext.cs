using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rectify.Models;

namespace Rectify.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        public DbSet<CustomerModel> CustomerModel {  get; set; }
        public DbSet<TicketModel> TicketModel { get; set; }


    }
}
