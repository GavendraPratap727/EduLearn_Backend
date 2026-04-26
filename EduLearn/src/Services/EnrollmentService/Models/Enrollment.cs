using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduLearn.EnrollmentService.Models
{
    public enum EnrollmentStatus
    {
        ACTIVE = 0,
        COMPLETED = 1,
        DROPPED = 2
    }

    [Table("Enrollments")]
    public class Enrollment
    {
        [Key]
        public Guid EnrollmentId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid CourseId { get; set; }

        [Required]
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [Required]
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.ACTIVE;

        [Required]
        [Range(0, 100)]
        public int ProgressPercent { get; set; } = 0;

        public DateTime? LastAccessedAt { get; set; }

        [Required]
        public bool CertificateIssued { get; set; } = false;

        public string? PaymentId { get; set; }
    }
}
