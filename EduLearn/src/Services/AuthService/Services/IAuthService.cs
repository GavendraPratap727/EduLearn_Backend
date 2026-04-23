using EduLearn.AuthService.Data;
using EduLearn.AuthService.Models;

namespace EduLearn.AuthService.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> LogoutAsync(Guid userId);
        Task<AuthResponse> ValidateTokenAsync(string token);
        Task<UserDto?> GetUserByIdAsync(Guid userId);
        Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
        Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<List<UserDto>> GetAllByRoleAsync(UserRoleType role);
        Task<AuthResponse> DeactivateAccountAsync(Guid userId);
        Task<AuthResponse> ReactivateAccountAsync(Guid userId);
        Task<List<UserDto>> SearchUsersAsync(string keyword);
    }
}
