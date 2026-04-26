using EduLearn.ReviewService.Models;

namespace EduLearn.ReviewService.Repositories
{
    public interface IReviewRepository
    {
        Task<Review?> FindByReviewIdAsync(Guid reviewId);
        Task<List<Review>> FindByCourseIdAsync(Guid courseId);
        Task<List<Review>> FindByStudentIdAsync(Guid studentId);
        Task<Review?> FindByStudentAndCourseAsync(Guid studentId, Guid courseId);
        Task<List<Review>> FindApprovedAsync(Guid courseId);
        Task<double> GetAverageRatingAsync(Guid courseId);
        Task<int> CountByCourseIdAsync(Guid courseId);
        Task<Review> AddAsync(Review review);
        Task<Review> UpdateAsync(Review review);
        Task DeleteAsync(Review review);
    }
}
