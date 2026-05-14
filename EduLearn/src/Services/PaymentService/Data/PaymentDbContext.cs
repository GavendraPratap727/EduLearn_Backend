using EduLearn.PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.PaymentService.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Payment configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                
                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);
                
                entity.Property(e => e.Status)
                    .HasDefaultValue("Pending");
                
                entity.Property(e => e.PaymentType)
                    .HasDefaultValue("Course");
                
                entity.Property(e => e.Currency)
                    .HasDefaultValue("INR");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Indexes
                entity.HasIndex(e => e.StudentId);
                entity.HasIndex(e => e.CourseId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.RazorpayOrderId);
                entity.HasIndex(e => e.RazorpayPaymentId);
            });

            // Student configuration
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.StudentId);
                
                entity.Property(e => e.Email)
                    .IsRequired();
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Course configuration
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.CourseId);
                
                entity.Property(e => e.Price)
                    .HasPrecision(18, 2);
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Relationships
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Student)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Course)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
