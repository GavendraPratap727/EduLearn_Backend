using EduLearn.ReviewService.Data;
using EduLearn.ReviewService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.ReviewService.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ReviewDbContext _context;

        public ReviewRepository(ReviewDbContext context)
        {
            _context = context;
        }

        public async Task<Review?> FindByReviewIdAsync(Guid reviewId)
        {
            return await _context.Reviews.FindAsync(reviewId);
        }

        public async Task<List<Review>> FindByCourseIdAsync(Guid courseId)
        {
            return await _context.Reviews
                .Where(r => r.CourseId == courseId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Review>> FindByStudentIdAsync(Guid studentId)
        {
            return await _context.Reviews
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> FindByStudentAndCourseAsync(Guid studentId, Guid courseId)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.StudentId == studentId && r.CourseId == courseId);
        }

        public async Task<List<Review>> FindApprovedAsync(Guid courseId)
        {
            return await _context.Reviews
                .Where(r => r.CourseId == courseId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<double> GetAverageRatingAsync(Guid courseId)
        {
            var ratings = _context.Reviews.Where(r => r.CourseId == courseId);
            if (!await ratings.AnyAsync())
            {
                return 0.0;
            }
            return await ratings.AverageAsync(r => r.Rating);
        }

        public async Task<int> CountByCourseIdAsync(Guid courseId)
        {
            return await _context.Reviews
                .CountAsync(r => r.CourseId == courseId);
        }

        public async Task<Review> AddAsync(Review review)
        {
            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<Review> UpdateAsync(Review review)
        {
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task DeleteAsync(Review review)
        {
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
        }
    }
}
