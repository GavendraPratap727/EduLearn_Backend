namespace EduLearn.PaymentService.Dtos
{
    // Request DTOs
    public class CreatePaymentOrderRequest
    {
        public Guid CourseId { get; set; }
        public string PaymentType { get; set; } = "Course"; // Course, Certificate, Subscription
        public decimal? Amount { get; set; } // Optional, will be fetched from course if not provided
        public string Currency { get; set; } = "INR";
        public string Receipt { get; set; } = string.Empty;
    }

    public class VerifyPaymentRequest
    {
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
        public Guid PaymentId { get; set; }
    }

    public class RefundPaymentRequest
    {
        public Guid PaymentId { get; set; }
        public decimal? Amount { get; set; } // Optional, full refund if not provided
        public string Reason { get; set; } = string.Empty;
    }

    // Response DTOs
    public class PaymentOrderResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PaymentOrderDto? Order { get; set; }
    }

    public class PaymentOrderDto
    {
        public Guid PaymentId { get; set; }
        public string RazorpayOrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Receipt { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class VerifyPaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PaymentDto? Payment { get; set; }
    }

    public class PaymentDto
    {
        public Guid PaymentId { get; set; }
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayPaymentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class PaymentStatusResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PaymentDto? Payment { get; set; }
    }

    public class RefundResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RefundId { get; set; }
    }

    // Razorpay DTOs
    public class RazorpayOrderRequest
    {
        public decimal Amount { get; set; } // Amount in paise (multiply by 100)
        public string Currency { get; set; } = "INR";
        public string Receipt { get; set; } = string.Empty;
        public object? Notes { get; set; }
    }

    public class RazorpayOrderResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Receipt { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public List<RazorpayNote>? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RazorpayNote
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
