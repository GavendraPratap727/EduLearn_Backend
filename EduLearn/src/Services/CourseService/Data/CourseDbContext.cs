using EduLearn.CourseService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.CourseService.Data
{
    public class CourseDbContext : DbContext
    {
        public CourseDbContext(DbContextOptions<CourseDbContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.CourseId);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.InstructorId).IsRequired();
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Level).IsRequired();
                entity.Property(e => e.Language).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
                entity.Property(e => e.IsPublished).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.IsApproved).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.TotalDuration).IsRequired().HasDefaultValue(0);
                entity.Property(e => e.EnrollmentCount).IsRequired().HasDefaultValue(0);
            });
        }
    }
}
