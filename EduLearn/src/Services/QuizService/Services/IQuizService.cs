using EduLearn.QuizService.Models;

namespace EduLearn.QuizService.Services
{
    public interface IQuizService
    {
        Task<QuizResponse> CreateQuizAsync(CreateQuizRequest request);
        Task<QuizResponse> GetQuizByIdAsync(Guid quizId);
        Task<QuizResponse> GetQuizzesByCourseAsync(Guid courseId);
        Task<QuizResponse> GetQuizByLessonAsync(Guid lessonId);
        Task<QuizResponse> UpdateQuizAsync(Guid quizId, UpdateQuizRequest request);
        Task<QuizResponse> DeleteQuizAsync(Guid quizId);
        Task<QuizResponse> PublishQuizAsync(Guid quizId);
        Task<QuizAttemptResponse> StartAttemptAsync(Guid studentId, StartAttemptRequest request);
        Task<QuizAttemptResponse> SubmitAttemptAsync(Guid attemptId, SubmitAttemptRequest request);
        Task<AttemptsResponse> GetAttemptsByStudentAsync(Guid studentId, Guid quizId);
        Task<QuizAttemptResponse> GetBestAttemptAsync(Guid studentId, Guid quizId);
        Task<CountResponse> GetAttemptCountAsync(Guid studentId, Guid quizId);
    }
}
