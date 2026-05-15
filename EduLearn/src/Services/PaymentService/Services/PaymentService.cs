using EduLearn.PaymentService.Data;
using EduLearn.PaymentService.Dtos;
using EduLearn.PaymentService.Models;
using EduLearn.PaymentService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.PaymentService.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repository;
        private readonly IRazorpayService _razorpayService;
        private readonly PaymentDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PaymentService(
            IPaymentRepository repository,
            IRazorpayService razorpayService,
            PaymentDbContext context,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _repository = repository;
            _razorpayService = razorpayService;
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<PaymentOrderResponse> CreatePaymentOrderAsync(Guid studentId, CreatePaymentOrderRequest request)
        {
            try
            {
                // Get course details to determine amount
                var (course, error) = await GetCourseDetailsInternalAsync(request.CourseId);
                if (course == null)
                {
                    return new PaymentOrderResponse
                    {
                        Success = false,
                        Message = $"Course not found: {error}"
                    };
                }

                // Determine amount based on payment type
                decimal amount = request.Amount ?? course.Price;
                if (request.PaymentType == "Certificate")
                {
                    amount = 500; // Fixed certificate fee
                }
                else if (request.PaymentType == "Subscription")
                {
                    amount = 999; // Monthly subscription fee
                }

                // Ensure student exists
                await EnsureStudentExistsAsync(studentId);

                // Create payment record
                var payment = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    StudentId = studentId,
                    CourseId = request.CourseId,
                    Amount = amount,
                    Currency = request.Currency,
                    Status = "Pending",
                    PaymentType = request.PaymentType,
                    CreatedAt = DateTime.UtcNow
                };

                payment = await _repository.CreateAsync(payment);

                // Create Razorpay order
                var razorpayOrderRequest = new RazorpayOrderRequest
                {
                    Amount = amount,
                    Currency = request.Currency,
                    Receipt = $"rcpt_{payment.PaymentId:N}".Substring(0, Math.Min(39, $"rcpt_{payment.PaymentId:N}".Length))
                };

                var razorpayOrder = await _razorpayService.CreateOrderAsync(razorpayOrderRequest, request.CourseId, request.PaymentType);

                // Update payment with Razorpay order ID
                payment.RazorpayOrderId = razorpayOrder.Id;
                payment = await _repository.UpdateAsync(payment);

                return new PaymentOrderResponse
                {
                    Success = true,
                    Message = "Payment order created successfully",
                    Order = new PaymentOrderDto
                    {
                        PaymentId = payment.PaymentId,
                        RazorpayOrderId = razorpayOrder.Id,
                        Amount = amount,
                        Currency = request.Currency,
                        Receipt = razorpayOrder.Receipt,
                        Status = razorpayOrder.Status,
                        CreatedAt = razorpayOrder.CreatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                return new PaymentOrderResponse
                {
                    Success = false,
                    Message = $"Failed to create payment order: {ex.Message}"
                };
            }
        }

        public async Task<VerifyPaymentResponse> VerifyPaymentAsync(Guid studentId, VerifyPaymentRequest request)
        {
            try
            {
                // Get payment record
                var payment = await _repository.GetByIdAsync(request.PaymentId);
                if (payment == null)
                {
                    return new VerifyPaymentResponse
                    {
                        Success = false,
                        Message = "Payment not found"
                    };
                }

                if (payment.StudentId != studentId)
                {
                    return new VerifyPaymentResponse
                    {
                        Success = false,
                        Message = "Unauthorized access to payment"
                    };
                }

                // Verify payment with Razorpay
                var isValid = await _razorpayService.VerifyPaymentAsync(
                    request.RazorpayOrderId,
                    request.RazorpayPaymentId,
                    request.RazorpaySignature
                );

                if (!isValid)
                {
                    payment.Status = "Failed";
                    await _repository.UpdateAsync(payment);

                    return new VerifyPaymentResponse
                    {
                        Success = false,
                        Message = "Invalid payment signature"
                    };
                }

                // Update payment status
                payment.Status = "Completed";
                payment.RazorpayPaymentId = request.RazorpayPaymentId;
                payment.RazorpaySignature = request.RazorpaySignature;
                payment.CompletedAt = DateTime.UtcNow;
                payment = await _repository.UpdateAsync(payment);

                // Trigger enrollment if it's a course payment
                if (payment.PaymentType == "Course")
                {
                    await TriggerEnrollmentAsync(studentId, payment.CourseId);
                }

                return new VerifyPaymentResponse
                {
                    Success = true,
                    Message = "Payment verified successfully",
                    Payment = new PaymentDto
                    {
                        PaymentId = payment.PaymentId,
                        StudentId = payment.StudentId,
                        CourseId = payment.CourseId,
                        Amount = payment.Amount,
                        Currency = payment.Currency,
                        Status = payment.Status,
                        PaymentType = payment.PaymentType,
                        RazorpayOrderId = payment.RazorpayOrderId,
                        RazorpayPaymentId = payment.RazorpayPaymentId,
                        CreatedAt = payment.CreatedAt,
                        CompletedAt = payment.CompletedAt
                    }
                };
            }
            catch (Exception ex)
            {
                return new VerifyPaymentResponse
                {
                    Success = false,
                    Message = $"Failed to verify payment: {ex.Message}"
                };
            }
        }

        public async Task<PaymentStatusResponse> GetPaymentStatusAsync(Guid paymentId, Guid studentId)
        {
            try
            {
                var payment = await _repository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return new PaymentStatusResponse
                    {
                        Success = false,
                        Message = "Payment not found"
                    };
                }

                if (payment.StudentId != studentId)
                {
                    return new PaymentStatusResponse
                    {
                        Success = false,
                        Message = "Unauthorized access to payment"
                    };
                }

                return new PaymentStatusResponse
                {
                    Success = true,
                    Message = "Payment status retrieved successfully",
                    Payment = new PaymentDto
                    {
                        PaymentId = payment.PaymentId,
                        StudentId = payment.StudentId,
                        CourseId = payment.CourseId,
                        Amount = payment.Amount,
                        Currency = payment.Currency,
                        Status = payment.Status,
                        PaymentType = payment.PaymentType,
                        RazorpayOrderId = payment.RazorpayOrderId,
                        RazorpayPaymentId = payment.RazorpayPaymentId,
                        CreatedAt = payment.CreatedAt,
                        CompletedAt = payment.CompletedAt
                    }
                };
            }
            catch (Exception ex)
            {
                return new PaymentStatusResponse
                {
                    Success = false,
                    Message = $"Failed to get payment status: {ex.Message}"
                };
            }
        }

        public async Task<IEnumerable<PaymentDto>> GetStudentPaymentsAsync(Guid studentId)
        {
            var payments = await _repository.GetByStudentIdAsync(studentId);
            return payments.Select(p => new PaymentDto
            {
                PaymentId = p.PaymentId,
                StudentId = p.StudentId,
                CourseId = p.CourseId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                PaymentType = p.PaymentType,
                RazorpayOrderId = p.RazorpayOrderId,
                RazorpayPaymentId = p.RazorpayPaymentId,
                CreatedAt = p.CreatedAt,
                CompletedAt = p.CompletedAt
            });
        }

        public async Task<IEnumerable<PaymentDto>> GetCoursePaymentsAsync(Guid courseId)
        {
            var payments = await _repository.GetByCourseIdAsync(courseId);
            return payments.Select(p => new PaymentDto
            {
                PaymentId = p.PaymentId,
                StudentId = p.StudentId,
                CourseId = p.CourseId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                PaymentType = p.PaymentType,
                RazorpayOrderId = p.RazorpayOrderId,
                RazorpayPaymentId = p.RazorpayPaymentId,
                CreatedAt = p.CreatedAt,
                CompletedAt = p.CompletedAt
            });
        }

        public async Task<RefundResponse> RefundPaymentAsync(Guid paymentId, RefundPaymentRequest request)
        {
            try
            {
                var payment = await _repository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return new RefundResponse
                    {
                        Success = false,
                        Message = "Payment not found"
                    };
                }

                if (payment.Status != "Completed")
                {
                    return new RefundResponse
                    {
                        Success = false,
                        Message = "Only completed payments can be refunded"
                    };
                }

                if (string.IsNullOrEmpty(payment.RazorpayPaymentId))
                {
                    return new RefundResponse
                    {
                        Success = false,
                        Message = "No Razorpay payment ID found for refund"
                    };
                }

                // Process refund with Razorpay
                var refundSuccess = await _razorpayService.ProcessRefundAsync(
                    payment.RazorpayPaymentId,
                    request.Amount
                );

                if (!refundSuccess)
                {
                    return new RefundResponse
                    {
                        Success = false,
                        Message = "Failed to process refund"
                    };
                }

                // Update payment status
                payment.Status = "Refunded";
                payment.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(payment);

                return new RefundResponse
                {
                    Success = true,
                    Message = "Refund processed successfully",
                    RefundId = $"refund_{payment.PaymentId}"
                };
            }
            catch (Exception ex)
            {
                return new RefundResponse
                {
                    Success = false,
                    Message = $"Failed to process refund: {ex.Message}"
                };
            }
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _repository.GetTotalRevenueAsync();
        }

        public async Task<int> GetPaymentCountAsync(string status = "Completed")
        {
            return await _repository.GetPaymentCountAsync(status);
        }

        private async Task EnsureStudentExistsAsync(Guid studentId)
        {
            try
            {
                // Check if student exists in database
                var existingStudent = await _context.Students.FindAsync(studentId);
                if (existingStudent != null)
                {
                    return;
                }

                // Create the student if it doesn't exist
                var newStudent = new Student
                {
                    StudentId = studentId,
                    Name = "Student User",
                    Email = $"student_{studentId:N}@edulearn.com",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Students.Add(newStudent);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Detach the failed entity to avoid poisoning the context
                var entry = _context.ChangeTracker.Entries<Student>().FirstOrDefault(e => e.Entity.StudentId == studentId);
                if (entry != null)
                {
                    entry.State = EntityState.Detached;
                }
            }
        }

        private async Task<(Course? course, string? error)> GetCourseDetailsInternalAsync(Guid courseId)
        {
            try
            {
                // First check if course exists in database
                var existingCourse = await _context.Courses.FindAsync(courseId);
                if (existingCourse != null)
                {
                    return (existingCourse, null);
                }

                // Create the course if it doesn't exist
                var newCourse = new Course
                {
                    CourseId = courseId,
                    Title = $"Course {courseId.ToString().Substring(0, 8)}",
                    Price = 999, // Default price
                    IsPaid = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Courses.Add(newCourse);
                await _context.SaveChangesAsync();
                
                return (newCourse, null);
            }
            catch (Exception ex)
            {
                // Detach the failed entity to avoid poisoning the context
                var entry = _context.ChangeTracker.Entries<Course>().FirstOrDefault(e => e.Entity.CourseId == courseId);
                if (entry != null)
                {
                    entry.State = EntityState.Detached;
                }
                
                var errorMsg = ex.InnerException != null ? $"{ex.Message} | {ex.InnerException.Message}" : ex.Message;
                return (null, errorMsg);
            }
        }

        // Keep the old method for compatibility if needed elsewhere
        private async Task<Course?> GetCourseDetailsAsync(Guid courseId)
        {
            var (course, _) = await GetCourseDetailsInternalAsync(courseId);
            return course;
        }

        private async Task TriggerEnrollmentAsync(Guid studentId, Guid courseId)
        {
            try
            {
                var enrollmentServiceUrl = _configuration["EnrollmentService:Url"] ?? "http://localhost:5003";
                var enrollmentRequest = new
                {
                    StudentId = studentId,
                    CourseId = courseId
                };

                // This would create an enrollment after successful payment
                // In a real implementation, you'd have proper error handling
                var response = await _httpClient.PostAsJsonAsync(
                    $"{enrollmentServiceUrl}/api/enrollments",
                    enrollmentRequest
                );
            }
            catch
            {
                // Log error but don't fail the payment verification
                // Enrollment can be handled separately
            }
        }
    }
}
