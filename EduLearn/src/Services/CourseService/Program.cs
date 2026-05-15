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
    var dbHost = builder.Configuration["DB_HOST"];
    var dbPort = builder.Configuration["DB_PORT"] ?? "5432";
    var dbName = builder.Configuration["DB_NAME"];
    var dbUser = builder.Configuration["DB_USER"];
    var dbPass = builder.Configuration["DB_PASSWORD"];

    string? connectionString = null;

    if (!string.IsNullOrWhiteSpace(dbHost) && !string.IsNullOrWhiteSpace(dbName))
    {
        connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true";
    }
    else
    {
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                         ?? builder.Configuration["DefaultConnection"];
    }

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.WriteLine("Warning: No database connection information found. Falling back to local SQLite.");
        options.UseSqlite("Data Source=course_fallback.db");
    }
    else if (connectionString.Contains("Data Source") || connectionString.Contains(".db"))
    {
        options.UseSqlite(connectionString.Trim());
    }
    else
    {
        options.UseNpgsql(connectionString.Trim(), x => x.MigrationsHistoryTable("__CourseMigrationsHistory"));
    }
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
            .WithOrigins(
                "http://localhost:4200", 
                "http://localhost:60804",
                "https://edulearn-frontend-9lw4.onrender.com",
                "https://edulearn-frontend.onrender.com",
                "https://edulearn-frontends.onrender.com",
                "https://edulearn-frontend-zn5e.onrender.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

// Initialize database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
        Console.WriteLine("Applying migrations...");
        
        // Temporary fix: Drop broken tables from previous failed attempts to ensure a clean migration
        try {
            dbContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS \"Courses\" CASCADE;");
            dbContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS \"__EFMigrationsHistory\" CASCADE;");
        } catch { /* Ignore if tables don't exist */ }

        dbContext.Database.Migrate();
        SeedData.Initialize(dbContext);
        Console.WriteLine("Database initialized successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Critical Error: Database initialization failed: {ex.Message}");
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CourseService API V1");
    c.RoutePrefix = "swagger";
});

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

// Health check endpoint
app.MapGet("/api/courses/health", () =>
{
    return Results.Ok(new { status = "Healthy", service = "CourseService", timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();
