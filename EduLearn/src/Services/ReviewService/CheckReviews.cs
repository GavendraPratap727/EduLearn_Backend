using EduLearn.ReviewService.Data;
using EduLearn.ReviewService.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EduLearn.ReviewService
{
    public class CheckReviews
    {
        public static void CheckAllReviews()
        {
            try
            {
                var options = new DbContextOptionsBuilder<ReviewDbContext>()
                    .UseNpgsql("Data Source=ReviewService.db")
                    .Options;

                using var context = new ReviewDbContext(options);
                
                Console.WriteLine("=== CHECKING REVIEW DATABASE ===");
                Console.WriteLine($"Database file: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReviewService.db")}");
                
                // Check if database exists
                var databaseExists = context.Database.CanConnect();
                Console.WriteLine($"Database exists and can connect: {databaseExists}");
                
                if (!databaseExists)
                {
                    Console.WriteLine("Database doesn't exist or can't connect!");
                    return;
                }
                
                // Count all reviews
                var totalReviews = context.Reviews.Count();
                Console.WriteLine($"\nTotal reviews in database: {totalReviews}");
                
                if (totalReviews == 0)
                {
                    Console.WriteLine("NO REVIEWS FOUND IN DATABASE!");
                    Console.WriteLine("This means either:");
                    Console.WriteLine("1. No students have submitted reviews yet");
                    Console.WriteLine("2. Review creation is failing");
                    Console.WriteLine("3. Reviews are being saved to a different database");
                    return;
                }
                
                // Show all reviews
                var allReviews = context.Reviews.ToList();
                Console.WriteLine($"\n=== ALL REVIEWS ({allReviews.Count}) ===");
                
                foreach (var review in allReviews)
                {
                    Console.WriteLine($"\nReview ID: {review.ReviewId}");
                    Console.WriteLine($"Course ID: {review.CourseId}");
                    Console.WriteLine($"Student ID: {review.StudentId}");
                    Console.WriteLine($"Rating: {review.Rating}");
                    Console.WriteLine($"Comment: {review.Comment}");
                    Console.WriteLine($"Is Approved: {review.IsApproved}");
                    Console.WriteLine($"Created At: {review.CreatedAt}");
                    Console.WriteLine($"Updated At: {review.UpdatedAt}");
                }
                
                // Group by course to show averages
                var courseGroups = allReviews.GroupBy(r => r.CourseId);
                Console.WriteLine($"\n=== RATINGS BY COURSE ===");
                
                foreach (var group in courseGroups)
                {
                    var avgRating = group.Average(r => r.Rating);
                    var count = group.Count();
                    Console.WriteLine($"Course {group.Key}: {avgRating:F1} average ({count} reviews)");
                }
                
                // Test the actual average calculation method
                Console.WriteLine($"\n=== TESTING REPOSITORY METHODS ===");
                foreach (var courseId in courseGroups.Select(g => g.Key))
                {
                    var avgFromRepo = context.Reviews
                        .Where(r => r.CourseId == courseId)
                        .Average(r => r.Rating);
                    var countFromRepo = context.Reviews
                        .Count(r => r.CourseId == courseId);
                    
                    Console.WriteLine($"Course {courseId}:");
                    Console.WriteLine($"  Repository Average: {avgFromRepo:F1}");
                    Console.WriteLine($"  Repository Count: {countFromRepo}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR checking reviews: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
