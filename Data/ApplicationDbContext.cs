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

    //Ensure postgis is install in the database
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("postgis");

        //Configuring relationships
        builder.Entity<Event>()
        .HasMany(e => e.Attendees).WithMany(u => u.AttendedByUser);
        builder.Entity<Event>()
        .HasOne(e => e.Organizer).WithMany(u => u.OrganizedByUser);
        builder.Entity<Event>()
        .HasMany(e => e.BanList);
        builder.Entity<ApplicationUser>()
        .HasMany(u => u.SavedEvents);
    }

    public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
}