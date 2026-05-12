using EduLearn.LessonService.Models;

namespace EduLearn.LessonService.Services
{
    public interface ILessonService
    {
        Task<LessonResponse> AddLessonAsync(CreateLessonRequest request);
        Task<LessonResponse> GetLessonByIdAsync(Guid lessonId);
        Task<LessonResponse> GetLessonsByCourseAsync(Guid courseId);
        Task<LessonResponse> GetOrderedLessonsAsync(Guid courseId);
        Task<LessonResponse> GetPreviewLessonsAsync(Guid courseId);
        Task<LessonResponse> UpdateLessonAsync(Guid lessonId, UpdateLessonRequest request);
        Task<LessonResponse> ReorderLessonsAsync(Guid courseId, List<Guid> lessonIds);
        Task<LessonResponse> PublishLessonAsync(Guid lessonId);
        Task<LessonResponse> DeleteLessonAsync(Guid lessonId);
        Task<LessonResponse> DeleteAllForCourseAsync(Guid courseId);
        Task<LessonResponse> GetLessonCountAsync(Guid courseId);
    }
}
