using System.Text.Json.Serialization;

namespace EduLearn.QuizService.Models
{
    // Request DTOs
    public class CreateQuizRequest
    {
        public Guid CourseId { get; set; }
        public Guid? LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int PassingScore { get; set; }
        public int MaxAttempts { get; set; }
        public List<QuizQuestion> Questions { get; set; } = new();
    }

    public class UpdateQuizRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public int? PassingScore { get; set; }
        public int? MaxAttempts { get; set; }
        public List<QuizQuestion>? Questions { get; set; }
    }

    public class StartAttemptRequest
    {
        public Guid QuizId { get; set; }
    }

    public class SubmitAttemptRequest
    {
        public Dictionary<int, int> Answers { get; set; } = new();
    }

    // Response DTOs
    public class QuizResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public QuizDto? Quiz { get; set; }
    }

    public class QuizDto
    {
        public Guid QuizId { get; set; }
        public Guid CourseId { get; set; }
        public Guid? LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int PassingScore { get; set; }
        public int MaxAttempts { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class QuizDetailDto : QuizDto
    {
        public List<QuizQuestion> Questions { get; set; } = new();
    }

    public class QuizAttemptResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public QuizAttemptDto? Attempt { get; set; }
    }

    public class QuizAttemptDto
    {
        public Guid AttemptId { get; set; }
        public Guid QuizId { get; set; }
        public Guid StudentId { get; set; }
        public int Score { get; set; }
        public bool IsPassed { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public Dictionary<int, int>? Answers { get; set; }
    }

    public class AttemptsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<QuizAttemptDto> Attempts { get; set; } = new();
    }

    public class CountResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
