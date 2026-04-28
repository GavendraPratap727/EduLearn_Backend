using EduLearn.QuizService.Data;
using EduLearn.QuizService.Models;
using EduLearn.QuizService.Repositories;
using EduLearn.QuizService.Services;
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
builder.Services.AddDbContext<QuizDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository
builder.Services.AddScoped<IQuizRepository, QuizRepository>();

// Add Service
builder.Services.AddScoped<IQuizService, QuizService>();

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

// Quiz endpoints
app.MapPost("/api/quizzes", async (CreateQuizRequest request, IQuizService quizService) =>
{
    var result = await quizService.CreateQuizAsync(request);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("CreateQuiz")
.WithOpenApi();

app.MapGet("/api/quizzes/{id}", async (Guid id, IQuizService quizService) =>
{
    var result = await quizService.GetQuizByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetQuizById")
.WithOpenApi();

app.MapGet("/api/quizzes/course/{courseId}", async (Guid courseId, IQuizService quizService) =>
{
    var result = await quizService.GetQuizzesByCourseAsync(courseId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetQuizzesByCourse")
.WithOpenApi();

app.MapGet("/api/quizzes/lesson/{lessonId}", async (Guid lessonId, IQuizService quizService) =>
{
    var result = await quizService.GetQuizByLessonAsync(lessonId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetQuizByLesson")
.WithOpenApi();

app.MapPut("/api/quizzes/{id}", async (Guid id, UpdateQuizRequest request, IQuizService quizService) =>
{
    var result = await quizService.UpdateQuizAsync(id, request);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("UpdateQuiz")
.WithOpenApi();

app.MapDelete("/api/quizzes/{id}", async (Guid id, IQuizService quizService) =>
{
    var result = await quizService.DeleteQuizAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("DeleteQuiz")
.WithOpenApi();

app.MapPut("/api/quizzes/{id}/publish", async (Guid id, IQuizService quizService) =>
{
    var result = await quizService.PublishQuizAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("PublishQuiz")
.WithOpenApi();

app.MapPost("/api/quizzes/attempt/start", async (HttpContext context, StartAttemptRequest request, IQuizService quizService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    var result = await quizService.StartAttemptAsync(studentId, request);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("StartAttempt")
.WithOpenApi();

app.MapPut("/api/quizzes/attempt/{id}/submit", async (Guid id, SubmitAttemptRequest request, IQuizService quizService) =>
{
    var result = await quizService.SubmitAttemptAsync(id, request);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("SubmitAttempt")
.WithOpenApi();

app.MapGet("/api/quizzes/attempts/{studentId}/{quizId}", async (Guid studentId, Guid quizId, HttpContext context, IQuizService quizService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != studentId && userRole != "ADMIN" && userRole != "INSTRUCTOR")
        {
            return Results.Forbid();
        }
    }

    var result = await quizService.GetAttemptsByStudentAsync(studentId, quizId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetAttemptsByStudent")
.WithOpenApi();

app.MapGet("/api/quizzes/best-attempt/{studentId}/{quizId}", async (Guid studentId, Guid quizId, HttpContext context, IQuizService quizService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != studentId && userRole != "ADMIN" && userRole != "INSTRUCTOR")
        {
            return Results.Forbid();
        }
    }

    var result = await quizService.GetBestAttemptAsync(studentId, quizId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetBestAttempt")
.WithOpenApi();

app.MapGet("/api/quizzes/attempt-count/{studentId}/{quizId}", async (Guid studentId, Guid quizId, HttpContext context, IQuizService quizService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != studentId && userRole != "ADMIN" && userRole != "INSTRUCTOR")
        {
            return Results.Forbid();
        }
    }

    var result = await quizService.GetAttemptCountAsync(studentId, quizId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetAttemptCount")
.WithOpenApi();

app.Run();
