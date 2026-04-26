using EduLearn.EnrollmentService.Data;
using EduLearn.EnrollmentService.Models;
using EduLearn.EnrollmentService.Repositories;
using EduLearn.EnrollmentService.Services;
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
builder.Services.AddDbContext<EnrollmentDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

// Add HttpClient
builder.Services.AddHttpClient<IEnrollmentService, EnrollmentService>();

// Add Service
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

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

// Enrollment endpoints
app.MapPost("/api/enrollments", async (CreateEnrollmentRequest request, IEnrollmentService enrollmentService) =>
{
    var result = await enrollmentService.EnrollAsync(request);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("Enroll")
.WithOpenApi();

app.MapGet("/api/enrollments/{id}", async (Guid id, IEnrollmentService enrollmentService) =>
{
    var result = await enrollmentService.GetEnrollmentByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetEnrollmentById")
.WithOpenApi();

app.MapGet("/api/enrollments/student/{id}", async (Guid id, HttpContext context, IEnrollmentService enrollmentService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Students can only view their own enrollments, Admins can view any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != id && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await enrollmentService.GetEnrollmentsByStudentAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetEnrollmentsByStudent")
.WithOpenApi();

app.MapGet("/api/enrollments/course/{id}", async (Guid id, IEnrollmentService enrollmentService) =>
{
    var result = await enrollmentService.GetEnrollmentsByCourseAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("GetEnrollmentsByCourse")
.WithOpenApi();

app.MapGet("/api/enrollments/isEnrolled", async (Guid studentId, Guid courseId, IEnrollmentService enrollmentService) =>
{
    var result = await enrollmentService.IsEnrolledAsync(studentId, courseId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("IsEnrolled")
.WithOpenApi();

app.MapPut("/api/enrollments/{id}/progress", async (Guid id, UpdateProgressRequest request, IEnrollmentService enrollmentService) =>
{
    var result = await enrollmentService.UpdateProgressAsync(id, request);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("UpdateProgress")
.WithOpenApi();

app.MapPut("/api/enrollments/{id}/complete", async (Guid id, IEnrollmentService enrollmentService) =>
{
    var result = await enrollmentService.CompleteEnrollmentAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("CompleteEnrollment")
.WithOpenApi();

app.MapPut("/api/enrollments/{id}/drop", async (Guid id, IEnrollmentService enrollmentService) =>
{
    var result = await enrollmentService.DropCourseAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("DropCourse")
.WithOpenApi();

app.MapGet("/api/enrollments/completed/{id}", async (Guid id, HttpContext context, IEnrollmentService enrollmentService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Students can only view their own completed courses, Admins can view any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != id && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await enrollmentService.GetCompletedCoursesAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetCompletedCourses")
.WithOpenApi();

app.MapGet("/api/enrollments/inProgress/{id}", async (Guid id, HttpContext context, IEnrollmentService enrollmentService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Students can only view their own in-progress courses, Admins can view any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != id && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await enrollmentService.GetInProgressCoursesAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetInProgressCourses")
.WithOpenApi();

app.MapGet("/api/enrollments/course/{id}/count", async (Guid id, IEnrollmentService enrollmentService) =>
{
    var result = await enrollmentService.GetEnrollmentCountAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("GetEnrollmentCount")
.WithOpenApi();

app.Run();
