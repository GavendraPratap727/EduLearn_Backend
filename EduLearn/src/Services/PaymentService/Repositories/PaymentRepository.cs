using EduLearn.PaymentService.Data;
using EduLearn.PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.PaymentService.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _context;

        public PaymentRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<Payment?> GetByIdAsync(Guid paymentId)
        {
            return await _context.Payments
                .Include(p => p.Student)
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }

        public async Task<Payment?> GetByRazorpayOrderIdAsync(string razorpayOrderId)
        {
            return await _context.Payments
                .Include(p => p.Student)
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.RazorpayOrderId == razorpayOrderId);
        }

        public async Task<Payment?> GetByRazorpayPaymentIdAsync(string razorpayPaymentId)
        {
            return await _context.Payments
                .Include(p => p.Student)
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.RazorpayPaymentId == razorpayPaymentId);
        }

        public async Task<IEnumerable<Payment>> GetByStudentIdAsync(Guid studentId)
        {
            return await _context.Payments
                .Include(p => p.Student)
                .Include(p => p.Course)
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetByCourseIdAsync(Guid courseId)
        {
            return await _context.Payments
                .Include(p => p.Student)
                .Include(p => p.Course)
                .Where(p => p.CourseId == courseId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<bool> DeleteAsync(Guid paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
                return false;

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CountByStudentIdAsync(Guid studentId)
        {
            return await _context.Payments
                .CountAsync(p => p.StudentId == studentId && p.Status == "Completed");
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Payments
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.Amount);
        }

        public async Task<int> GetPaymentCountAsync(string status = "Completed")
        {
            return await _context.Payments
                .CountAsync(p => p.Status == status);
        }
    }
}
