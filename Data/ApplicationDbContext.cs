using ExpenseMate.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseMate.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Expense> Expenses { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Expense>().ToTable("Expenses");
            modelBuilder.Entity<User>().ToTable("Users");
        }
    }
}
