#!/bin/bash

# Wait for database to be ready (if needed)
echo "Starting EduLearn Microservices..."

# Start AuthService on port 5001
echo "Starting AuthService on port 5001..."
cd /app/src/Services/AuthService
dotnet EduLearn.AuthService.dll --urls="http://0.0.0.0:5001" &
AUTH_PID=$!

# Start CourseService on port 5002
echo "Starting CourseService on port 5002..."
cd /app/src/Services/CourseService
dotnet EduLearn.CourseService.dll --urls="http://0.0.0.0:5002" &
COURSE_PID=$!

# Start EnrollmentService on port 5003
echo "Starting EnrollmentService on port 5003..."
cd /app/src/Services/EnrollmentService
dotnet EduLearn.EnrollmentService.dll --urls="http://0.0.0.0:5003" &
ENROLLMENT_PID=$!

# Start LessonService on port 5004
echo "Starting LessonService on port 5004..."
cd /app/src/Services/LessonService
dotnet EduLearn.LessonService.dll --urls="http://0.0.0.0:5004" &
LESSON_PID=$!

# Start PaymentService on port 5005
echo "Starting PaymentService on port 5005..."
cd /app/src/Services/PaymentService
dotnet EduLearn.PaymentService.dll --urls="http://0.0.0.0:5005" &
PAYMENT_PID=$!

# Start ProgressService on port 5006
echo "Starting ProgressService on port 5006..."
cd /app/src/Services/ProgressService
dotnet EduLearn.ProgressService.dll --urls="http://0.0.0.0:5006" &
PROGRESS_PID=$!

# Start QuizService on port 5007
echo "Starting QuizService on port 5007..."
cd /app/src/Services/QuizService
dotnet EduLearn.QuizService.dll --urls="http://0.0.0.0:5007" &
QUIZ_PID=$!

# Start ReviewService on port 5008
echo "Starting ReviewService on port 5008..."
cd /app/src/Services/ReviewService
dotnet EduLearn.ReviewService.dll --urls="http://0.0.0.0:5008" &
REVIEW_PID=$!

# Wait for all services to start
sleep 5

# Check if all services are running
echo "Checking service status..."
if kill -0 $AUTH_PID 2>/dev/null; then
    echo "✅ AuthService is running (PID: $AUTH_PID)"
else
    echo "❌ AuthService failed to start"
fi

if kill -0 $COURSE_PID 2>/dev/null; then
    echo "✅ CourseService is running (PID: $COURSE_PID)"
else
    echo "❌ CourseService failed to start"
fi

if kill -0 $ENROLLMENT_PID 2>/dev/null; then
    echo "✅ EnrollmentService is running (PID: $ENROLLMENT_PID)"
else
    echo "❌ EnrollmentService failed to start"
fi

if kill -0 $LESSON_PID 2>/dev/null; then
    echo "✅ LessonService is running (PID: $LESSON_PID)"
else
    echo "❌ LessonService failed to start"
fi

if kill -0 $PAYMENT_PID 2>/dev/null; then
    echo "✅ PaymentService is running (PID: $PAYMENT_PID)"
else
    echo "❌ PaymentService failed to start"
fi

if kill -0 $PROGRESS_PID 2>/dev/null; then
    echo "✅ ProgressService is running (PID: $PROGRESS_PID)"
else
    echo "❌ ProgressService failed to start"
fi

if kill -0 $QUIZ_PID 2>/dev/null; then
    echo "✅ QuizService is running (PID: $QUIZ_PID)"
else
    echo "❌ QuizService failed to start"
fi

if kill -0 $REVIEW_PID 2>/dev/null; then
    echo "✅ ReviewService is running (PID: $REVIEW_PID)"
else
    echo "❌ ReviewService failed to start"
fi

echo "All services started. Keeping container running..."
echo "Available endpoints:"
echo "- AuthService: http://localhost:5001"
echo "- CourseService: http://localhost:5002"
echo "- EnrollmentService: http://localhost:5003"
echo "- LessonService: http://localhost:5004"
echo "- PaymentService: http://localhost:5005"
echo "- ProgressService: http://localhost:5006"
echo "- QuizService: http://localhost:5007"
echo "- ReviewService: http://localhost:5008"

# Wait for any service to exit
wait
