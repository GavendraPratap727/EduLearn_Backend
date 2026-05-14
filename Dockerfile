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
# Example: SERVICE_NAME=AuthService -> runs /app/Auth/EduLearn.AuthService.dll
ENTRYPOINT ["sh", "-c", "if [ \"$SERVICE_NAME\" = \"AuthService\" ]; then dotnet Auth/EduLearn.AuthService.dll; \
    elif [ \"$SERVICE_NAME\" = \"CourseService\" ]; then dotnet Course/EduLearn.CourseService.dll; \
    elif [ \"$SERVICE_NAME\" = \"EnrollmentService\" ]; then dotnet Enrollment/EduLearn.EnrollmentService.dll; \
    elif [ \"$SERVICE_NAME\" = \"LessonService\" ]; then dotnet Lesson/EduLearn.LessonService.dll; \
    elif [ \"$SERVICE_NAME\" = \"PaymentService\" ]; then dotnet Payment/EduLearn.PaymentService.dll; \
    elif [ \"$SERVICE_NAME\" = \"ProgressService\" ]; then dotnet Progress/EduLearn.ProgressService.dll; \
    elif [ \"$SERVICE_NAME\" = \"QuizService\" ]; then dotnet Quiz/EduLearn.QuizService.dll; \
    elif [ \"$SERVICE_NAME\" = \"ReviewService\" ]; then dotnet Review/EduLearn.ReviewService.dll; \
    else echo \"Unknown Service: $SERVICE_NAME\"; exit 1; fi --urls http://0.0.0.0:${PORT:-8080}"]
