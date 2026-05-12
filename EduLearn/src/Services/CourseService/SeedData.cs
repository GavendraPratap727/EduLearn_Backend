using EduLearn.CourseService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.CourseService.Data
{
    public static class SeedData
    {
        public static void Initialize(CourseDbContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Check if any courses already exist
            if (context.Courses.Any())
            {
                return; // Database has been seeded
            }

            var courses = new Course[]
            {
                new Course
                {
                    CourseId = new Guid("58a99e03-6426-4ae4-add1-336549bd4b25"),
                    Title = "Introduction to Web Development",
                    Description = "Learn the fundamentals of HTML, CSS, and JavaScript to build modern web applications.",
                    InstructorId = new Guid("6809215f-c7f2-4973-8e7d-d1ad0ed71471"),
                    Category = "Web Development",
                    Level = CourseLevel.Beginner,
                    Language = "English",
                    Price = 0,
                    ThumbnailUrl = "https://via.placeholder.com/300x200?text=Web+Dev",
                    IsPublished = true,
                    IsApproved = true,
                    IsSubmittedForReview = true,
                    IsFinished = true,
                    CreatedAt = DateTime.UtcNow,
                    TotalDuration = 1200, // 20 hours
                    EnrollmentCount = 0
                },
                new Course
                {
                    CourseId = new Guid("e050da5e-a078-46aa-9442-f0d02e8715b4"),
                    Title = "Advanced React Development",
                    Description = "Master React hooks, state management, and advanced patterns for building complex applications.",
                    InstructorId = new Guid("6809215f-c7f2-4973-8e7d-d1ad0ed71471"),
                    Category = "Web Development",
                    Level = CourseLevel.Advanced,
                    Language = "English",
                    Price = 49.99m,
                    ThumbnailUrl = "https://via.placeholder.com/300x200?text=React+Advanced",
                    IsPublished = true,
                    IsApproved = true,
                    IsSubmittedForReview = true,
                    IsFinished = true,
                    CreatedAt = DateTime.UtcNow,
                    TotalDuration = 1800, // 30 hours
                    EnrollmentCount = 0
                },
                new Course
                {
                    CourseId = new Guid("67cc3693-b840-4be1-b41f-43a56e049918"),
                    Title = "Database Design Fundamentals",
                    Description = "Learn relational database design, SQL, and best practices for data modeling.",
                    InstructorId = new Guid("6809215f-c7f2-4973-8e7d-d1ad0ed71471"),
                    Category = "Database",
                    Level = CourseLevel.Intermediate,
                    Language = "English",
                    Price = 29.99m,
                    ThumbnailUrl = "https://via.placeholder.com/300x200?text=Database",
                    IsPublished = true,
                    IsApproved = true,
                    IsSubmittedForReview = true,
                    IsFinished = true,
                    CreatedAt = DateTime.UtcNow,
                    TotalDuration = 900, // 15 hours
                    EnrollmentCount = 0
                }
            };

            context.Courses.AddRange(courses);
            context.SaveChanges();
        }
    }
}
