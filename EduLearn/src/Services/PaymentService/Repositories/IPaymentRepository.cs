using EduLearn.PaymentService.Models;

namespace EduLearn.PaymentService.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(Guid paymentId);
        Task<Payment?> GetByRazorpayOrderIdAsync(string razorpayOrderId);
        Task<Payment?> GetByRazorpayPaymentIdAsync(string razorpayPaymentId);
        Task<IEnumerable<Payment>> GetByStudentIdAsync(Guid studentId);
        Task<IEnumerable<Payment>> GetByCourseIdAsync(Guid courseId);
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment> UpdateAsync(Payment payment);
        Task<bool> DeleteAsync(Guid paymentId);
        Task<int> CountByStudentIdAsync(Guid studentId);
        Task<decimal> GetTotalRevenueAsync();
        Task<int> GetPaymentCountAsync(string status = "Completed");
    }
}
