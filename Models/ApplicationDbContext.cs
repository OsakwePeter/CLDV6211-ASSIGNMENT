using EventEase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace EventEase.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Venue> Venue { get; set; }
        public DbSet<Event> Event { get; set; }
        public DbSet<Booking> Booking { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Prevent multiple cascade paths to Booking
            // Venue -> Booking: restrict (handle deletion in app layer)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Venue)
                .WithMany()
                .HasForeignKey(b => b.VenueID)
                .OnDelete(DeleteBehavior.Restrict);

            // Event -> Booking: cascade (deleting an event removes its bookings)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany()
                .HasForeignKey(b => b.EventID)
                .OnDelete(DeleteBehavior.Cascade);

            // Event -> Venue: set null (event can exist without a venue)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Venue)
                .WithMany()
                .HasForeignKey(e => e.VenueID)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraint: prevent same venue booked for same event
            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.VenueID, b.EventID })
                .IsUnique();
        }
    }
}

