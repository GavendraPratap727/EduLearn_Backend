using EduLearn.LessonService.Authorization;
using EduLearn.LessonService.Data;
using EduLearn.LessonService.Models;
using EduLearn.LessonService.Repositories;
using EduLearn.LessonService.Services;
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
builder.Services.AddDbContext<LessonDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository
builder.Services.AddScoped<ILessonRepository, LessonRepository>();

// Add Service
builder.Services.AddScoped<ILessonService, LessonService>();

// Add Authorization Helper
builder.Services.AddScoped<JwtAuthorizationHelper>();

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

// Lesson endpoints
app.MapPost("/api/lessons", async (CreateLessonRequest request, ILessonService lessonService) =>
{
    var result = await lessonService.AddLessonAsync(request);
    return Results.Ok(result);
})
.RequireAuthorization()
.WithName("AddLesson")
.WithOpenApi();

app.MapGet("/api/lessons/{id}", async (Guid id, ILessonService lessonService) =>
{
    var result = await lessonService.GetLessonByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.WithName("GetLessonById")
.WithOpenApi();

app.MapGet("/api/lessons/course/{courseId}", async (Guid courseId, ILessonService lessonService) =>
{
    var result = await lessonService.GetLessonsByCourseAsync(courseId);
    return Results.Ok(result);
})
.WithName("GetLessonsByCourse")
.WithOpenApi();

app.MapGet("/api/lessons/course/{courseId}/ordered", async (Guid courseId, ILessonService lessonService) =>
{
    var result = await lessonService.GetOrderedLessonsAsync(courseId);
    return Results.Ok(result);
})
.WithName("GetOrderedLessons")
.WithOpenApi();

app.MapGet("/api/lessons/preview/{courseId}", async (Guid courseId, ILessonService lessonService) =>
{
    var result = await lessonService.GetPreviewLessonsAsync(courseId);
    return Results.Ok(result);
})
.WithName("GetPreviewLessons")
.WithOpenApi();

app.MapPut("/api/lessons/{id}", async (Guid id, UpdateLessonRequest request, ILessonService lessonService) =>
{
    var result = await lessonService.UpdateLessonAsync(id, request);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("UpdateLesson")
.WithOpenApi();

app.MapPut("/api/lessons/reorder", async (Guid courseId, List<Guid> lessonIds, ILessonService lessonService) =>
{
    var result = await lessonService.ReorderLessonsAsync(courseId, lessonIds);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("ReorderLessons")
.WithOpenApi();

app.MapPut("/api/lessons/{id}/publish", async (Guid id, ILessonService lessonService) =>
{
    var result = await lessonService.PublishLessonAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("PublishLesson")
.WithOpenApi();

app.MapDelete("/api/lessons/{id}", async (Guid id, ILessonService lessonService) =>
{
    var result = await lessonService.DeleteLessonAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("DeleteLesson")
.WithOpenApi();

app.MapDelete("/api/lessons/course/{courseId}", async (Guid courseId, ILessonService lessonService) =>
{
    var result = await lessonService.DeleteAllForCourseAsync(courseId);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("DeleteAllForCourse")
.WithOpenApi();

app.MapGet("/api/lessons/course/{courseId}/count", async (Guid courseId, ILessonService lessonService) =>
{
    var result = await lessonService.GetLessonCountAsync(courseId);
    return Results.Ok(result);
})
.WithName("GetLessonCount")
.WithOpenApi();

app.Run();
