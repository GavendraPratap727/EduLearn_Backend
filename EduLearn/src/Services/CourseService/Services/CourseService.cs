using EduLearn.CourseService.Models;
using EduLearn.CourseService.Repositories;

namespace EduLearn.CourseService.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _repository;

        public CourseService(ICourseRepository repository)
        {
            _repository = repository;
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
                Level = request.Level,
                Language = request.Language,
                Price = request.Price,
                ThumbnailUrl = request.ThumbnailUrl,
                IsPublished = false,
                IsApproved = false,
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
                return new CourseResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            return new CourseResponse
            {
                Success = true,
                Message = "Course retrieved successfully",
                Course = MapToCourseDto(course)
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
                return new CourseResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            course.IsPublished = true;
            var updatedCourse = await _repository.UpdateAsync(course);
            return new CourseResponse
            {
                Success = true,
                Message = "Course published successfully",
                Course = MapToCourseDto(updatedCourse)
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
            var updatedCourse = await _repository.UpdateAsync(course);
            return new CourseResponse
            {
                Success = true,
                Message = "Course approved successfully",
                Course = MapToCourseDto(updatedCourse)
            };
        }

        public async Task<CourseResponse> DeleteCourseAsync(Guid courseId)
        {
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
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                TotalDuration = course.TotalDuration,
                EnrollmentCount = course.EnrollmentCount
            };
        }
    }
}
