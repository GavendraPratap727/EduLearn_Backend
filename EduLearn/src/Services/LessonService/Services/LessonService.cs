using EduLearn.LessonService.Models;
using EduLearn.LessonService.Repositories;

namespace EduLearn.LessonService.Services
{
    public class LessonService : ILessonService
    {
        private readonly ILessonRepository _repository;

        public LessonService(ILessonRepository repository)
        {
            _repository = repository;
        }

        public async Task<LessonResponse> AddLessonAsync(CreateLessonRequest request)
        {
            var lesson = new Lesson
            {
                LessonId = Guid.NewGuid(),
                CourseId = request.CourseId,
                Title = request.Title,
                Description = request.Description,
                ContentType = request.ContentType,
                ContentUrl = request.ContentUrl,
                DurationMinutes = request.DurationMinutes,
                DisplayOrder = request.DisplayOrder,
                IsPreview = request.IsPreview,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            };

            var createdLesson = await _repository.AddAsync(lesson);
            return new LessonResponse
            {
                Success = true,
                Message = "Lesson created successfully",
                Lesson = MapToLessonDto(createdLesson)
            };
        }

        public async Task<LessonResponse> GetLessonByIdAsync(Guid lessonId)
        {
            var lesson = await _repository.FindByLessonIdAsync(lessonId);
            if (lesson == null)
            {
                return new LessonResponse
                {
                    Success = false,
                    Message = "Lesson not found"
                };
            }

            return new LessonResponse
            {
                Success = true,
                Message = "Lesson retrieved successfully",
                Lesson = MapToLessonDto(lesson)
            };
        }

        public async Task<LessonResponse> GetLessonsByCourseAsync(Guid courseId)
        {
            var lessons = await _repository.FindByCourseIdAsync(courseId);
            return new LessonResponse
            {
                Success = true,
                Message = "Lessons retrieved successfully",
                Lessons = lessons.Select(MapToLessonDto).ToList()
            };
        }

        public async Task<LessonResponse> GetOrderedLessonsAsync(Guid courseId)
        {
            var lessons = await _repository.FindByCourseIdOrderByDisplayOrderAsync(courseId);
            return new LessonResponse
            {
                Success = true,
                Message = "Ordered lessons retrieved successfully",
                Lessons = lessons.Select(MapToLessonDto).ToList()
            };
        }

        public async Task<LessonResponse> GetPreviewLessonsAsync(Guid courseId)
        {
            var lessons = await _repository.FindPreviewLessonsAsync(courseId);
            return new LessonResponse
            {
                Success = true,
                Message = "Preview lessons retrieved successfully",
                Lessons = lessons.Select(MapToLessonDto).ToList()
            };
        }

        public async Task<LessonResponse> UpdateLessonAsync(Guid lessonId, UpdateLessonRequest request)
        {
            var lesson = await _repository.FindByLessonIdAsync(lessonId);
            if (lesson == null)
            {
                return new LessonResponse
                {
                    Success = false,
                    Message = "Lesson not found"
                };
            }

            lesson.Title = request.Title;
            lesson.Description = request.Description;
            lesson.ContentType = request.ContentType;
            lesson.ContentUrl = request.ContentUrl;
            lesson.DurationMinutes = request.DurationMinutes;
            lesson.IsPreview = request.IsPreview;

            var updatedLesson = await _repository.UpdateAsync(lesson);
            return new LessonResponse
            {
                Success = true,
                Message = "Lesson updated successfully",
                Lesson = MapToLessonDto(updatedLesson)
            };
        }

        public async Task<LessonResponse> ReorderLessonsAsync(Guid courseId, List<Guid> lessonIds)
        {
            var lessonOrders = lessonIds.Select((id, index) => (id, index + 1)).ToList();
            await _repository.ReorderLessonsAsync(lessonOrders);
            return new LessonResponse
            {
                Success = true,
                Message = "Lessons reordered successfully"
            };
        }

        public async Task<LessonResponse> PublishLessonAsync(Guid lessonId)
        {
            var lesson = await _repository.FindByLessonIdAsync(lessonId);
            if (lesson == null)
            {
                return new LessonResponse
                {
                    Success = false,
                    Message = "Lesson not found"
                };
            }

            lesson.IsPublished = true;
            var updatedLesson = await _repository.UpdateAsync(lesson);
            return new LessonResponse
            {
                Success = true,
                Message = "Lesson published successfully",
                Lesson = MapToLessonDto(updatedLesson)
            };
        }

        public async Task<LessonResponse> DeleteLessonAsync(Guid lessonId)
        {
            await _repository.DeleteAsync(lessonId);
            return new LessonResponse
            {
                Success = true,
                Message = "Lesson deleted successfully"
            };
        }

        public async Task<LessonResponse> DeleteAllForCourseAsync(Guid courseId)
        {
            await _repository.DeleteByCourseIdAsync(courseId);
            return new LessonResponse
            {
                Success = true,
                Message = "All lessons for course deleted successfully"
            };
        }

        public async Task<LessonResponse> GetLessonCountAsync(Guid courseId)
        {
            var count = await _repository.CountByCourseIdAsync(courseId);
            return new LessonResponse
            {
                Success = true,
                Message = "Lesson count retrieved successfully",
                Count = count
            };
        }

        private LessonDto MapToLessonDto(Lesson lesson)
        {
            return new LessonDto
            {
                LessonId = lesson.LessonId,
                CourseId = lesson.CourseId,
                Title = lesson.Title,
                Description = lesson.Description,
                ContentType = lesson.ContentType,
                ContentUrl = lesson.ContentUrl,
                DurationMinutes = lesson.DurationMinutes,
                DisplayOrder = lesson.DisplayOrder,
                IsPreview = lesson.IsPreview,
                IsPublished = lesson.IsPublished,
                CreatedAt = lesson.CreatedAt
            };
        }
    }
}
