using System.ComponentModel.DataAnnotations;

namespace EduLearn.PaymentService.Models
{
    public class Payment
    {
        [Key]
        public Guid PaymentId { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid StudentId { get; set; }
        
        [Required]
        public Guid CourseId { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        public string Currency { get; set; } = "INR";
        
        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Refunded
        
        [Required]
        public string PaymentType { get; set; } = "Course"; // Course, Certificate, Subscription
        
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayPaymentId { get; set; }
        public string? RazorpaySignature { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Navigation properties
        public Student? Student { get; set; }
        public Course? Course { get; set; }
    }
    
    public class Student
    {
        [Key]
        public Guid StudentId { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
    
    public class Course
    {
        [Key]
        public Guid CourseId { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public decimal Price { get; set; }
        
        public bool IsPaid { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
