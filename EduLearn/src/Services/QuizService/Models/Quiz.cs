using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EduLearn.QuizService.Models
{
    public class Quiz
    {
        [Key]
        public Guid QuizId { get; set; }

        [Required]
        public Guid CourseId { get; set; }

        public Guid? LessonId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int TimeLimitMinutes { get; set; }

        [Required]
        [Range(0, 100)]
        public int PassingScore { get; set; }

        [Required]
        public int MaxAttempts { get; set; }

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Questions stored as JSON string
        [Column(TypeName = "text")]
        public string Questions { get; set; } = string.Empty;
    }

    public class QuizQuestion
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int CorrectAnswer { get; set; }
    }
}
