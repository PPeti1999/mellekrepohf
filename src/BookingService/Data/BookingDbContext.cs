using BookingService.Entities;
using Microsoft.EntityFrameworkCore;
using MassTransit; // <--- FONTOS: Ez kell a konfigurációs metódusokhoz!

namespace BookingService.Data
{
    public class BookingDbContext : DbContext
    {
        public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) {}
        
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ez a három sor hozza létre a MassTransit Outbox tábláit
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
        }
    }
}