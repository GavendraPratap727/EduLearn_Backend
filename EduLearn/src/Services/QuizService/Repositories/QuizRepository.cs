using EduLearn.QuizService.Data;
using EduLearn.QuizService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.QuizService.Repositories
{
    public class QuizRepository : IQuizRepository
    {
        private readonly QuizDbContext _context;

        public QuizRepository(QuizDbContext context)
        {
            _context = context;
        }

        public async Task<Quiz?> FindByQuizIdAsync(Guid quizId)
        {
            return await _context.Quizzes.FindAsync(quizId);
        }

        public async Task<Quiz?> FindByCourseIdAsync(Guid courseId)
        {
            return await _context.Quizzes
                .FirstOrDefaultAsync(q => q.CourseId == courseId && q.LessonId == null);
        }

        public async Task<Quiz?> FindByLessonIdAsync(Guid lessonId)
        {
            return await _context.Quizzes
                .FirstOrDefaultAsync(q => q.LessonId == lessonId);
        }

        public async Task<List<Quiz>> FindByCourseIdListAsync(Guid courseId)
        {
            return await _context.Quizzes
                .Where(q => q.CourseId == courseId)
                .ToListAsync();
        }

        public async Task<List<QuizAttempt>> FindAttemptsByStudentAndQuizAsync(Guid studentId, Guid quizId)
        {
            return await _context.QuizAttempts
                .Where(a => a.StudentId == studentId && a.QuizId == quizId)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();
        }

        public async Task<QuizAttempt?> FindAttemptByIdAsync(Guid attemptId)
        {
            return await _context.QuizAttempts.FindAsync(attemptId);
        }

        public async Task<int> CountAttemptsAsync(Guid studentId, Guid quizId)
        {
            return await _context.QuizAttempts
                .CountAsync(a => a.StudentId == studentId && a.QuizId == quizId);
        }

        public async Task<QuizAttempt?> FindBestAttemptAsync(Guid studentId, Guid quizId)
        {
            return await _context.QuizAttempts
                .Where(a => a.StudentId == studentId && a.QuizId == quizId)
                .OrderByDescending(a => a.Score)
                .FirstOrDefaultAsync();
        }

        public async Task<Quiz> AddAsync(Quiz quiz)
        {
            await _context.Quizzes.AddAsync(quiz);
            await _context.SaveChangesAsync();
            return quiz;
        }

        public async Task<QuizAttempt> AddAttemptAsync(QuizAttempt attempt)
        {
            await _context.QuizAttempts.AddAsync(attempt);
            await _context.SaveChangesAsync();
            return attempt;
        }

        public async Task<Quiz> UpdateAsync(Quiz quiz)
        {
            _context.Quizzes.Update(quiz);
            await _context.SaveChangesAsync();
            return quiz;
        }

        public async Task<QuizAttempt> UpdateAttemptAsync(QuizAttempt attempt)
        {
            _context.QuizAttempts.Update(attempt);
            await _context.SaveChangesAsync();
            return attempt;
        }

        public async Task DeleteAsync(Quiz quiz)
        {
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAttemptAsync(QuizAttempt attempt)
        {
            _context.QuizAttempts.Remove(attempt);
            await _context.SaveChangesAsync();
        }
    }
}
