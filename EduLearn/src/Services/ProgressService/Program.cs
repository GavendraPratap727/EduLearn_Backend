using EduLearn.ProgressService.Data;
using EduLearn.ProgressService.Models;
using EduLearn.ProgressService.Repositories;
using EduLearn.ProgressService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddDbContext<ProgressDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository
builder.Services.AddScoped<IProgressRepository, ProgressRepository>();

// Add HttpClient
builder.Services.AddHttpClient<IProgressService, ProgressService>();

// Add Service
builder.Services.AddScoped<IProgressService, ProgressService>();

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

// Progress endpoints
app.MapPost("/api/progress", async (CreateLessonProgressRequest request, IProgressService progressService) =>
{
    var result = await progressService.CreateProgressAsync(request);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("CreateProgress")
.WithOpenApi();

app.MapGet("/api/progress/{id}", async (Guid id, IProgressService progressService) =>
{
    var result = await progressService.GetProgressByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetProgressById")
.WithOpenApi();

app.MapGet("/api/progress/student/{studentId}/lesson/{lessonId}", async (Guid studentId, Guid lessonId, HttpContext context, IProgressService progressService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Students can only view their own progress, Admins can view any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != studentId && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await progressService.GetProgressByStudentAndLessonAsync(studentId, lessonId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetProgressByStudentAndLesson")
.WithOpenApi();

app.MapGet("/api/progress/student/{id}", async (Guid id, HttpContext context, IProgressService progressService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Students can only view their own progress, Admins can view any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != id && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await progressService.GetProgressByStudentAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetProgressByStudent")
.WithOpenApi();

app.MapGet("/api/progress/course/{id}", async (Guid id, IProgressService progressService) =>
{
    var result = await progressService.GetProgressByCourseAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("GetProgressByCourse")
.WithOpenApi();

app.MapGet("/api/progress/course/{courseId}/student/{studentId}", async (Guid courseId, Guid studentId, HttpContext context, IProgressService progressService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Students can only view their own progress, Admins can view any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != studentId && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await progressService.GetCourseProgressAsync(studentId, courseId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetCourseProgress")
.WithOpenApi();

app.MapPut("/api/progress/{id}", async (Guid id, UpdateLessonProgressRequest request, IProgressService progressService) =>
{
    var result = await progressService.UpdateProgressAsync(id, request);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("UpdateProgress")
.WithOpenApi();

app.MapPut("/api/progress/{id}/complete", async (Guid id, IProgressService progressService) =>
{
    var result = await progressService.MarkLessonCompleteAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("MarkLessonComplete")
.WithOpenApi();

app.MapGet("/api/progress/stats/{id}", async (Guid id, HttpContext context, IProgressService progressService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Students can only view their own stats, Admins can view any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != id && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await progressService.GetOverallStatsAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetOverallStats")
.WithOpenApi();

app.Run();
