using EduLearn.AuthService.Data;

namespace EduLearn.AuthService.Models
{
    public record RegisterRequest(
        string FullName,
        string Email,
        string Password,
        UserRoleType Role = UserRoleType.STUDENT
    );

    public record LoginRequest(
        string Email,
        string Password
    );

    public record UpdateProfileRequest(
        string? FullName,
        string? AvatarUrl
    );

    public record ChangePasswordRequest(
        string OldPassword,
        string NewPassword
    );

    public record AuthResponse(
        bool Success,
        string? Message,
        string? Token,
        UserDto? User
    );

    public record UserDto(
        Guid Id,
        string FullName,
        string Email,
        UserRoleType Role,
        string? AvatarUrl,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? LastLoginAt
    );
}
