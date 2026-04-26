using EduLearn.ReviewService.Models;

namespace EduLearn.ReviewService.Services
{
    public interface IReviewService
    {
        Task<ReviewResponse> AddReviewAsync(Guid studentId, CreateReviewRequest request);
        Task<ReviewResponse> GetReviewByIdAsync(Guid reviewId);
        Task<ReviewResponse> GetReviewsByCourseAsync(Guid courseId, bool approvedOnly = false);
        Task<ReviewResponse> GetReviewsByStudentAsync(Guid studentId);
        Task<ReviewResponse> GetApprovedReviewsAsync(Guid courseId);
        Task<ReviewResponse> UpdateReviewAsync(Guid reviewId, Guid studentId, UpdateReviewRequest request);
        Task<ReviewResponse> ApproveReviewAsync(Guid reviewId);
        Task<ReviewResponse> DeleteReviewAsync(Guid reviewId, Guid studentId);
        Task<AverageRatingResponse> GetAverageRatingAsync(Guid courseId);
        Task<int> GetReviewCountAsync(Guid courseId);
        Task<HasReviewedResponse> HasStudentReviewedAsync(Guid studentId, Guid courseId);
    }
}
