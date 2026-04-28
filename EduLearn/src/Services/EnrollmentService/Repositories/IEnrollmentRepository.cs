using EduLearn.EnrollmentService.Models;

namespace EduLearn.EnrollmentService.Repositories
{
    public interface IEnrollmentRepository
    {
        Task<Enrollment?> FindByEnrollmentIdAsync(Guid enrollmentId);
        Task<List<Enrollment>> FindByStudentIdAsync(Guid studentId);
        Task<List<Enrollment>> FindByCourseIdAsync(Guid courseId);
        Task<Enrollment?> FindByStudentAndCourseAsync(Guid studentId, Guid courseId);
        Task<bool> IsEnrolledAsync(Guid studentId, Guid courseId);
        Task<List<Enrollment>> FindCompletedAsync(Guid studentId);
        Task<List<Enrollment>> FindInProgressAsync(Guid studentId);
        Task<int> CountByCourseIdAsync(Guid courseId);
        Task<Enrollment> AddAsync(Enrollment enrollment);
        Task<Enrollment> UpdateAsync(Enrollment enrollment);
        Task DeleteAsync(Guid enrollmentId);
        Task DeleteByStudentAndCourseAsync(Guid studentId, Guid courseId);
    }
}
