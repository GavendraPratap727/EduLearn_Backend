using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduLearn.ProgressService.Models
{
    [Table("LessonProgress")]
    public class LessonProgress
    {
        [Key]
        public Guid ProgressId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid LessonId { get; set; }

        [Required]
        public Guid CourseId { get; set; }

        [Required]
        public bool IsCompleted { get; set; } = false;

        public int WatchedSeconds { get; set; } = 0;

        public DateTime? LastWatchedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
