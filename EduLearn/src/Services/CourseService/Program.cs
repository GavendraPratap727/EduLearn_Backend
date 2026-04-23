using EduLearn.CourseService.Data;
using EduLearn.CourseService.Models;
using EduLearn.CourseService.Repositories;
using EduLearn.CourseService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<CourseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository
builder.Services.AddScoped<ICourseRepository, CourseRepository>();

// Add Service
builder.Services.AddScoped<ICourseService, CourseService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Course endpoints
app.MapPost("/api/courses", async (CreateCourseRequest request, ICourseService courseService) =>
{
    var result = await courseService.CreateCourseAsync(request);
    return Results.Ok(result);
})
.WithName("CreateCourse")
.WithOpenApi();

app.MapGet("/api/courses/{id}", async (Guid id, ICourseService courseService) =>
{
    var result = await courseService.GetCourseByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.WithName("GetCourseById")
.WithOpenApi();

app.MapGet("/api/courses/instructor/{id}", async (Guid id, ICourseService courseService) =>
{
    var result = await courseService.GetCoursesByInstructorAsync(id);
    return Results.Ok(result);
})
.WithName("GetCoursesByInstructor")
.WithOpenApi();

app.MapGet("/api/courses/category/{category}", async (string category, ICourseService courseService) =>
{
    var result = await courseService.GetCoursesByCategoryAsync(category);
    return Results.Ok(result);
})
.WithName("GetCoursesByCategory")
.WithOpenApi();

app.MapGet("/api/courses/published", async (ICourseService courseService) =>
{
    var result = await courseService.GetPublishedCoursesAsync();
    return Results.Ok(result);
})
.WithName("GetPublishedCourses")
.WithOpenApi();

app.MapGet("/api/courses/search", async (string keyword, ICourseService courseService) =>
{
    var result = await courseService.SearchCoursesAsync(keyword);
    return Results.Ok(result);
})
.WithName("SearchCourses")
.WithOpenApi();

app.MapPut("/api/courses/{id}", async (Guid id, UpdateCourseRequest request, ICourseService courseService) =>
{
    var result = await courseService.UpdateCourseAsync(id, request);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.WithName("UpdateCourse")
.WithOpenApi();

app.MapPut("/api/courses/{id}/publish", async (Guid id, ICourseService courseService) =>
{
    var result = await courseService.PublishCourseAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.WithName("PublishCourse")
.WithOpenApi();

app.MapPut("/api/courses/{id}/approve", async (Guid id, ICourseService courseService) =>
{
    var result = await courseService.ApproveCourseAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.WithName("ApproveCourse")
.WithOpenApi();

app.MapDelete("/api/courses/{id}", async (Guid id, ICourseService courseService) =>
{
    var result = await courseService.DeleteCourseAsync(id);
    return Results.Ok(result);
})
.WithName("DeleteCourse")
.WithOpenApi();

app.MapGet("/api/courses/top-rated", async (int limit, ICourseService courseService) =>
{
    var result = await courseService.GetTopRatedCoursesAsync(limit);
    return Results.Ok(result);
})
.WithName("GetTopRatedCourses")
.WithOpenApi();

app.MapPost("/api/courses/{id}/increment-enrollment", async (Guid id, ICourseService courseService) =>
{
    await courseService.IncrementEnrollmentAsync(id);
    return Results.Ok(new { Success = true, Message = "Enrollment incremented successfully" });
})
.WithName("IncrementEnrollment")
.WithOpenApi();

app.Run();
