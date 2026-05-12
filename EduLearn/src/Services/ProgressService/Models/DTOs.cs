using System.ComponentModel.DataAnnotations;

namespace EduLearn.ProgressService.Models
{
    public class LessonProgressDto
    {
        public Guid ProgressId { get; set; }
        public Guid StudentId { get; set; }
        public Guid LessonId { get; set; }
        public Guid CourseId { get; set; }
        public bool IsCompleted { get; set; }
        public int WatchedSeconds { get; set; }
        public DateTime? LastWatchedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateLessonProgressRequest
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid LessonId { get; set; }

        [Required]
        public Guid CourseId { get; set; }
    }

    public class UpdateLessonProgressRequest
    {
        public bool IsCompleted { get; set; }
        public int WatchedSeconds { get; set; }
    }

    public class LessonProgressResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public LessonProgressDto? Progress { get; set; }
        public List<LessonProgressDto>? ProgressList { get; set; }
        public int CourseProgressPercent { get; set; }
    }

    public class OverallStatsResponse
    {
        public bool Success { get; set; }
        public Dictionary<string, int> Stats { get; set; } = new Dictionary<string, int>();
    }

    // Certificate DTOs
    public class CertificateDto
    {
        public string CertificateId { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public DateTime IssuedAt { get; set; }
        public string? CertificateUrl { get; set; }
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class CertificateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public CertificateDto? Certificate { get; set; }
    }

    public class IssueCertificateRequest
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid CourseId { get; set; }
    }
}
