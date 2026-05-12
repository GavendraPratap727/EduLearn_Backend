using EduLearn.ReviewService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.ReviewService.Data
{
    public class ReviewDbContext : DbContext
    {
        public ReviewDbContext(DbContextOptions<ReviewDbContext> options) : base(options)
        {
        }

        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.ReviewId);
                entity.Property(e => e.Rating).IsRequired();
                entity.Property(e => e.IsApproved).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                
                // Unique index on (CourseId, StudentId) to prevent duplicate reviews
                entity.HasIndex(e => new { e.CourseId, e.StudentId }).IsUnique();
            });
        }
    }
}
