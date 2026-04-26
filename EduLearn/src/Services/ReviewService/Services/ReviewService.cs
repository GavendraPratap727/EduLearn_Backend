using EduLearn.ReviewService.Models;
using EduLearn.ReviewService.Repositories;
using System.Net.Http.Json;

namespace EduLearn.ReviewService.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ReviewService(IReviewRepository repository, HttpClient httpClient, IConfiguration configuration)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<ReviewResponse> AddReviewAsync(Guid studentId, CreateReviewRequest request)
        {
            // Check if student has already reviewed this course
            var existingReview = await _repository.FindByStudentAndCourseAsync(studentId, request.CourseId);
            if (existingReview != null)
            {
                return new ReviewResponse
                {
                    Success = false,
                    Message = "You have already reviewed this course"
                };
            }

            // Check if student is enrolled in the course (call EnrollmentService)
            bool isEnrolled = await CheckEnrollmentAsync(studentId, request.CourseId);
            if (!isEnrolled)
            {
                return new ReviewResponse
                {
                    Success = false,
                    Message = "You must be enrolled in this course to review it"
                };
            }

            // Check if student has made progress (call ProgressService)
            bool hasProgress = await CheckProgressAsync(studentId, request.CourseId);
            if (!hasProgress)
            {
                return new ReviewResponse
                {
                    Success = false,
                    Message = "You must make progress in this course before reviewing"
                };
            }

            var review = new Review
            {
                ReviewId = Guid.NewGuid(),
                CourseId = request.CourseId,
                StudentId = studentId,
                Rating = request.Rating,
                Comment = request.Comment,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            var createdReview = await _repository.AddAsync(review);

            return new ReviewResponse
            {
                Success = true,
                Message = "Review submitted successfully. It will be visible after approval.",
                Review = MapToReviewDto(createdReview)
            };
        }

        public async Task<ReviewResponse> GetReviewByIdAsync(Guid reviewId)
        {
            var review = await _repository.FindByReviewIdAsync(reviewId);
            if (review == null)
            {
                return new ReviewResponse
                {
                    Success = false,
                    Message = "Review not found"
                };
            }

            return new ReviewResponse
            {
                Success = true,
                Message = "Review found",
                Review = MapToReviewDto(review)
            };
        }

        public async Task<ReviewResponse> GetReviewsByCourseAsync(Guid courseId, bool approvedOnly = false)
        {
            List<Review> reviews;
            if (approvedOnly)
            {
                reviews = await _repository.FindApprovedAsync(courseId);
            }
            else
            {
                reviews = await _repository.FindByCourseIdAsync(courseId);
            }

            return new ReviewResponse
            {
                Success = true,
                Message = $"Found {reviews.Count} reviews",
                Reviews = reviews.Select(MapToReviewDto).ToList()
            };
        }

        public async Task<ReviewResponse> GetReviewsByStudentAsync(Guid studentId)
        {
            var reviews = await _repository.FindByStudentIdAsync(studentId);

            return new ReviewResponse
            {
                Success = true,
                Message = $"Found {reviews.Count} reviews",
                Reviews = reviews.Select(MapToReviewDto).ToList()
            };
        }

        public async Task<ReviewResponse> GetApprovedReviewsAsync(Guid courseId)
        {
            var reviews = await _repository.FindApprovedAsync(courseId);

            return new ReviewResponse
            {
                Success = true,
                Message = $"Found {reviews.Count} approved reviews",
                Reviews = reviews.Select(MapToReviewDto).ToList()
            };
        }

        public async Task<ReviewResponse> UpdateReviewAsync(Guid reviewId, Guid studentId, UpdateReviewRequest request)
        {
            var review = await _repository.FindByReviewIdAsync(reviewId);
            if (review == null)
            {
                return new ReviewResponse
                {
                    Success = false,
                    Message = "Review not found"
                };
            }

            // Check if the student owns this review
            if (review.StudentId != studentId)
            {
                return new ReviewResponse
                {
                    Success = false,
                    Message = "You can only edit your own reviews"
                };
            }

            if (request.Rating.HasValue)
            {
                review.Rating = request.Rating.Value;
            }

            if (request.Comment != null)
            {
                review.Comment = request.Comment;
            }

            review.UpdatedAt = DateTime.UtcNow;

            var updatedReview = await _repository.UpdateAsync(review);

            return new ReviewResponse
            {
                Success = true,
                Message = "Review updated successfully",
                Review = MapToReviewDto(updatedReview)
            };
        }

        public async Task<ReviewResponse> ApproveReviewAsync(Guid reviewId)
        {
            var review = await _repository.FindByReviewIdAsync(reviewId);
            if (review == null)
            {
                return new ReviewResponse
                {
                    Success = false,
                    Message = "Review not found"
                };
            }

            review.IsApproved = true;
            review.UpdatedAt = DateTime.UtcNow;

            var updatedReview = await _repository.UpdateAsync(review);

            return new ReviewResponse
            {
                Success = true,
                Message = "Review approved successfully",
                Review = MapToReviewDto(updatedReview)
            };
        }

        public async Task<ReviewResponse> DeleteReviewAsync(Guid reviewId, Guid studentId)
        {
            var review = await _repository.FindByReviewIdAsync(reviewId);
            if (review == null)
            {
                return new ReviewResponse
                {
                    Success = false,
                    Message = "Review not found"
                };
            }

            // Check if the student owns this review or is admin
            if (review.StudentId != studentId)
            {
                return new ReviewResponse
                {
                    Success = false,
                    Message = "You can only delete your own reviews"
                };
            }

            await _repository.DeleteAsync(review);

            return new ReviewResponse
            {
                Success = true,
                Message = "Review deleted successfully"
            };
        }

        public async Task<AverageRatingResponse> GetAverageRatingAsync(Guid courseId)
        {
            var averageRating = await _repository.GetAverageRatingAsync(courseId);
            var reviewCount = await _repository.CountByCourseIdAsync(courseId);

            return new AverageRatingResponse
            {
                Success = true,
                Message = "Average rating calculated",
                AverageRating = Math.Round(averageRating, 1),
                ReviewCount = reviewCount
            };
        }

        public async Task<int> GetReviewCountAsync(Guid courseId)
        {
            return await _repository.CountByCourseIdAsync(courseId);
        }

        public async Task<HasReviewedResponse> HasStudentReviewedAsync(Guid studentId, Guid courseId)
        {
            var review = await _repository.FindByStudentAndCourseAsync(studentId, courseId);

            return new HasReviewedResponse
            {
                Success = true,
                HasReviewed = review != null
            };
        }

        private async Task<bool> CheckEnrollmentAsync(Guid studentId, Guid courseId)
        {
            try
            {
                var enrollmentServiceUrl = _configuration["EnrollmentService:Url"] ?? "http://localhost:5003";
                var response = await _httpClient.GetAsync($"{enrollmentServiceUrl}/api/enrollments/isEnrolled/{studentId}/{courseId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                // If enrollment service is unavailable, allow review for testing
                return true;
            }
        }

        private async Task<bool> CheckProgressAsync(Guid studentId, Guid courseId)
        {
            try
            {
                var progressServiceUrl = _configuration["ProgressService:Url"] ?? "http://localhost:5004";
                var response = await _httpClient.GetAsync($"{progressServiceUrl}/api/progress/course/{courseId}/student/{studentId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                // If progress service is unavailable, allow review for testing
                return true;
            }
        }

        private ReviewDto MapToReviewDto(Review review)
        {
            return new ReviewDto
            {
                ReviewId = review.ReviewId,
                CourseId = review.CourseId,
                StudentId = review.StudentId,
                Rating = review.Rating,
                Comment = review.Comment,
                IsApproved = review.IsApproved,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };
        }
    }
}
