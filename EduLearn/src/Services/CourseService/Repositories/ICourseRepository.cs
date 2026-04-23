using EduLearn.CourseService.Models;

namespace EduLearn.CourseService.Repositories
{
    public interface ICourseRepository
    {
        Task<Course?> FindByCourseIdAsync(Guid courseId);
        Task<List<Course>> FindByInstructorIdAsync(Guid instructorId);
        Task<List<Course>> FindByCategoryAsync(string category);
        Task<List<Course>> FindPublishedCoursesAsync();
        Task<List<Course>> SearchCoursesAsync(string keyword);
        Task<List<Course>> FindTopRatedAsync(int limit);
        Task<int> CountByInstructorIdAsync(Guid instructorId);
        Task<Course> AddAsync(Course course);
        Task<Course> UpdateAsync(Course course);
        Task DeleteAsync(Guid courseId);
        Task IncrementEnrollmentAsync(Guid courseId);
    }
}
