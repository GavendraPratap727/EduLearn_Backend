using EduLearn.EnrollmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.EnrollmentService.Data
{
    public class EnrollmentDbContext : DbContext
    {
        public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options) : base(options)
        {
        }

        public DbSet<Enrollment> Enrollments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.EnrollmentId);
                entity.Property(e => e.EnrollmentId).HasDefaultValueSql("newid()");
                entity.Property(e => e.EnrolledAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.Status).HasDefaultValue(0);
                entity.Property(e => e.ProgressPercent).HasDefaultValue(0);
                entity.Property(e => e.CertificateIssued).HasDefaultValue(false);
            });
        }
    }
}
