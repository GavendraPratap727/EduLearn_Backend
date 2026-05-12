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
builder.Services.AddHttpContextAccessor();
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
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
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (connectionString != null && (connectionString.Contains("Data Source") || connectionString.Contains(".db")))
        options.UseSqlite(connectionString);
    else
        options.UseNpgsql(connectionString);
});

// Add Repository
builder.Services.AddScoped<ICourseRepository, CourseRepository>();

// Add Service
builder.Services.AddScoped<ICourseService, CourseService>();

// Add Authorization Helper
builder.Services.AddScoped<JwtAuthorizationHelper>();


// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder
            .WithOrigins("http://localhost:4200", "http://localhost:60804")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
    dbContext.Database.EnsureCreated();
    SeedData.Initialize(dbContext);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

// Course endpoints
app.MapGet("/api/courses", async (ICourseService courseService) =>
{
    var result = await courseService.GetAllCoursesAsync();
    return Results.Ok(result);
})
.WithName("GetAllCourses")
.WithOpenApi();

app.MapPost("/api/courses", async (CreateCourseRequest request, HttpContext context, ICourseService courseService) =>
{
    try
    {
        var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(currentUserIdClaim, out Guid instructorId))
        {
            // Force the instructor ID to be the current user's ID
            request.InstructorId = instructorId;
        }
        
        var result = await courseService.CreateCourseAsync(request);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating course: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem($"Error creating course: {ex.Message}");
    }
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("CreateCourse")
.WithOpenApi();



app.MapGet("/api/courses/pending", async (ICourseService courseService) =>
{
    var result = await courseService.GetPendingCoursesAsync();
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetPendingCourses")
.WithOpenApi();

app.MapGet("/api/courses/published", async (ICourseService courseService) =>
{
    var result = await courseService.GetPublishedCoursesAsync();
    return Results.Ok(result);
})
.WithName("GetPublishedCourses")
.WithOpenApi();

app.MapGet("/api/courses/top-rated", async (int limit, ICourseService courseService) =>
{
    var result = await courseService.GetTopRatedCoursesAsync(limit);
    return Results.Ok(result);
})
.WithName("GetTopRatedCourses")
.WithOpenApi();

app.MapGet("/api/courses/search", async (string keyword, ICourseService courseService) =>
{
    var result = await courseService.SearchCoursesAsync(keyword);
    return Results.Ok(result);
})
.WithName("SearchCourses")
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

app.MapPut("/api/courses/{id}/finish", async (Guid id, HttpContext context, ICourseService courseService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        // Instructors can only finish their own courses, Admins can finish any
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var course = await courseService.GetCourseByIdAsync(id);
        if (course.Success && userRole != "ADMIN" && course.Course?.InstructorId != currentUserId)
        {
            return Results.Forbid();
        }
    }
    var result = await courseService.FinishCourseAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("FinishCourse")
.WithOpenApi();

app.MapPut("/api/courses/{id}/approve", async (Guid id, ICourseService courseService) =>
{
    var result = await courseService.ApproveCourseAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("AdminOnly")
.WithName("ApproveCourse")
.WithOpenApi();

app.MapPut("/api/courses/{id}/reject", async (Guid id, ICourseService courseService) =>
{
    var result = await courseService.RejectCourseAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("AdminOnly")
.WithName("RejectCourse")
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




app.MapPost("/api/courses/{id}/increment-enrollment", async (Guid id, ICourseService courseService) =>
{
    await courseService.IncrementEnrollmentAsync(id);
    return Results.Ok(new { Success = true, Message = "Enrollment incremented successfully" });
})
.WithName("IncrementEnrollment")
.WithOpenApi();

app.Run();
