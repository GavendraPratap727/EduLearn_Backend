# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy full source
COPY . .

# Publish each service individually into /app/publish
# We use separate folders during build to avoid conflicts
RUN dotnet publish "EduLearn/src/Services/AuthService/EduLearn.AuthService.csproj" -c Release -o /app/publish/Auth
RUN dotnet publish "EduLearn/src/Services/CourseService/EduLearn.CourseService.csproj" -c Release -o /app/publish/Course
RUN dotnet publish "EduLearn/src/Services/EnrollmentService/EduLearn.EnrollmentService.csproj" -c Release -o /app/publish/Enrollment
RUN dotnet publish "EduLearn/src/Services/LessonService/EduLearn.LessonService.csproj" -c Release -o /app/publish/Lesson
RUN dotnet publish "EduLearn/src/Services/PaymentService/EduLearn.PaymentService.csproj" -c Release -o /app/publish/Payment
RUN dotnet publish "EduLearn/src/Services/ProgressService/EduLearn.ProgressService.csproj" -c Release -o /app/publish/Progress
RUN dotnet publish "EduLearn/src/Services/QuizService/EduLearn.QuizService.csproj" -c Release -o /app/publish/Quiz
RUN dotnet publish "EduLearn/src/Services/ReviewService/EduLearn.ReviewService.csproj" -c Release -o /app/publish/Review

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy all published services
COPY --from=build /app/publish .

# The SERVICE_NAME env var (from render.yaml) tells us which subfolder and DLL to run
ENTRYPOINT ["sh", "-c", "case \"$SERVICE_NAME\" in \
    \"AuthService\") dotnet Auth/EduLearn.AuthService.dll --urls http://0.0.0.0:${PORT:-8080} ;; \
    \"CourseService\") dotnet Course/EduLearn.CourseService.dll --urls http://0.0.0.0:${PORT:-8080} ;; \
    \"EnrollmentService\") dotnet Enrollment/EduLearn.EnrollmentService.dll --urls http://0.0.0.0:${PORT:-8080} ;; \
    \"LessonService\") dotnet Lesson/EduLearn.LessonService.dll --urls http://0.0.0.0:${PORT:-8080} ;; \
    \"PaymentService\") dotnet Payment/EduLearn.PaymentService.dll --urls http://0.0.0.0:${PORT:-8080} ;; \
    \"ProgressService\") dotnet Progress/EduLearn.ProgressService.dll --urls http://0.0.0.0:${PORT:-8080} ;; \
    \"QuizService\") dotnet Quiz/EduLearn.QuizService.dll --urls http://0.0.0.0:${PORT:-8080} ;; \
    \"ReviewService\") dotnet Review/EduLearn.ReviewService.dll --urls http://0.0.0.0:${PORT:-8080} ;; \
    *) echo \"Unknown Service: $SERVICE_NAME\"; exit 1 ;; \
    esac"]
