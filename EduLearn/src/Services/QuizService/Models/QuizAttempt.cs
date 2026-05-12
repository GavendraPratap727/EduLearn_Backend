using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduLearn.QuizService.Models
{
    public class QuizAttempt
    {
        [Key]
        public Guid AttemptId { get; set; }

        [Required]
        public Guid QuizId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        [Required]
        [Range(0, 100)]
        public int Score { get; set; }

        public bool IsPassed { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SubmittedAt { get; set; }

        // Answers stored as JSON string mapping QuestionId to selected answer
        [Column(TypeName = "text")]
        public string Answers { get; set; } = string.Empty;

        // Navigation property
        [ForeignKey("QuizId")]
        public Quiz? Quiz { get; set; }
    }
}
