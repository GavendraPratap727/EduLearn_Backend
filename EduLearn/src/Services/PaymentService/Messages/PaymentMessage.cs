using System;

namespace EduLearn.PaymentService.Messages
{
    public class PaymentProcessedMessage
    {
        public Guid PaymentId { get; set; }
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public bool IsSuccessful { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string EventType { get; set; } = "PaymentProcessed";
    }

    public class PaymentFailedMessage
    {
        public Guid PaymentId { get; set; }
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime FailedAt { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string EventType { get; set; } = "PaymentFailed";
    }

    public class PaymentRefundedMessage
    {
        public Guid PaymentId { get; set; }
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public decimal RefundAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime RefundedAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string EventType { get; set; } = "PaymentRefunded";
    }
}
