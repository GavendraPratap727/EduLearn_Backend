using EduLearn.ReviewService.Data;
using EduLearn.ReviewService.Models;
using EduLearn.ReviewService.Repositories;
using EduLearn.ReviewService.Services;
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
builder.Services.AddDbContext<ReviewDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repository
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

// Add HttpClient
builder.Services.AddHttpClient<IReviewService, ReviewService>();

// Add Service
builder.Services.AddScoped<IReviewService, ReviewService>();

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

// Review endpoints
app.MapPost("/api/reviews", async (HttpContext context, CreateReviewRequest request, IReviewService reviewService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    var result = await reviewService.AddReviewAsync(studentId, request);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("AddReview")
.WithOpenApi();

app.MapGet("/api/reviews/{id}", async (Guid id, IReviewService reviewService) =>
{
    var result = await reviewService.GetReviewByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetReviewById")
.WithOpenApi();

app.MapGet("/api/reviews/course/{courseId}", async (Guid courseId, bool approvedOnly, IReviewService reviewService) =>
{
    var result = await reviewService.GetReviewsByCourseAsync(courseId, approvedOnly);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetReviewsByCourse")
.WithOpenApi();

app.MapGet("/api/reviews/course/{courseId}/approved", async (Guid courseId, IReviewService reviewService) =>
{
    var result = await reviewService.GetApprovedReviewsAsync(courseId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetApprovedReviews")
.WithOpenApi();

app.MapGet("/api/reviews/student/{studentId}", async (Guid studentId, HttpContext context, IReviewService reviewService) =>
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

    var result = await reviewService.GetReviewsByStudentAsync(studentId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetReviewsByStudent")
.WithOpenApi();

app.MapPut("/api/reviews/{id}", async (Guid id, HttpContext context, UpdateReviewRequest request, IReviewService reviewService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    var result = await reviewService.UpdateReviewAsync(id, studentId, request);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("UpdateReview")
.WithOpenApi();

app.MapPut("/api/reviews/{id}/approve", async (Guid id, IReviewService reviewService) =>
{
    var result = await reviewService.ApproveReviewAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("AdminOnly")
.WithName("ApproveReview")
.WithOpenApi();

app.MapDelete("/api/reviews/{id}", async (Guid id, HttpContext context, IReviewService reviewService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(currentUserIdClaim, out Guid studentId))
    {
        return Results.Unauthorized();
    }

    var result = await reviewService.DeleteReviewAsync(id, studentId);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
})
.RequireAuthorization("Authenticated")
.WithName("DeleteReview")
.WithOpenApi();

app.MapGet("/api/reviews/average/{courseId}", async (Guid courseId, IReviewService reviewService) =>
{
    var result = await reviewService.GetAverageRatingAsync(courseId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("GetAverageRating")
.WithOpenApi();

app.MapGet("/api/reviews/count/{courseId}", async (Guid courseId, IReviewService reviewService) =>
{
    var count = await reviewService.GetReviewCountAsync(courseId);
    return Results.Ok(new { Success = true, Count = count });
})
.RequireAuthorization("Authenticated")
.WithName("GetReviewCount")
.WithOpenApi();

app.MapGet("/api/reviews/hasReviewed/{studentId}/{courseId}", async (Guid studentId, Guid courseId, HttpContext context, IReviewService reviewService) =>
{
    var currentUserIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (currentUserId != studentId && userRole != "ADMIN")
        {
            return Results.Forbid();
        }
    }

    var result = await reviewService.HasStudentReviewedAsync(studentId, courseId);
    return Results.Ok(result);
})
.RequireAuthorization("Authenticated")
.WithName("HasStudentReviewed")
.WithOpenApi();

app.Run();
