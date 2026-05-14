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
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder
            .WithOrigins(
                "http://localhost:4200", 
                "http://localhost:60804",
                "https://edulearn-frontend-9lw4.onrender.com",
                "https://edulearn-frontend.onrender.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
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
builder.Services.AddDbContext<ProgressDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                         ?? builder.Configuration["DefaultConnection"];

    if (string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("Warning: DefaultConnection not found. Falling back to local SQLite.");
        options.UseSqlite("Data Source=progress_fallback.db");
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// Add Repository
builder.Services.AddScoped<IProgressRepository, ProgressRepository>();

// Add HttpClient
builder.Services.AddHttpClient<IProgressService, ProgressService>();

// Add Service
builder.Services.AddScoped<IProgressService, ProgressService>();

var app = builder.Build();

// Initialize database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        Console.WriteLine("Initializing database...");
        dbContext.Database.EnsureCreated();
        Console.WriteLine("Database initialized successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Critical Error: Database initialization failed: {ex.Message}");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseCors("AllowFrontend");
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
    var currentUserIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
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
    var currentUserIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
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
    var currentUserIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
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

app.MapPost("/api/progress/lesson/{lessonId}/complete", async (Guid lessonId, [Microsoft.AspNetCore.Mvc.FromQuery] Guid courseId, HttpContext context, IProgressService progressService) =>
{
    var currentUserIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    // Try to find existing progress
    var progressResult = await progressService.GetProgressByStudentAndLessonAsync(studentId, lessonId);
    Guid progressId;

    if (progressResult.Success && progressResult.Progress != null)
    {
        progressId = progressResult.Progress.ProgressId;
    }
    else
    {
        // Create new progress record
        var createResult = await progressService.CreateProgressAsync(new CreateLessonProgressRequest
        {
            StudentId = studentId,
            CourseId = courseId,
            LessonId = lessonId
        });

        if (!createResult.Success || createResult.Progress == null)
        {
            return Results.BadRequest(createResult);
        }
        progressId = createResult.Progress.ProgressId;
    }

    var result = await progressService.MarkLessonCompleteAsync(progressId);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("MarkLessonCompleteByLessonId")
.WithOpenApi();

app.MapGet("/api/progress/stats/{id}", async (Guid id, HttpContext context, IProgressService progressService) =>
{
    var currentUserIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
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

// Certificate endpoints
app.MapPost("/api/progress/certificates/issue", async (IssueCertificateRequest request, IProgressService progressService) =>
{
    var result = await progressService.IssueCertificateAsync(request);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("IssueCertificate")
.WithOpenApi();

app.MapGet("/api/progress/certificates/{id}", async (string id, IProgressService progressService) =>
{
    var result = await progressService.GetCertificateByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetCertificateById")
.WithOpenApi();

app.MapGet("/api/progress/certificates/student/{studentId}", async (Guid studentId, HttpContext context, IProgressService progressService) =>
{
    var currentUserIdClaim = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != studentId && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }
    var result = await progressService.GetCertificatesByStudentAsync(studentId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetCertificatesByStudent")
.WithOpenApi();

app.MapGet("/api/progress/certificates/verify/{verificationCode}", async (string verificationCode, IProgressService progressService) =>
{
    var result = await progressService.VerifyCertificateAsync(verificationCode);
    return Results.Ok(result);
})
.WithName("VerifyCertificate")
.WithOpenApi();

// Health check endpoint
app.MapGet("/api/progress/health", () =>
{
    return Results.Ok(new { status = "Healthy", service = "ProgressService", timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();
