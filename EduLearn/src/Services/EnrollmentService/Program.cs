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
builder.Services.AddDbContext<EnrollmentDbContext>(options =>
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
        options.UseSqlite("Data Source=enrollment_fallback.db");
    }
    else if (connectionString.Contains("Data Source") || connectionString.Contains(".db"))
    {
        options.UseSqlite(connectionString.Trim());
    }
    else
    {
        options.UseNpgsql(connectionString.Trim(), x => x.MigrationsHistoryTable("__EnrollmentMigrationsHistory"));
    }
});

// Add Repository
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

// Add HttpClient
builder.Services.AddHttpClient<IEnrollmentService, EnrollmentService>();

// Add Service
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();


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
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>();
        
        // Targeted Reset: Only drop tables belonging to this service to avoid conflicts in shared DB
        try {
            Console.WriteLine("Force Reset: Wiping EnrollmentService tables...");
            dbContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS \"Enrollments\" CASCADE;");
            Console.WriteLine("EnrollmentService table wipe successful.");
        } catch (Exception ex) { 
            Console.WriteLine($"Reset Warning: {ex.Message}");
        }

        Console.WriteLine("Applying schema (Forced Create)...");
        try {
            // EnsureCreated skips if ANY table exists. We force it by running the script manually.
            var script = dbContext.Database.GenerateCreateScript();
            dbContext.Database.ExecuteSqlRaw(script);
            Console.WriteLine("Database initialized successfully via forced script.");
        } catch (Exception ex) {
            Console.WriteLine($"Forced Create Note: {ex.Message} (Usually means tables already exist).");
            // Fallback to EnsureCreated just in case
            dbContext.Database.EnsureCreated();
        }
    } 
    catch (Exception dbEx) 
    {
        Console.WriteLine($"CRITICAL: Database initialization failed: {dbEx.Message}");
        if (dbEx.InnerException != null) 
            Console.WriteLine($"INNER ERROR: {dbEx.InnerException.Message}");
        throw; 
    }
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EnrollmentService API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");

// Global Error Handler to preserve CORS headers on crash
app.Use(async (context, next) => {
    try {
        await next();
    } catch (Exception ex) {
        Console.WriteLine($"CRASH [V6]: {ex.Message}");
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        // Ensure CORS headers are present even on 500
        if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin")) {
            context.Response.Headers.Add("Access-Control-Allow-Origin", context.Request.Headers["Origin"].ToString() ?? "*");
        }
        await context.Response.WriteAsJsonAsync(new { 
            error = ex.Message, 
            detail = ex.InnerException?.Message,
            marker = "V6-GLOBAL-CATCH"
        });
    }
});
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

// Enrollment endpoints
app.MapPost("/api/enrollments", async (HttpContext context, CreateEnrollmentRequest request, IEnrollmentService enrollmentService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    request.StudentId = studentId;
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

app.MapDelete("/api/enrollments/course/{id}", async (Guid id, IEnrollmentService enrollmentService) =>
{
    var result = await enrollmentService.DeleteEnrollmentsByCourseAsync(id);
    return Results.Ok(result);
})
.RequireAuthorization("InstructorOrAdmin")
.WithName("DeleteEnrollmentsByCourse")
.WithOpenApi();

// Health check endpoint
app.MapGet("/api/enrollments/health", () =>
{
    return Results.Ok(new { status = "Healthy", service = "EnrollmentService", timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();
