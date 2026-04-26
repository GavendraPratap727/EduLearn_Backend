using EduLearn.EnrollmentService.Models;
using EduLearn.EnrollmentService.Repositories;
using System.Net.Http.Json;

namespace EduLearn.EnrollmentService.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IEnrollmentRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public EnrollmentService(IEnrollmentRepository repository, HttpClient httpClient, IConfiguration configuration)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<EnrollmentResponse> EnrollAsync(CreateEnrollmentRequest request)
        {
            // Check if already enrolled
            if (await _repository.IsEnrolledAsync(request.StudentId, request.CourseId))
            {
                return new EnrollmentResponse
                {
                    Success = false,
                    Message = "Student is already enrolled in this course"
                };
            }

            var enrollment = new Enrollment
            {
                EnrollmentId = Guid.NewGuid(),
                StudentId = request.StudentId,
                CourseId = request.CourseId,
                EnrolledAt = DateTime.UtcNow,
                Status = EnrollmentStatus.ACTIVE,
                ProgressPercent = 0,
                CertificateIssued = false,
                PaymentId = request.PaymentId
            };

            var createdEnrollment = await _repository.AddAsync(enrollment);

            // Call CourseService to increment enrollment count
            try
            {
                var courseServiceUrl = _configuration["CourseService:Url"] ?? "http://localhost:5001";
                var response = await _httpClient.PostAsJsonAsync($"{courseServiceUrl}/api/courses/{request.CourseId}/increment-enrollment", new { });
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                // Log error but don't fail enrollment if CourseService call fails
                Console.WriteLine($"Failed to increment enrollment count: {ex.Message}");
            }

            return new EnrollmentResponse
            {
                Success = true,
                Message = "Enrollment successful",
                Enrollment = MapToEnrollmentDto(createdEnrollment)
            };
        }

        public async Task<EnrollmentResponse> GetEnrollmentByIdAsync(Guid enrollmentId)
        {
            var enrollment = await _repository.FindByEnrollmentIdAsync(enrollmentId);
            if (enrollment == null)
            {
                return new EnrollmentResponse
                {
                    Success = false,
                    Message = "Enrollment not found"
                };
            }

            return new EnrollmentResponse
            {
                Success = true,
                Message = "Enrollment found",
                Enrollment = MapToEnrollmentDto(enrollment)
            };
        }

        public async Task<EnrollmentResponse> GetEnrollmentsByStudentAsync(Guid studentId)
        {
            var enrollments = await _repository.FindByStudentIdAsync(studentId);
            return new EnrollmentResponse
            {
                Success = true,
                Message = "Enrollments found",
                Enrollments = enrollments.Select(MapToEnrollmentDto).ToList()
            };
        }

        public async Task<EnrollmentResponse> GetEnrollmentsByCourseAsync(Guid courseId)
        {
            var enrollments = await _repository.FindByCourseIdAsync(courseId);
            return new EnrollmentResponse
            {
                Success = true,
                Message = "Enrollments found",
                Enrollments = enrollments.Select(MapToEnrollmentDto).ToList()
            };
        }

        public async Task<EnrollmentResponse> IsEnrolledAsync(Guid studentId, Guid courseId)
        {
            var isEnrolled = await _repository.IsEnrolledAsync(studentId, courseId);
            return new EnrollmentResponse
            {
                Success = true,
                Message = isEnrolled ? "Student is enrolled" : "Student is not enrolled"
            };
        }

        public async Task<EnrollmentResponse> UpdateProgressAsync(Guid enrollmentId, UpdateProgressRequest request)
        {
            var enrollment = await _repository.FindByEnrollmentIdAsync(enrollmentId);
            if (enrollment == null)
            {
                return new EnrollmentResponse
                {
                    Success = false,
                    Message = "Enrollment not found"
                };
            }

            enrollment.ProgressPercent = request.ProgressPercent;
            enrollment.LastAccessedAt = DateTime.UtcNow;

            if (request.ProgressPercent >= 100)
            {
                enrollment.Status = EnrollmentStatus.COMPLETED;
                enrollment.CompletedAt = DateTime.UtcNow;
            }

            var updatedEnrollment = await _repository.UpdateAsync(enrollment);

            return new EnrollmentResponse
            {
                Success = true,
                Message = "Progress updated successfully",
                Enrollment = MapToEnrollmentDto(updatedEnrollment)
            };
        }

        public async Task<EnrollmentResponse> CompleteEnrollmentAsync(Guid enrollmentId)
        {
            var enrollment = await _repository.FindByEnrollmentIdAsync(enrollmentId);
            if (enrollment == null)
            {
                return new EnrollmentResponse
                {
                    Success = false,
                    Message = "Enrollment not found"
                };
            }

            enrollment.Status = EnrollmentStatus.COMPLETED;
            enrollment.ProgressPercent = 100;
            enrollment.CompletedAt = DateTime.UtcNow;
            enrollment.LastAccessedAt = DateTime.UtcNow;

            var updatedEnrollment = await _repository.UpdateAsync(enrollment);

            return new EnrollmentResponse
            {
                Success = true,
                Message = "Enrollment completed successfully",
                Enrollment = MapToEnrollmentDto(updatedEnrollment)
            };
        }

        public async Task<EnrollmentResponse> DropCourseAsync(Guid enrollmentId)
        {
            var enrollment = await _repository.FindByEnrollmentIdAsync(enrollmentId);
            if (enrollment == null)
            {
                return new EnrollmentResponse
                {
                    Success = false,
                    Message = "Enrollment not found"
                };
            }

            enrollment.Status = EnrollmentStatus.DROPPED;
            var updatedEnrollment = await _repository.UpdateAsync(enrollment);

            return new EnrollmentResponse
            {
                Success = true,
                Message = "Course dropped successfully",
                Enrollment = MapToEnrollmentDto(updatedEnrollment)
            };
        }

        public async Task<EnrollmentResponse> GetCompletedCoursesAsync(Guid studentId)
        {
            var enrollments = await _repository.FindCompletedAsync(studentId);
            return new EnrollmentResponse
            {
                Success = true,
                Message = "Completed courses found",
                Enrollments = enrollments.Select(MapToEnrollmentDto).ToList()
            };
        }

        public async Task<EnrollmentResponse> GetInProgressCoursesAsync(Guid studentId)
        {
            var enrollments = await _repository.FindInProgressAsync(studentId);
            return new EnrollmentResponse
            {
                Success = true,
                Message = "In-progress courses found",
                Enrollments = enrollments.Select(MapToEnrollmentDto).ToList()
            };
        }

        public async Task<EnrollmentResponse> GetEnrollmentCountAsync(Guid courseId)
        {
            var count = await _repository.CountByCourseIdAsync(courseId);
            return new EnrollmentResponse
            {
                Success = true,
                Message = $"Total enrollments: {count}"
            };
        }

        private EnrollmentDto MapToEnrollmentDto(Enrollment enrollment)
        {
            return new EnrollmentDto
            {
                EnrollmentId = enrollment.EnrollmentId,
                StudentId = enrollment.StudentId,
                CourseId = enrollment.CourseId,
                EnrolledAt = enrollment.EnrolledAt,
                CompletedAt = enrollment.CompletedAt,
                Status = enrollment.Status,
                ProgressPercent = enrollment.ProgressPercent,
                LastAccessedAt = enrollment.LastAccessedAt,
                CertificateIssued = enrollment.CertificateIssued,
                PaymentId = enrollment.PaymentId
            };
        }
    }
}
