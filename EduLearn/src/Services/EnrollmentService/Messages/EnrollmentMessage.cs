using System;

namespace EduLearn.EnrollmentService.Messages
{
    public class EnrollmentCreatedMessage
    {
        public Guid EnrollmentId { get; set; }
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public string EventType { get; set; } = "EnrollmentCreated";
    }

    public class EnrollmentUpdatedMessage
    {
        public Guid EnrollmentId { get; set; }
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string EventType { get; set; } = "EnrollmentUpdated";
    }

    public class EnrollmentCancelledMessage
    {
        public Guid EnrollmentId { get; set; }
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public DateTime CancelledAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string EventType { get; set; } = "EnrollmentCancelled";
    }
}
