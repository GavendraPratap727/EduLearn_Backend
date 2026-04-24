using EduLearn.LessonService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.LessonService.Data
{
    public class LessonDbContext : DbContext
    {
        public LessonDbContext(DbContextOptions<LessonDbContext> options) : base(options)
        {
        }

        public DbSet<Lesson> Lessons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.HasKey(e => e.LessonId);
                entity.Property(e => e.CourseId).IsRequired();
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.ContentType).IsRequired();
                entity.Property(e => e.ContentUrl).HasMaxLength(500);
                entity.Property(e => e.DurationMinutes).IsRequired();
                entity.Property(e => e.DisplayOrder).IsRequired();
                entity.Property(e => e.IsPreview).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.IsPublished).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("datetime('now')");
            });
        }
    }
}
