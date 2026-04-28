using EduLearn.ProgressService.Models;

namespace EduLearn.ProgressService.Repositories
{
    public interface IProgressRepository
    {
        Task<LessonProgress?> FindByProgressIdAsync(Guid progressId);
        Task<LessonProgress?> FindByStudentAndLessonAsync(Guid studentId, Guid lessonId);
        Task<List<LessonProgress>> FindByStudentAsync(Guid studentId);
        Task<List<LessonProgress>> FindByCourseAsync(Guid courseId);
        Task<List<LessonProgress>> FindByStudentAndCourseAsync(Guid studentId, Guid courseId);
        Task<int> CountCompletedLessonsAsync(Guid studentId, Guid courseId);
        Task<int> CountTotalLessonsAsync(Guid courseId);
        Task<LessonProgress> AddAsync(LessonProgress progress);
        Task<LessonProgress> UpdateAsync(LessonProgress progress);
        Task DeleteAsync(Guid progressId);

        // Certificate methods
        Task<Certificate?> FindCertificateByIdAsync(string certificateId);
        Task<Certificate?> FindCertificateByVerificationCodeAsync(string verificationCode);
        Task<List<Certificate>> FindCertificatesByStudentAsync(Guid studentId);
        Task<Certificate?> FindCertificateByStudentAndCourseAsync(Guid studentId, Guid courseId);
        Task<Certificate> AddCertificateAsync(Certificate certificate);
        Task<Certificate> UpdateCertificateAsync(Certificate certificate);
    }
}
