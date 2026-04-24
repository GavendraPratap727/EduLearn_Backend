using System.ComponentModel.DataAnnotations;

namespace EduLearn.LessonService.Models
{
    public class LessonDto
    {
        public Guid LessonId { get; set; }
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ContentType ContentType { get; set; }
        public string? ContentUrl { get; set; }
        public int DurationMinutes { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPreview { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateLessonRequest
    {
        [Required]
        public Guid CourseId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public ContentType ContentType { get; set; }

        [MaxLength(500)]
        public string? ContentUrl { get; set; }

        [Required]
        public int DurationMinutes { get; set; }

        [Required]
        public int DisplayOrder { get; set; }

        public bool IsPreview { get; set; } = false;
    }

    public class UpdateLessonRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public ContentType ContentType { get; set; }

        [MaxLength(500)]
        public string? ContentUrl { get; set; }

        [Required]
        public int DurationMinutes { get; set; }

        public bool IsPreview { get; set; }
    }

    public class LessonResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public LessonDto? Lesson { get; set; }
        public List<LessonDto>? Lessons { get; set; }
        public int? Count { get; set; }
    }
}
