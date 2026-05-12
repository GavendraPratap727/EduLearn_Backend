using EduLearn.QuizService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.QuizService.Data
{
    public class QuizDbContext : DbContext
    {
        public QuizDbContext(DbContextOptions<QuizDbContext> options) : base(options)
        {
        }

        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Quiz>(entity =>
            {
                entity.HasKey(e => e.QuizId);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Questions).IsRequired();
            });

            modelBuilder.Entity<QuizAttempt>(entity =>
            {
                entity.HasKey(e => e.AttemptId);
                entity.HasOne(e => e.Quiz)
                      .WithMany()
                      .HasForeignKey(e => e.QuizId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
