using EduLearn.EnrollmentService.Data;
using EduLearn.EnrollmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.EnrollmentService.Repositories
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly EnrollmentDbContext _context;

        public EnrollmentRepository(EnrollmentDbContext context)
        {
            _context = context;
        }

        public async Task<Enrollment?> FindByEnrollmentIdAsync(Guid enrollmentId)
        {
            return await _context.Enrollments.FindAsync(enrollmentId);
        }

        public async Task<List<Enrollment>> FindByStudentIdAsync(Guid studentId)
        {
            return await _context.Enrollments.Where(e => e.StudentId == studentId).ToListAsync();
        }

        public async Task<List<Enrollment>> FindByCourseIdAsync(Guid courseId)
        {
            return await _context.Enrollments.Where(e => e.CourseId == courseId).ToListAsync();
        }

        public async Task<Enrollment?> FindByStudentAndCourseAsync(Guid studentId, Guid courseId)
        {
            return await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        }

        public async Task<bool> IsEnrolledAsync(Guid studentId, Guid courseId)
        {
            return await _context.Enrollments
                .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.Status == EnrollmentStatus.ACTIVE);
        }

        public async Task<List<Enrollment>> FindCompletedAsync(Guid studentId)
        {
            return await _context.Enrollments
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.COMPLETED)
                .ToListAsync();
        }

        public async Task<List<Enrollment>> FindInProgressAsync(Guid studentId)
        {
            return await _context.Enrollments
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.ACTIVE && e.ProgressPercent > 0 && e.ProgressPercent < 100)
                .ToListAsync();
        }

        public async Task<int> CountByCourseIdAsync(Guid courseId)
        {
            return await _context.Enrollments
                .CountAsync(e => e.CourseId == courseId && e.Status == EnrollmentStatus.ACTIVE);
        }

        public async Task<Enrollment> AddAsync(Enrollment enrollment)
        {
            await _context.Enrollments.AddAsync(enrollment);
            await _context.SaveChangesAsync();
            return enrollment;
        }

        public async Task<Enrollment> UpdateAsync(Enrollment enrollment)
        {
            _context.Enrollments.Update(enrollment);
            await _context.SaveChangesAsync();
            return enrollment;
        }

        public async Task DeleteAsync(Guid enrollmentId)
        {
            var enrollment = await FindByEnrollmentIdAsync(enrollmentId);
            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteByStudentAndCourseAsync(Guid studentId, Guid courseId)
        {
            var enrollment = await FindByStudentAndCourseAsync(studentId, courseId);
            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
            }
        }
    }
}
