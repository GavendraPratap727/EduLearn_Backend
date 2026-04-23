using System.ComponentModel.DataAnnotations;

namespace EduLearn.CourseService.Models
{
    public class Course
    {
        [Key]
        public Guid CourseId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public Guid InstructorId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public CourseLevel Level { get; set; }

        [Required]
        [MaxLength(50)]
        public string Language { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        public bool IsPublished { get; set; } = false;

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int TotalDuration { get; set; } = 0; // in minutes

        public int EnrollmentCount { get; set; } = 0;
    }

    public enum CourseLevel
    {
        Beginner = 0,
        Intermediate = 1,
        Advanced = 2
    }
}
