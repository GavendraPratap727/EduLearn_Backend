using EduLearn.ProgressService.Models;

namespace EduLearn.ProgressService.Services
{
    public interface IProgressService
    {
        Task<LessonProgressResponse> CreateProgressAsync(CreateLessonProgressRequest request);
        Task<LessonProgressResponse> GetProgressByIdAsync(Guid progressId);
        Task<LessonProgressResponse> GetProgressByStudentAndLessonAsync(Guid studentId, Guid lessonId);
        Task<LessonProgressResponse> GetProgressByStudentAsync(Guid studentId);
        Task<LessonProgressResponse> GetProgressByCourseAsync(Guid courseId);
        Task<LessonProgressResponse> GetCourseProgressAsync(Guid studentId, Guid courseId);
        Task<LessonProgressResponse> UpdateProgressAsync(Guid progressId, UpdateLessonProgressRequest request);
        Task<LessonProgressResponse> MarkLessonCompleteAsync(Guid progressId);
        Task<OverallStatsResponse> GetOverallStatsAsync(Guid studentId);
    }
}
