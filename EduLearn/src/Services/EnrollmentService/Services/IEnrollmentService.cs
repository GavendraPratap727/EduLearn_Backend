using EduLearn.EnrollmentService.Models;

namespace EduLearn.EnrollmentService.Services
{
    public interface IEnrollmentService
    {
        Task<EnrollmentResponse> EnrollAsync(CreateEnrollmentRequest request);
        Task<EnrollmentResponse> GetEnrollmentByIdAsync(Guid enrollmentId);
        Task<EnrollmentResponse> GetEnrollmentsByStudentAsync(Guid studentId);
        Task<EnrollmentResponse> GetEnrollmentsByCourseAsync(Guid courseId);
        Task<EnrollmentResponse> IsEnrolledAsync(Guid studentId, Guid courseId);
        Task<EnrollmentResponse> UpdateProgressAsync(Guid enrollmentId, UpdateProgressRequest request);
        Task<EnrollmentResponse> CompleteEnrollmentAsync(Guid enrollmentId);
        Task<EnrollmentResponse> DropCourseAsync(Guid enrollmentId);
        Task<EnrollmentResponse> GetCompletedCoursesAsync(Guid studentId);
        Task<EnrollmentResponse> GetInProgressCoursesAsync(Guid studentId);
        Task<EnrollmentResponse> GetEnrollmentCountAsync(Guid courseId);
    }
}
