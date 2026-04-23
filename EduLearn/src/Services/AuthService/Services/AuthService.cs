using BCrypt.Net;
using EduLearn.AuthService.Data;
using EduLearn.AuthService.Models;
using EduLearn.AuthService.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EduLearn.AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if user already exists
            if (await _userRepository.ExistsByEmailAsync(request.Email))
            {
                return new AuthResponse(false, "User with this email already exists", null, null);
            }

            // Create new user
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user = await _userRepository.AddAsync(user);

            var token = GenerateJwtToken(user);

            return new AuthResponse(true, "Registration successful", token, MapToUserDto(user));
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return new AuthResponse(false, "Invalid email or password", null, null);
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResponse(false, "Invalid email or password", null, null);
            }

            if (!user.IsActive)
            {
                return new AuthResponse(false, "Account is inactive", null, null);
            }

            await _userRepository.UpdateLastLoginAsync(user.Id);

            var token = GenerateJwtToken(user);

            return new AuthResponse(true, "Login successful", token, MapToUserDto(user));
        }

        public async Task<AuthResponse> LogoutAsync(Guid userId)
        {
            // In a real implementation, you might want to invalidate the token
            // For now, this is a placeholder
            return new AuthResponse(true, "Logout successful", null, null);
        }

        public async Task<AuthResponse> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");
                var key = Encoding.UTF8.GetBytes(secretKey);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true
                }, out _);

                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out Guid userId))
                {
                    var user = await _userRepository.FindByUserIdAsync(userId);
                    if (user != null && user.IsActive)
                    {
                        return new AuthResponse(true, "Token is valid", null, MapToUserDto(user));
                    }
                    else
                    {
                        return new AuthResponse(false, "User not found or inactive", null, null);
                    }
                }

                return new AuthResponse(false, "Invalid user ID in token", null, null);
            }
            catch (Exception ex)
            {
                return new AuthResponse(false, $"Token validation failed: {ex.Message}", null, null);
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            return user != null ? MapToUserDto(user) : null;
        }

        public async Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null)
            {
                return new AuthResponse(false, "User not found", null, null);
            }

            if (!string.IsNullOrEmpty(request.FullName))
            {
                user.FullName = request.FullName;
            }

            if (!string.IsNullOrEmpty(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl;
            }

            await _userRepository.UpdateAsync(user);

            return new AuthResponse(true, "Profile updated successfully", null, MapToUserDto(user));
        }

        public async Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null)
            {
                return new AuthResponse(false, "User not found", null, null);
            }

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            {
                return new AuthResponse(false, "Old password is incorrect", null, null);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);

            return new AuthResponse(true, "Password changed successfully", null, null);
        }

        public async Task<List<UserDto>> GetAllByRoleAsync(UserRoleType role)
        {
            var users = await _userRepository.FindAllByRoleAsync(role);
            return users.Select(MapToUserDto).ToList();
        }

        public async Task<AuthResponse> DeactivateAccountAsync(Guid userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null)
            {
                return new AuthResponse(false, "User not found", null, null);
            }

            user.IsActive = false;
            await _userRepository.UpdateAsync(user);

            return new AuthResponse(true, "Account deactivated successfully", null, null);
        }

        public async Task<AuthResponse> ReactivateAccountAsync(Guid userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null)
            {
                return new AuthResponse(false, "User not found", null, null);
            }

            user.IsActive = true;
            await _userRepository.UpdateAsync(user);

            return new AuthResponse(true, "Account reactivated successfully", null, null);
        }

        public async Task<List<UserDto>> SearchUsersAsync(string keyword)
        {
            var users = await _userRepository.SearchUsersAsync(keyword);
            return users.Select(MapToUserDto).ToList();
        }

        private string GenerateJwtToken(User user)
        {
            var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");
            var key = Encoding.UTF8.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto(
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.AvatarUrl,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt
            );
        }
    }
}
