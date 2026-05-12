using System.ComponentModel.DataAnnotations;

namespace EduLearn.ReviewService.Models
{
    public class CreateReviewRequest
    {
        [Required]
        public Guid CourseId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }
    }

    public class UpdateReviewRequest
    {
        [Range(1, 5)]
        public int? Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }
    }

    public class ReviewDto
    {
        public Guid ReviewId { get; set; }
        public Guid CourseId { get; set; }
        public Guid StudentId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ReviewResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ReviewDto? Review { get; set; }
        public List<ReviewDto>? Reviews { get; set; }
    }

    public class AverageRatingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class HasReviewedResponse
    {
        public bool Success { get; set; }
        public bool HasReviewed { get; set; }
    }
}
