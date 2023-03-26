namespace _2cpbackend.Data;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using _2cpbackend.Models;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions options)
    : base(options)
    {
        
    }

    public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;
}