namespace Eventi.Server.Data;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Eventi.Server.Models;

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
        builder.Entity<EventCategory>()
        .HasMany(c => c.Events).WithOne(e => e.Category);
    }

    public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<EventCategory> Categories { get; set; } = null!;
}