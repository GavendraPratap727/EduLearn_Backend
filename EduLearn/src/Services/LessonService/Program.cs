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
builder.Services.AddDbContext<LessonDbContext>(options =>
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
        options.UseSqlite("Data Source=lesson_fallback.db");
    }
    else if (connectionString.Contains("Data Source") || connectionString.Contains(".db"))
    {
        options.UseSqlite(connectionString.Trim());
    }
    else
    {
        options.UseNpgsql(connectionString.Trim());
    }
});

// Add Repository
builder.Services.AddScoped<ILessonRepository, LessonRepository>();

// Add Service
builder.Services.AddScoped<ILessonService, LessonService>();

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
                "https://edulearn-frontends.onrender.com"
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
        var dbContext = scope.ServiceProvider.GetRequiredService<LessonDbContext>();
        Console.WriteLine("Applying migrations...");
        dbContext.Database.Migrate();
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

app.UseCors("AllowFrontend");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

// Lesson endpoints
app.MapPost("/api/lessons", async (CreateLessonRequest request, ILessonService lessonService) =>
{
    var result = await lessonService.AddLessonAsync(request);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
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

// Health check endpoint
app.MapGet("/api/lessons/health", () =>
{
    return Results.Ok(new { status = "Healthy", service = "LessonService", timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();
