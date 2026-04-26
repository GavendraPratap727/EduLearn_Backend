using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduLearn.ProgressService.Models
{
    public class Certificate
    {
        [Key]
        [Column(TypeName = "char(36)")]
        public string CertificateId { get; set; } = Guid.NewGuid().ToString("N").ToUpper();

        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid CourseId { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? CertificateUrl { get; set; }

        [Required]
        [MaxLength(36)]
        public string VerificationCode { get; set; } = Guid.NewGuid().ToString("N").ToUpper();

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual LessonProgress? Student { get; set; }
    }
}
