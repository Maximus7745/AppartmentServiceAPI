using Microsoft.EntityFrameworkCore;

namespace ApartmentServiceAPI.Models
{
    public class ApplicationDbCobtext : DbContext
    {
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public ApplicationDbCobtext(DbContextOptions<ApplicationDbCobtext> options) : base(options) 
        {
            Database.EnsureCreated();
        }

    }
}
