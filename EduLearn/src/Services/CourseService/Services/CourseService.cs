using EduLearn.CourseService.Models;
using EduLearn.CourseService.Repositories;

namespace EduLearn.CourseService.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CourseService(ICourseRepository repository, IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request)
        {
            var course = new Course
            {
                CourseId = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                InstructorId = request.InstructorId,
                Category = request.Category,
                Level = Enum.TryParse<CourseLevel>(request.Level.ToString(), out var parsedLevel) ? parsedLevel : CourseLevel.Beginner,
                Language = request.Language,
                Price = request.Price,
                ThumbnailUrl = request.ThumbnailUrl,
                IsPublished = false,
                IsApproved = false,
                IsSubmittedForReview = false,
                IsFinished = false,
                CreatedAt = DateTime.UtcNow
            };

            var createdCourse = await _repository.AddAsync(course);
            return new CourseResponse
            {
                Success = true,
                Message = "Course created successfully",
                Course = MapToCourseDto(createdCourse)
            };
        }

        public async Task<CourseResponse> GetCourseByIdAsync(Guid courseId)
        {
            var course = await _repository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return new CourseResponse { Success = false, Message = "Course not found" };
            }

            // Check if user is authorized to view this course
            var currentUserIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            bool isAuthorized = false;
            if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
            {
                isAuthorized = (course.InstructorId == currentUserId || userRole == "ADMIN");
            }

            // If not authorized, check if course is live
            if (!isAuthorized && (!course.IsPublished || !course.IsApproved))
            {
                return new CourseResponse { Success = false, Message = "Course is not yet available for public viewing" };
            }

            return new CourseResponse
            {
                Success = true,
                Message = "Course retrieved successfully",
                Course = MapToCourseDto(course)
            };
        }
        public async Task<CourseResponse> GetAllCoursesAsync()
        {
            var allCourses = await _repository.GetAllAsync();
            var courses = allCourses.Where(c => c.IsPublished && c.IsApproved).ToList();
            return new CourseResponse
            {
                Success = true,
                Message = "All courses retrieved successfully",
                Courses = courses.Select(MapToCourseDto).ToList()
            };
        }

        public async Task<CourseResponse> GetCoursesByInstructorAsync(Guid instructorId)
        {
            var courses = await _repository.FindByInstructorIdAsync(instructorId);
            return new CourseResponse
            {
                Success = true,
                Message = "Courses retrieved successfully",
                Courses = courses.Select(MapToCourseDto).ToList()
            };
        }

        public async Task<CourseResponse> GetCoursesByCategoryAsync(string category)
        {
            var courses = await _repository.FindByCategoryAsync(category);
            return new CourseResponse
            {
                Success = true,
                Message = "Courses retrieved successfully",
                Courses = courses.Select(MapToCourseDto).ToList()
            };
        }

        public async Task<CourseResponse> GetPublishedCoursesAsync()
        {
            var courses = await _repository.FindPublishedCoursesAsync();
            return new CourseResponse
            {
                Success = true,
                Message = "Published courses retrieved successfully",
                Courses = courses.Select(MapToCourseDto).ToList()
            };
        }

        public async Task<CourseResponse> SearchCoursesAsync(string keyword)
        {
            var courses = await _repository.SearchCoursesAsync(keyword);
            return new CourseResponse
            {
                Success = true,
                Message = "Courses retrieved successfully",
                Courses = courses.Select(MapToCourseDto).ToList()
            };
        }

        public async Task<CourseResponse> UpdateCourseAsync(Guid courseId, UpdateCourseRequest request)
        {
            var course = await _repository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return new CourseResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            course.Title = request.Title;
            course.Description = request.Description;
            course.Category = request.Category;
            course.Level = request.Level;
            course.Language = request.Language;
            course.Price = request.Price;
            course.ThumbnailUrl = request.ThumbnailUrl;
            course.TotalDuration = request.TotalDuration;

            // If an instructor updates a course, it should go back to "In Review" or "Draft"
            // unless it's already approved (optional choice, but let's be strict)
            var userRole = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "ADMIN")
            {
                course.IsPublished = false;
                course.IsApproved = false;
                // We keep IsSubmittedForReview as is, or set to false to force re-submission
                course.IsSubmittedForReview = false; 
            }

            var updatedCourse = await _repository.UpdateAsync(course);
            return new CourseResponse
            {
                Success = true,
                Message = "Course updated successfully",
                Course = MapToCourseDto(updatedCourse)
            };
        }

        public async Task<CourseResponse> PublishCourseAsync(Guid courseId)
        {
            var course = await _repository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return new CourseResponse { Success = false, Message = "Course not found" };
            }

            course.IsSubmittedForReview = true;
            course.IsPublished = false;
            await _repository.UpdateAsync(course);
            return new CourseResponse
            {
                Success = true,
                Message = "Course submitted for admin approval",
                Course = MapToCourseDto(course)
            };
        }

        public async Task<CourseResponse> ApproveCourseAsync(Guid courseId)
        {
            var course = await _repository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return new CourseResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            course.IsApproved = true;
            course.IsPublished = true;
            var updatedCourse = await _repository.UpdateAsync(course);
            return new CourseResponse
            {
                Success = true,
                Message = "Course approved successfully",
                Course = MapToCourseDto(updatedCourse)
            };
        }

        public async Task<CourseResponse> RejectCourseAsync(Guid courseId)
        {
            var course = await _repository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return new CourseResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            course.IsApproved = false;
            course.IsPublished = false;
            course.IsSubmittedForReview = false;
            var updatedCourse = await _repository.UpdateAsync(course);
            return new CourseResponse
            {
                Success = true,
                Message = "Course rejected successfully",
                Course = MapToCourseDto(updatedCourse)
            };
        }

        public async Task<CourseResponse> FinishCourseAsync(Guid courseId)
        {
            var course = await _repository.FindByCourseIdAsync(courseId);
            if (course == null)
            {
                return new CourseResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            course.IsFinished = true;
            var updatedCourse = await _repository.UpdateAsync(course);
            return new CourseResponse
            {
                Success = true,
                Message = "Course finished successfully. Students can now earn certificates.",
                Course = MapToCourseDto(updatedCourse)
            };
        }

        public async Task<CourseResponse> DeleteCourseAsync(Guid courseId)
        {
            // Clean up related enrollments before deleting course
            try
            {
                var enrollmentServiceUrl = _httpContextAccessor.HttpContext?.RequestServices?.GetService<IConfiguration>()?["EnrollmentService:Url"] ?? "http://localhost:5003";
                using var httpClient = new HttpClient();
                var cleanupResponse = await httpClient.DeleteAsync($"{enrollmentServiceUrl}/api/enrollments/course/{courseId}");
                // Continue with course deletion even if enrollment cleanup fails
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clean up enrollments for course {courseId}: {ex.Message}");
            }

            await _repository.DeleteAsync(courseId);
            return new CourseResponse
            {
                Success = true,
                Message = "Course deleted successfully"
            };
        }

        public async Task<CourseResponse> GetTopRatedCoursesAsync(int limit)
        {
            var courses = await _repository.FindTopRatedAsync(limit);
            return new CourseResponse
            {
                Success = true,
                Message = "Top-rated courses retrieved successfully",
                Courses = courses.Select(MapToCourseDto).ToList()
            };
        }

        public async Task<CourseResponse> GetPendingCoursesAsync()
        {
            var courses = await _repository.FindPendingCoursesAsync();
            return new CourseResponse
            {
                Success = true,
                Message = "Pending courses retrieved successfully",
                Courses = courses.Select(MapToCourseDto).ToList()
            };
        }


        public async Task IncrementEnrollmentAsync(Guid courseId)
        {
            await _repository.IncrementEnrollmentAsync(courseId);
        }

        private CourseDto MapToCourseDto(Course course)
        {
            return new CourseDto
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                InstructorId = course.InstructorId,
                Category = course.Category,
                Level = course.Level,
                Language = course.Language,
                Price = course.Price,
                ThumbnailUrl = course.ThumbnailUrl,
                IsPublished = course.IsPublished,
                IsApproved = course.IsApproved,
                IsSubmittedForReview = course.IsSubmittedForReview,
                IsFinished = course.IsFinished,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                TotalDuration = course.TotalDuration,
                EnrollmentCount = course.EnrollmentCount
            };
        }
    }
}
