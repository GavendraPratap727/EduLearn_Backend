using EduLearn.AuthService.Data;

namespace EduLearn.AuthService.Repositories
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email);
        Task<User?> FindByUserIdAsync(Guid userId);
        Task<bool> ExistsByEmailAsync(string email);
        Task<List<User>> FindAllByRoleAsync(UserRoleType role);
        Task<List<User>> FindAllActiveAsync();
        Task<List<User>> SearchUsersAsync(string keyword);
        Task UpdateLastLoginAsync(Guid userId);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
    }
}
