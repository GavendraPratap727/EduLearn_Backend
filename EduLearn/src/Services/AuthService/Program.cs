using Microsoft.EntityFrameworkCore;
using EduLearn.AuthService.Data;
using EduLearn.AuthService.Services;
using EduLearn.AuthService.Repositories;
using EduLearn.AuthService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<EduLearn.AuthService.Data.AuthDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Add AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Authentication endpoints
app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService) =>
{
    var result = await authService.RegisterAsync(request);
    return Results.Ok(result);
})
.WithName("Register")
.WithOpenApi();

app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService) =>
{
    var result = await authService.LoginAsync(request);
    if (!result.Success)
    {
        return Results.Unauthorized();
    }
    return Results.Ok(result);
})
.WithName("Login")
.WithOpenApi();

// User management endpoints
app.MapPost("/api/users/logout", async (Guid userId, IAuthService authService) =>
{
    var result = await authService.LogoutAsync(userId);
    return Results.Ok(result);
})
.WithName("Logout")
.WithOpenApi();

app.MapGet("/api/users/{userId}", async (Guid userId, IAuthService authService) =>
{
    var result = await authService.GetUserByIdAsync(userId);
    return result != null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetUserById")
.WithOpenApi();

app.MapPut("/api/users/{userId}/profile", async (Guid userId, UpdateProfileRequest request, IAuthService authService) =>
{
    var result = await authService.UpdateProfileAsync(userId, request);
    return Results.Ok(result);
})
.WithName("UpdateProfile")
.WithOpenApi();

app.MapPut("/api/users/{userId}/password", async (Guid userId, ChangePasswordRequest request, IAuthService authService) =>
{
    var result = await authService.ChangePasswordAsync(userId, request);
    return Results.Ok(result);
})
.WithName("ChangePassword")
.WithOpenApi();

app.MapGet("/api/users/role/{role}", async (UserRoleType role, IAuthService authService) =>
{
    var result = await authService.GetAllByRoleAsync(role);
    return Results.Ok(result);
})
.WithName("GetAllByRole")
.WithOpenApi();

app.MapPut("/api/users/{userId}/deactivate", async (Guid userId, IAuthService authService) =>
{
    var result = await authService.DeactivateAccountAsync(userId);
    return Results.Ok(result);
})
.WithName("DeactivateAccount")
.WithOpenApi();

app.MapPut("/api/users/{userId}/reactivate", async (Guid userId, IAuthService authService) =>
{
    var result = await authService.ReactivateAccountAsync(userId);
    return Results.Ok(result);
})
.WithName("ReactivateAccount")
.WithOpenApi();

app.MapGet("/api/users/search/{keyword}", async (string keyword, IAuthService authService) =>
{
    var result = await authService.SearchUsersAsync(keyword);
    return Results.Ok(result);
})
.WithName("SearchUsers")
.WithOpenApi();

app.MapPost("/api/auth/validate", async (string token, IAuthService authService) =>
{
    var result = await authService.ValidateTokenAsync(token);
    return Results.Ok(result);
})
.WithName("ValidateToken")
.WithOpenApi();

app.Run();
