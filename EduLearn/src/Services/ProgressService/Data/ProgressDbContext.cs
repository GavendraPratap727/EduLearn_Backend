using EduLearn.ProgressService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.ProgressService.Data
{
    public class ProgressDbContext : DbContext
    {
        public ProgressDbContext(DbContextOptions<ProgressDbContext> options) : base(options)
        {
        }

        public DbSet<LessonProgress> LessonProgress { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LessonProgress>(entity =>
            {
                entity.HasKey(e => e.ProgressId);
                entity.Property(e => e.ProgressId).HasDefaultValueSql("newid()");
                entity.Property(e => e.IsCompleted).HasDefaultValue(false);
                entity.Property(e => e.WatchedSeconds).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            });
        }
    }
}
