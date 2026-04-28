using EduLearn.ProgressService.Models;
using EduLearn.ProgressService.Repositories;
using System.Net.Http.Json;

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
            
            // Get total lessons from LessonService
            int totalLessons = 0;
            try
            {
                var lessonServiceUrl = _configuration["LessonService:Url"] ?? "http://localhost:5002";
                var response = await _httpClient.GetAsync($"{lessonServiceUrl}/api/lessons/course/{courseId}/count");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>();
                    totalLessons = result?.count ?? 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get lesson count: {ex.Message}");
            }

            var completedLessons = progressList.Count(p => p.IsCompleted);
            var progressPercent = totalLessons > 0 ? (completedLessons * 100) / totalLessons : 0;

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
    }
}
