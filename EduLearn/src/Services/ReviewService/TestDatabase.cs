using EduLearn.ReviewService.Data;
using EduLearn.ReviewService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.ReviewService
{
    public class TestDatabase
    {
        public static void TestReviewData()
        {
            var options = new DbContextOptionsBuilder<ReviewDbContext>()
                .UseSqlite("Data Source=ReviewService.db")
                .Options;

            using var context = new ReviewDbContext(options);
            
            Console.WriteLine("=== Testing Review Database ===");
            
            // Check if database exists and has reviews
            var reviewCount = context.Reviews.Count();
            Console.WriteLine($"Total reviews in database: {reviewCount}");
            
            if (reviewCount > 0)
            {
                var reviews = context.Reviews.ToList();
                foreach (var review in reviews)
                {
                    Console.WriteLine($"Review ID: {review.ReviewId}");
                    Console.WriteLine($"Course ID: {review.CourseId}");
                    Console.WriteLine($"Student ID: {review.StudentId}");
                    Console.WriteLine($"Rating: {review.Rating}");
                    Console.WriteLine($"IsApproved: {review.IsApproved}");
                    Console.WriteLine($"Created: {review.CreatedAt}");
                    Console.WriteLine("---");
                }
                
                // Test average calculation
                var avgRating = reviews.Average(r => r.Rating);
                Console.WriteLine($"Average rating: {avgRating}");
                
                // Test average by course
                var courseGroups = reviews.GroupBy(r => r.CourseId);
                foreach (var group in courseGroups)
                {
                    var courseAvg = group.Average(r => r.Rating);
                    Console.WriteLine($"Course {group.Key} average: {courseAvg}");
                }
            }
            else
            {
                Console.WriteLine("No reviews found in database!");
            }
        }
    }
}
