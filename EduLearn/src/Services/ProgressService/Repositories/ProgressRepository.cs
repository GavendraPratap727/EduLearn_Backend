using EduLearn.ProgressService.Data;
using EduLearn.ProgressService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.ProgressService.Repositories
{
    public class ProgressRepository : IProgressRepository
    {
        private readonly ProgressDbContext _context;

        public ProgressRepository(ProgressDbContext context)
        {
            _context = context;
        }

        public async Task<LessonProgress?> FindByProgressIdAsync(Guid progressId)
        {
            return await _context.LessonProgress.FindAsync(progressId);
        }

        public async Task<LessonProgress?> FindByStudentAndLessonAsync(Guid studentId, Guid lessonId)
        {
            return await _context.LessonProgress
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LessonId == lessonId);
        }

        public async Task<List<LessonProgress>> FindByStudentAsync(Guid studentId)
        {
            return await _context.LessonProgress.Where(p => p.StudentId == studentId).ToListAsync();
        }

        public async Task<List<LessonProgress>> FindByCourseAsync(Guid courseId)
        {
            return await _context.LessonProgress.Where(p => p.CourseId == courseId).ToListAsync();
        }

        public async Task<List<LessonProgress>> FindByStudentAndCourseAsync(Guid studentId, Guid courseId)
        {
            return await _context.LessonProgress
                .Where(p => p.StudentId == studentId && p.CourseId == courseId)
                .ToListAsync();
        }

        public async Task<int> CountCompletedLessonsAsync(Guid studentId, Guid courseId)
        {
            return await _context.LessonProgress
                .CountAsync(p => p.StudentId == studentId && p.CourseId == courseId && p.IsCompleted);
        }

        public async Task<int> CountTotalLessonsAsync(Guid courseId)
        {
            // This would typically call LessonService, but for now we'll count progress records
            // In a real implementation, this should call LessonService to get total lessons
            return await _context.LessonProgress
                .Where(p => p.CourseId == courseId)
                .Select(p => p.LessonId)
                .Distinct()
                .CountAsync();
        }

        public async Task<LessonProgress> AddAsync(LessonProgress progress)
        {
            await _context.LessonProgress.AddAsync(progress);
            await _context.SaveChangesAsync();
            return progress;
        }

        public async Task<LessonProgress> UpdateAsync(LessonProgress progress)
        {
            _context.LessonProgress.Update(progress);
            await _context.SaveChangesAsync();
            return progress;
        }

        public async Task DeleteAsync(Guid progressId)
        {
            var progress = await FindByProgressIdAsync(progressId);
            if (progress != null)
            {
                _context.LessonProgress.Remove(progress);
                await _context.SaveChangesAsync();
            }
        }

        // Certificate methods
        public async Task<Certificate?> FindCertificateByIdAsync(string certificateId)
        {
            return await _context.Certificates.FindAsync(certificateId);
        }

        public async Task<Certificate?> FindCertificateByVerificationCodeAsync(string verificationCode)
        {
            return await _context.Certificates
                .FirstOrDefaultAsync(c => c.VerificationCode == verificationCode);
        }

        public async Task<List<Certificate>> FindCertificatesByStudentAsync(Guid studentId)
        {
            return await _context.Certificates
                .Where(c => c.StudentId == studentId)
                .ToListAsync();
        }

        public async Task<Certificate?> FindCertificateByStudentAndCourseAsync(Guid studentId, Guid courseId)
        {
            return await _context.Certificates
                .FirstOrDefaultAsync(c => c.StudentId == studentId && c.CourseId == courseId);
        }

        public async Task<Certificate> AddCertificateAsync(Certificate certificate)
        {
            await _context.Certificates.AddAsync(certificate);
            await _context.SaveChangesAsync();
            return certificate;
        }

        public async Task<Certificate> UpdateCertificateAsync(Certificate certificate)
        {
            _context.Certificates.Update(certificate);
            await _context.SaveChangesAsync();
            return certificate;
        }
    }
}
