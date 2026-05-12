using EduLearn.ProgressService.Models;
using EduLearn.ProgressService.Repositories;
using System.Net.Http.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace EduLearn.ProgressService.Services
{
    public class ProgressService : IProgressService
    {
        private readonly IProgressRepository _repository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ProgressService(IProgressRepository repository, HttpClient httpClient, IConfiguration configuration)
        {
            _repository = repository;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<LessonProgressResponse> CreateProgressAsync(CreateLessonProgressRequest request)
        {
            // Check if progress already exists
            var existing = await _repository.FindByStudentAndLessonAsync(request.StudentId, request.LessonId);
            if (existing != null)
            {
                return new LessonProgressResponse
                {
                    Success = false,
                    Message = "Progress record already exists"
                };
            }

            var progress = new LessonProgress
            {
                ProgressId = Guid.NewGuid(),
                StudentId = request.StudentId,
                LessonId = request.LessonId,
                CourseId = request.CourseId,
                IsCompleted = false,
                WatchedSeconds = 0,
                CreatedAt = DateTime.UtcNow
            };

            var createdProgress = await _repository.AddAsync(progress);

            return new LessonProgressResponse
            {
                Success = true,
                Message = "Progress created",
                Progress = MapToLessonProgressDto(createdProgress)
            };
        }

        public async Task<LessonProgressResponse> GetProgressByIdAsync(Guid progressId)
        {
            var progress = await _repository.FindByProgressIdAsync(progressId);
            if (progress == null)
            {
                return new LessonProgressResponse
                {
                    Success = false,
                    Message = "Progress not found"
                };
            }

            return new LessonProgressResponse
            {
                Success = true,
                Message = "Progress found",
                Progress = MapToLessonProgressDto(progress)
            };
        }

        public async Task<LessonProgressResponse> GetProgressByStudentAndLessonAsync(Guid studentId, Guid lessonId)
        {
            var progress = await _repository.FindByStudentAndLessonAsync(studentId, lessonId);
            if (progress == null)
            {
                return new LessonProgressResponse
                {
                    Success = false,
                    Message = "Progress not found"
                };
            }

            return new LessonProgressResponse
            {
                Success = true,
                Message = "Progress found",
                Progress = MapToLessonProgressDto(progress)
            };
        }

        public async Task<LessonProgressResponse> GetProgressByStudentAsync(Guid studentId)
        {
            var progressList = await _repository.FindByStudentAsync(studentId);
            return new LessonProgressResponse
            {
                Success = true,
                Message = "Progress found",
                ProgressList = progressList.Select(MapToLessonProgressDto).ToList()
            };
        }

        public async Task<LessonProgressResponse> GetProgressByCourseAsync(Guid courseId)
        {
            var progressList = await _repository.FindByCourseAsync(courseId);
            return new LessonProgressResponse
            {
                Success = true,
                Message = "Progress found",
                ProgressList = progressList.Select(MapToLessonProgressDto).ToList()
            };
        }

        public async Task<LessonProgressResponse> GetCourseProgressAsync(Guid studentId, Guid courseId)
        {
            var progressList = await _repository.FindByStudentAndCourseAsync(studentId, courseId);
            
            int totalItems = 0;
            int completedItems = 0;

            // 1. Get total lessons from LessonService
            try
            {
                var lessonServiceUrl = _configuration["LessonService:Url"] ?? "http://localhost:5002";
                var response = await _httpClient.GetAsync($"{lessonServiceUrl}/api/lessons/course/{courseId}/count");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>();
                    int totalLessons = result?.count ?? 0;
                    totalItems += totalLessons;
                    completedItems += progressList.Count(p => p.IsCompleted);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get lesson count: {ex.Message}");
            }

            // 2. Get quizzes from QuizService
            try
            {
                var quizServiceUrl = _configuration["QuizService:Url"] ?? "http://localhost:5005";
                var response = await _httpClient.GetAsync($"{quizServiceUrl}/api/quizzes/course/{courseId}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>();
                    // Access quizzes from the response
                    var quizzes = result?.quizzes;
                    if (quizzes != null)
                    {
                        foreach (var quiz in quizzes)
                        {
                            bool isPublished = quiz.isPublished ?? false;
                            if (!isPublished) continue;

                            totalItems++;
                            
                            // Check if student has a passing attempt for this quiz
                            Guid quizId = Guid.Parse(quiz.quizId.ToString());
                            var bestAttemptResponse = await _httpClient.GetAsync($"{quizServiceUrl}/api/quizzes/best-attempt/{studentId}/{quizId}");
                            if (bestAttemptResponse.IsSuccessStatusCode)
                            {
                                var bestAttemptResult = await bestAttemptResponse.Content.ReadFromJsonAsync<dynamic>();
                                if (bestAttemptResult?.success == true && bestAttemptResult?.attempt != null)
                                {
                                    bool isPassed = bestAttemptResult?.attempt?.isPassed ?? false;
                                    if (isPassed)
                                    {
                                        completedItems++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get quiz progress: {ex.Message}");
            }

            var progressPercent = totalItems > 0 ? (completedItems * 100) / totalItems : 0;

            return new LessonProgressResponse
            {
                Success = true,
                Message = "Course progress calculated",
                ProgressList = progressList.Select(MapToLessonProgressDto).ToList(),
                CourseProgressPercent = progressPercent
            };
        }

        public async Task<LessonProgressResponse> UpdateProgressAsync(Guid progressId, UpdateLessonProgressRequest request)
        {
            var progress = await _repository.FindByProgressIdAsync(progressId);
            if (progress == null)
            {
                return new LessonProgressResponse
                {
                    Success = false,
                    Message = "Progress not found"
                };
            }

            if (request.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
            }

            if (request.WatchedSeconds > 0)
            {
                progress.WatchedSeconds = request.WatchedSeconds;
            }

            progress.LastWatchedAt = DateTime.UtcNow;

            var updatedProgress = await _repository.UpdateAsync(progress);

            return new LessonProgressResponse
            {
                Success = true,
                Message = "Progress updated",
                Progress = MapToLessonProgressDto(updatedProgress)
            };
        }

        public async Task<LessonProgressResponse> MarkLessonCompleteAsync(Guid progressId)
        {
            var progress = await _repository.FindByProgressIdAsync(progressId);
            if (progress == null)
            {
                return new LessonProgressResponse
                {
                    Success = false,
                    Message = "Progress not found"
                };
            }

            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
            progress.LastWatchedAt = DateTime.UtcNow;

            var updatedProgress = await _repository.UpdateAsync(progress);

            return new LessonProgressResponse
            {
                Success = true,
                Message = "Lesson marked as complete",
                Progress = MapToLessonProgressDto(updatedProgress)
            };
        }

        public async Task<OverallStatsResponse> GetOverallStatsAsync(Guid studentId)
        {
            var progressList = await _repository.FindByStudentAsync(studentId);
            
            var uniqueCourseIds = progressList.Select(p => p.CourseId).Distinct().ToList();
            int totalCoursesEnrolled = uniqueCourseIds.Count;
            int totalCoursesCompleted = 0;
            int coursesInProgress = 0;

            foreach (var courseId in uniqueCourseIds)
            {
                var courseProgress = await GetCourseProgressAsync(studentId, courseId);
                if (courseProgress.CourseProgressPercent >= 100)
                {
                    totalCoursesCompleted++;
                }
                else if (courseProgress.CourseProgressPercent > 0)
                {
                    coursesInProgress++;
                }
            }

            return new OverallStatsResponse
            {
                Success = true,
                Stats = new Dictionary<string, int>
                {
                    { "totalCoursesEnrolled", totalCoursesEnrolled },
                    { "totalCoursesCompleted", totalCoursesCompleted },
                    { "coursesInProgress", coursesInProgress },
                    { "totalLessonsCompleted", progressList.Count(p => p.IsCompleted) }
                }
            };
        }

        private LessonProgressDto MapToLessonProgressDto(LessonProgress progress)
        {
            return new LessonProgressDto
            {
                ProgressId = progress.ProgressId,
                StudentId = progress.StudentId,
                LessonId = progress.LessonId,
                CourseId = progress.CourseId,
                IsCompleted = progress.IsCompleted,
                WatchedSeconds = progress.WatchedSeconds,
                LastWatchedAt = progress.LastWatchedAt,
                CompletedAt = progress.CompletedAt,
                CreatedAt = progress.CreatedAt
            };
        }

        // Certificate methods
        public async Task<CertificateResponse> IssueCertificateAsync(IssueCertificateRequest request)
        {
            // Check if certificate already exists
            var existingCertificate = await _repository.FindCertificateByStudentAndCourseAsync(request.StudentId, request.CourseId);
            if (existingCertificate != null)
            {
                return new CertificateResponse
                {
                    Success = false,
                    Message = "Certificate already issued for this course"
                };
            }

            // Create certificate entity
            var certificate = new Certificate
            {
                CertificateId = Guid.NewGuid().ToString("N").ToUpper(),
                StudentId = request.StudentId,
                CourseId = request.CourseId,
                IssuedAt = DateTime.UtcNow,
                VerificationCode = Guid.NewGuid().ToString("N").ToUpper()
            };

            // Generate PDF certificate (simplified - in production would upload to Azure Blob)
            var certificateUrl = await GenerateCertificatePdfAsync(certificate);
            certificate.CertificateUrl = certificateUrl;

            var createdCertificate = await _repository.AddCertificateAsync(certificate);

            return new CertificateResponse
            {
                Success = true,
                Message = "Certificate issued successfully",
                Certificate = MapToCertificateDto(createdCertificate)
            };
        }

        public async Task<CertificateResponse> GetCertificateByIdAsync(string certificateId)
        {
            var certificate = await _repository.FindCertificateByIdAsync(certificateId);
            if (certificate == null)
            {
                return new CertificateResponse
                {
                    Success = false,
                    Message = "Certificate not found"
                };
            }

            return new CertificateResponse
            {
                Success = true,
                Message = "Certificate found",
                Certificate = MapToCertificateDto(certificate)
            };
        }

        public async Task<CertificateResponse> GetCertificatesByStudentAsync(Guid studentId)
        {
            var certificates = await _repository.FindCertificatesByStudentAsync(studentId);
            return new CertificateResponse
            {
                Success = true,
                Message = $"Found {certificates.Count} certificates"
            };
        }

        public async Task<CertificateResponse> VerifyCertificateAsync(string verificationCode)
        {
            var certificate = await _repository.FindCertificateByVerificationCodeAsync(verificationCode);
            if (certificate == null)
            {
                return new CertificateResponse
                {
                    Success = false,
                    Message = "Invalid verification code"
                };
            }

            return new CertificateResponse
            {
                Success = true,
                Message = "Certificate verified",
                Certificate = MapToCertificateDto(certificate)
            };
        }

        private async Task<string> GenerateCertificatePdfAsync(Certificate certificate)
        {
            string studentName = "Learner";
            string courseTitle = "Course";

            // 1. Fetch Student Name from AuthService
            try
            {
                var authServiceUrl = _configuration["AuthService:Url"] ?? "http://localhost:5025";
                var response = await _httpClient.GetAsync($"{authServiceUrl}/api/auth/users/{certificate.StudentId}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>();
                    if (result != null) {
                        string firstName = result.user?.firstName ?? "";
                        string lastName = result.user?.lastName ?? "";
                        studentName = $"{firstName} {lastName}".Trim();
                        if (string.IsNullOrEmpty(studentName)) studentName = result.user?.username ?? "Learner";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get student name: {ex.Message}");
            }

            // 2. Fetch Course Title from CourseService
            try
            {
                var courseServiceUrl = _configuration["CourseService:Url"] ?? "http://localhost:5001";
                var response = await _httpClient.GetAsync($"{courseServiceUrl}/api/courses/{certificate.CourseId}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>();
                    if (result != null) {
                        courseTitle = result.course?.title ?? "Course";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get course title: {ex.Message}");
            }

            // 3. Generate PDF using QuestPDF
            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(1, Unit.Inch);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(16));

                        page.Content()
                            .Border(2)
                            .Padding(20)
                            .Column(column =>
                            {
                                column.Spacing(20);
                                column.Item().AlignCenter().Text("EduLearn").FontSize(48).FontColor(Colors.Blue.Medium).SemiBold();
                                column.Item().AlignCenter().Text("CERTIFICATE OF COMPLETION").FontSize(32).SemiBold();
                                column.Item().PaddingTop(30).AlignCenter().Text("This is to certify that");
                                column.Item().AlignCenter().Text(studentName).FontSize(36).Italic().FontColor(Colors.Black);
                                column.Item().AlignCenter().Text("has successfully completed the course");
                                column.Item().AlignCenter().Text(courseTitle).FontSize(28).SemiBold().FontColor(Colors.Blue.Darken2);
                                column.Item().PaddingTop(40).Row(row =>
                                {
                                    row.RelativeItem().Column(c => {
                                        c.Item().Text("EduLearn Academy").SemiBold();
                                        c.Item().Text("Authorized Signature").FontSize(12);
                                    });
                                    row.RelativeItem().AlignRight().Column(c => {
                                        c.Item().Text($"Issued: {certificate.IssuedAt:MMMM dd, yyyy}").SemiBold();
                                        c.Item().Text($"ID: {certificate.CertificateId}").FontSize(10);
                                    });
                                });
                                column.Item().PaddingTop(20).AlignCenter().Text($"Verification Code: {certificate.VerificationCode}").FontSize(10).FontColor(Colors.Grey.Medium);
                            });
                    });
                });

                string fileName = $"{certificate.CertificateId}.pdf";
                string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "certificates");
                if (!Directory.Exists(wwwrootPath)) Directory.CreateDirectory(wwwrootPath);

                string filePath = Path.Combine(wwwrootPath, fileName);
                document.GeneratePdf(filePath);

                return $"http://localhost:5004/certificates/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate PDF: {ex.Message}");
                return $"https://edulearn-certificates.blob.core.windows.net/certificates/{certificate.CertificateId}.pdf";
            }
        }

        private CertificateDto MapToCertificateDto(Certificate certificate)
        {
            return new CertificateDto
            {
                CertificateId = certificate.CertificateId,
                StudentId = certificate.StudentId,
                CourseId = certificate.CourseId,
                IssuedAt = certificate.IssuedAt,
                CertificateUrl = certificate.CertificateUrl,
                VerificationCode = certificate.VerificationCode
            };
        }
    }
}
