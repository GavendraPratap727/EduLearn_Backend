using EduLearn.CourseService.Models;

namespace EduLearn.CourseService.Services
{
    public interface ICourseService
    {
        Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request);
        Task<CourseResponse> GetCourseByIdAsync(Guid courseId);
        Task<CourseResponse> GetCoursesByInstructorAsync(Guid instructorId);
        Task<CourseResponse> GetCoursesByCategoryAsync(string category);
        Task<CourseResponse> GetPublishedCoursesAsync();
        Task<CourseResponse> SearchCoursesAsync(string keyword);
        Task<CourseResponse> UpdateCourseAsync(Guid courseId, UpdateCourseRequest request);
        Task<CourseResponse> PublishCourseAsync(Guid courseId);
        Task<CourseResponse> ApproveCourseAsync(Guid courseId);
        Task<CourseResponse> DeleteCourseAsync(Guid courseId);
        Task<CourseResponse> GetTopRatedCoursesAsync(int limit);
        Task IncrementEnrollmentAsync(Guid courseId);
    }
}
