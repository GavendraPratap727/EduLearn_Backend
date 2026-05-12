# Use the official .NET 8.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file
COPY ["EduLearn/EduLearn.sln", "./EduLearn/"]
WORKDIR "/src/EduLearn"

# Copy all the project files
COPY ["EduLearn/src/", "./src/"]

# Restore dependencies for all projects
RUN dotnet restore "src/Services/AuthService/EduLearn.AuthService.csproj"
RUN dotnet restore "src/Services/CourseService/EduLearn.CourseService.csproj"
RUN dotnet restore "src/Services/EnrollmentService/EduLearn.EnrollmentService.csproj"
RUN dotnet restore "src/Services/LessonService/EduLearn.LessonService.csproj"
RUN dotnet restore "src/Services/PaymentService/EduLearn.PaymentService.csproj"
RUN dotnet restore "src/Services/ProgressService/EduLearn.ProgressService.csproj"
RUN dotnet restore "src/Services/QuizService/EduLearn.QuizService.csproj"
RUN dotnet restore "src/Services/ReviewService/EduLearn.ReviewService.csproj"

# Build all projects
WORKDIR "/src/EduLearn"
RUN dotnet build "EduLearn.sln" -c Release -o /app/build

# This stage is used to publish the application
FROM build AS publish
RUN dotnet publish "src/Services/AuthService/EduLearn.AuthService.csproj" -c Release -o /app/publish/auth /p:UseAppHost=false
RUN dotnet publish "src/Services/CourseService/EduLearn.CourseService.csproj" -c Release -o /app/publish/course /p:UseAppHost=false
RUN dotnet publish "src/Services/EnrollmentService/EduLearn.EnrollmentService.csproj" -c Release -o /app/publish/enrollment /p:UseAppHost=false
RUN dotnet publish "src/Services/LessonService/EduLearn.LessonService.csproj" -c Release -o /app/publish/lesson /p:UseAppHost=false
RUN dotnet publish "src/Services/PaymentService/EduLearn.PaymentService.csproj" -c Release -o /app/publish/payment /p:UseAppHost=false
RUN dotnet publish "src/Services/ProgressService/EduLearn.ProgressService.csproj" -c Release -o /app/publish/progress /p:UseAppHost=false
RUN dotnet publish "src/Services/QuizService/EduLearn.QuizService.csproj" -c Release -o /app/publish/quiz /p:UseAppHost=false
RUN dotnet publish "src/Services/ReviewService/EduLearn.ReviewService.csproj" -c Release -o /app/publish/review /p:UseAppHost=false

# Final stage: Use the runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a script to start all services
COPY ["start-services.sh", "./"]
RUN chmod +x "./start-services.sh"

# Set the default command to the startup script
CMD ["./start-services.sh"]
