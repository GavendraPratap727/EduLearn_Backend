using Microsoft.EntityFrameworkCore;
using EduLearn.AuthService.Data;
using EduLearn.AuthService.Services;
using EduLearn.AuthService.Repositories;
using EduLearn.AuthService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured"))),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("InstructorOrAdmin", policy => policy.RequireRole("INSTRUCTOR", "ADMIN"));
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
});

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
app.UseAuthentication();
app.UseAuthorization();

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
.RequireAuthorization("Authenticated")
.WithName("Logout")
.WithOpenApi();

app.MapGet("/api/users/{userId}", async (Guid userId, HttpContext context, IAuthService authService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Users can view their own profile, Admins can view any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != userId && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await authService.GetUserByIdAsync(userId);
    return result != null ? Results.Ok(result) : Results.NotFound();
})
.RequireAuthorization("Authenticated")
.WithName("GetUserById")
.WithOpenApi();

app.MapPut("/api/users/{userId}/profile", async (Guid userId, UpdateProfileRequest request, HttpContext context, IAuthService authService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Users can update their own profile, Admins can update any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != userId && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await authService.UpdateProfileAsync(userId, request);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("UpdateProfile")
.WithOpenApi();

app.MapPut("/api/users/{userId}/password", async (Guid userId, ChangePasswordRequest request, HttpContext context, IAuthService authService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Users can only change their own password
        if (currentUserId != userId)
        {
            return Results.Forbid();
        }
    }
    var result = await authService.ChangePasswordAsync(userId, request);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("ChangePassword")
.WithOpenApi();

app.MapGet("/api/users/role/{role}", async (UserRoleType role, IAuthService authService) =>
{
    var result = await authService.GetAllByRoleAsync(role);
    return Results.Ok(result);
})
.RequireAuthorization("AdminOnly")
.WithName("GetAllByRole")
.WithOpenApi();

app.MapPut("/api/users/{userId}/deactivate", async (Guid userId, IAuthService authService) =>
{
    var result = await authService.DeactivateAccountAsync(userId);
    return Results.Ok(result);
})
.RequireAuthorization("AdminOnly")
.WithName("DeactivateAccount")
.WithOpenApi();

app.MapPut("/api/users/{userId}/reactivate", async (Guid userId, IAuthService authService) =>
{
    var result = await authService.ReactivateAccountAsync(userId);
    return Results.Ok(result);
})
.RequireAuthorization("AdminOnly")
.WithName("ReactivateAccount")
.WithOpenApi();

app.MapGet("/api/users/search/{keyword}", async (string keyword, IAuthService authService) =>
{
    var result = await authService.SearchUsersAsync(keyword);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
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
