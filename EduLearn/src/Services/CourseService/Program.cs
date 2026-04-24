using EduLearn.CourseService.Authorization;
using EduLearn.CourseService.Data;
using EduLearn.CourseService.Models;
using EduLearn.CourseService.Repositories;
using EduLearn.CourseService.Services;
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
builder.Services.AddDbContext<CourseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository
builder.Services.AddScoped<ICourseRepository, CourseRepository>();

// Add Service
builder.Services.AddScoped<ICourseService, CourseService>();

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

// Course endpoints
app.MapPost("/api/courses", async (CreateCourseRequest request, ICourseService courseService) =>
{
    var result = await courseService.CreateCourseAsync(request);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("CreateCourse")
.WithOpenApi();

app.MapGet("/api/courses/{id}", async (Guid id, ICourseService courseService) =>
{
    var result = await courseService.GetCourseByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.WithName("GetCourseById")
.WithOpenApi();

app.MapGet("/api/courses/instructor/{id}", async (Guid id, HttpContext context, ICourseService courseService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Instructors can only view their own courses, Admins can view any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != id && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await courseService.GetCoursesByInstructorAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
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

app.MapPut("/api/courses/{id}", async (Guid id, UpdateCourseRequest request, HttpContext context, ICourseService courseService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Instructors can only update their own courses, Admins can update any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var course = await courseService.GetCourseByIdAsync(id);
        if (course.Success && userRole != "ADMIN" && course.Course?.InstructorId != currentUserId)
        {
            return Results.Forbid();
        }
    }
    var result = await courseService.UpdateCourseAsync(id, request);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("UpdateCourse")
.WithOpenApi();

app.MapPut("/api/courses/{id}/publish", async (Guid id, HttpContext context, ICourseService courseService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Instructors can only publish their own courses, Admins can publish any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var course = await courseService.GetCourseByIdAsync(id);
        if (course.Success && userRole != "ADMIN" && course.Course?.InstructorId != currentUserId)
        {
            return Results.Forbid();
        }
    }
    var result = await courseService.PublishCourseAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("PublishCourse")
.WithOpenApi();

app.MapPut("/api/courses/{id}/approve", async (Guid id, ICourseService courseService) =>
{
    var result = await courseService.ApproveCourseAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("AdminOnly")
.WithName("ApproveCourse")
.WithOpenApi();

app.MapDelete("/api/courses/{id}", async (Guid id, HttpContext context, ICourseService courseService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Instructors can only delete their own courses, Admins can delete any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var course = await courseService.GetCourseByIdAsync(id);
        if (course.Success && userRole != "ADMIN" && course.Course?.InstructorId != currentUserId)
        {
            return Results.Forbid();
        }
    }
    var result = await courseService.DeleteCourseAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
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
.RequireAuthorization("Authenticated")
.WithName("IncrementEnrollment")
.WithOpenApi();

app.Run();
