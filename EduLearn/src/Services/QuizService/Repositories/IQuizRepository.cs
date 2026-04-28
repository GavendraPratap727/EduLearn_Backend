using EduLearn.QuizService.Models;

namespace EduLearn.QuizService.Repositories
{
    public interface IQuizRepository
    {
        Task<Quiz?> FindByQuizIdAsync(Guid quizId);
        Task<Quiz?> FindByCourseIdAsync(Guid courseId);
        Task<Quiz?> FindByLessonIdAsync(Guid lessonId);
        Task<List<Quiz>> FindByCourseIdListAsync(Guid courseId);
        Task<List<QuizAttempt>> FindAttemptsByStudentAndQuizAsync(Guid studentId, Guid quizId);
        Task<QuizAttempt?> FindAttemptByIdAsync(Guid attemptId);
        Task<int> CountAttemptsAsync(Guid studentId, Guid quizId);
        Task<QuizAttempt?> FindBestAttemptAsync(Guid studentId, Guid quizId);
        Task<Quiz> AddAsync(Quiz quiz);
        Task<QuizAttempt> AddAttemptAsync(QuizAttempt attempt);
        Task<Quiz> UpdateAsync(Quiz quiz);
        Task<QuizAttempt> UpdateAttemptAsync(QuizAttempt attempt);
        Task DeleteAsync(Quiz quiz);
        Task DeleteAttemptAsync(QuizAttempt attempt);
    }
}
