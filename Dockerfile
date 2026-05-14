# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and restore dependencies
COPY ["EduLearn/EduLearn.sln", "EduLearn/"]
COPY ["EduLearn/src/Services/AuthService/*.csproj", "EduLearn/src/Services/AuthService/"]
COPY ["EduLearn/src/Services/CourseService/*.csproj", "EduLearn/src/Services/CourseService/"]
COPY ["EduLearn/src/Services/EnrollmentService/*.csproj", "EduLearn/src/Services/EnrollmentService/"]
COPY ["EduLearn/src/Services/LessonService/*.csproj", "EduLearn/src/Services/LessonService/"]
COPY ["EduLearn/src/Services/PaymentService/*.csproj", "EduLearn/src/Services/PaymentService/"]
COPY ["EduLearn/src/Services/ProgressService/*.csproj", "EduLearn/src/Services/ProgressService/"]
COPY ["EduLearn/src/Services/QuizService/*.csproj", "EduLearn/src/Services/QuizService/"]
COPY ["EduLearn/src/Services/ReviewService/*.csproj", "EduLearn/src/Services/ReviewService/"]

RUN dotnet restore "EduLearn/EduLearn.sln"

# Copy full source
COPY . .

# Build all projects in the solution
RUN dotnet publish "EduLearn/EduLearn.sln" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy all published files
COPY --from=build /app/publish .

# The SERVICE_NAME env var (from render.yaml) tells us which DLL to run
# Example: SERVICE_NAME=AuthService -> runs EduLearn.AuthService.dll
ENTRYPOINT ["sh", "-c", "dotnet EduLearn.${SERVICE_NAME}.dll --urls http://0.0.0.0:${PORT:-8080}"]
