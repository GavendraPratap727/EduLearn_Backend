using System.ComponentModel.DataAnnotations;

namespace EduLearn.CourseService.Models
{
    public class CourseDto
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid InstructorId { get; set; }
        public string Category { get; set; } = string.Empty;
        public CourseLevel Level { get; set; }
        public string Language { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsPublished { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TotalDuration { get; set; }
        public int EnrollmentCount { get; set; }
    }

    public class CreateCourseRequest
    {
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
    }

    public class UpdateCourseRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

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

        public int TotalDuration { get; set; }
    }

    public class CourseResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public CourseDto? Course { get; set; }
        public List<CourseDto>? Courses { get; set; }
    }
}
