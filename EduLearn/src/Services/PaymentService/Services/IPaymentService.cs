using EduLearn.PaymentService.Dtos;

namespace EduLearn.PaymentService.Services
{
    public interface IPaymentService
    {
        Task<PaymentOrderResponse> CreatePaymentOrderAsync(Guid studentId, CreatePaymentOrderRequest request);
        Task<VerifyPaymentResponse> VerifyPaymentAsync(Guid studentId, VerifyPaymentRequest request);
        Task<PaymentStatusResponse> GetPaymentStatusAsync(Guid paymentId, Guid studentId);
        Task<IEnumerable<PaymentDto>> GetStudentPaymentsAsync(Guid studentId);
        Task<IEnumerable<PaymentDto>> GetCoursePaymentsAsync(Guid courseId);
        Task<RefundResponse> RefundPaymentAsync(Guid paymentId, RefundPaymentRequest request);
        Task<decimal> GetTotalRevenueAsync();
        Task<int> GetPaymentCountAsync(string status = "Completed");
    }
}
