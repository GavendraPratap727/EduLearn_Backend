using System.ComponentModel.DataAnnotations;

namespace EduLearn.LessonService.Models
{
    public class Lesson
    {
        [Key]
        public Guid LessonId { get; set; }

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

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ContentType
    {
        VIDEO = 0,
        ARTICLE = 1,
        PDF = 2,
        QUIZ_LINK = 3
    }
}
