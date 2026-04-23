using EduLearn.CourseService.Data;
using EduLearn.CourseService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.CourseService.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly CourseDbContext _context;

        public CourseRepository(CourseDbContext context)
        {
            _context = context;
        }

        public async Task<Course?> FindByCourseIdAsync(Guid courseId)
        {
            return await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
        }

        public async Task<List<Course>> FindByInstructorIdAsync(Guid instructorId)
        {
            return await _context.Courses.Where(c => c.InstructorId == instructorId).ToListAsync();
        }

        public async Task<List<Course>> FindByCategoryAsync(string category)
        {
            return await _context.Courses.Where(c => c.Category == category).ToListAsync();
        }

        public async Task<List<Course>> FindPublishedCoursesAsync()
        {
            return await _context.Courses.Where(c => c.IsPublished && c.IsApproved).ToListAsync();
        }

        public async Task<List<Course>> SearchCoursesAsync(string keyword)
        {
            return await _context.Courses
                .Where(c => c.Title.Contains(keyword) || c.Description.Contains(keyword))
                .ToListAsync();
        }

        public async Task<List<Course>> FindTopRatedAsync(int limit)
        {
            // Note: This would typically JOIN with a Reviews table
            // For now, we'll return courses ordered by enrollment count as a proxy
            return await _context.Courses
                .Where(c => c.IsPublished && c.IsApproved)
                .OrderByDescending(c => c.EnrollmentCount)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> CountByInstructorIdAsync(Guid instructorId)
        {
            return await _context.Courses.CountAsync(c => c.InstructorId == instructorId);
        }

        public async Task<Course> AddAsync(Course course)
        {
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return course;
        }

        public async Task<Course> UpdateAsync(Course course)
        {
            course.UpdatedAt = DateTime.UtcNow;
            _context.Courses.Update(course);
            await _context.SaveChangesAsync();
            return course;
        }

        public async Task DeleteAsync(Guid courseId)
        {
            var course = await FindByCourseIdAsync(courseId);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
        }

        public async Task IncrementEnrollmentAsync(Guid courseId)
        {
            await _context.Courses
                .Where(c => c.CourseId == courseId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.EnrollmentCount, c => c.EnrollmentCount + 1));
        }
    }
}
