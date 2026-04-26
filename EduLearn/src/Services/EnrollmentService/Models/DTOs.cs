using System.ComponentModel.DataAnnotations;

namespace EduLearn.EnrollmentService.Models
{
    public class EnrollmentDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public EnrollmentStatus Status { get; set; }
        public int ProgressPercent { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public bool CertificateIssued { get; set; }
        public string? PaymentId { get; set; }
    }

    public class CreateEnrollmentRequest
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid CourseId { get; set; }

        public string? PaymentId { get; set; }
    }

    public class UpdateProgressRequest
    {
        [Required]
        [Range(0, 100)]
        public int ProgressPercent { get; set; }
    }

    public class EnrollmentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public EnrollmentDto? Enrollment { get; set; }
        public List<EnrollmentDto>? Enrollments { get; set; }
    }
}
