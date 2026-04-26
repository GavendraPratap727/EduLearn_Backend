using EduLearn.QuizService.Models;
using EduLearn.QuizService.Repositories;
using System.Text.Json;

namespace EduLearn.QuizService.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _repository;

        public QuizService(IQuizRepository repository)
        {
            _repository = repository;
        }

        public async Task<QuizResponse> CreateQuizAsync(CreateQuizRequest request)
        {
            var quiz = new Quiz
            {
                QuizId = Guid.NewGuid(),
                CourseId = request.CourseId,
                LessonId = request.LessonId,
                Title = request.Title,
                Description = request.Description,
                TimeLimitMinutes = request.TimeLimitMinutes,
                PassingScore = request.PassingScore,
                MaxAttempts = request.MaxAttempts,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow,
                Questions = JsonSerializer.Serialize(request.Questions)
            };

            var createdQuiz = await _repository.AddAsync(quiz);

            return new QuizResponse
            {
                Success = true,
                Message = "Quiz created successfully",
                Quiz = MapToQuizDto(createdQuiz)
            };
        }

        public async Task<QuizResponse> GetQuizByIdAsync(Guid quizId)
        {
            var quiz = await _repository.FindByQuizIdAsync(quizId);
            if (quiz == null)
            {
                return new QuizResponse
                {
                    Success = false,
                    Message = "Quiz not found"
                };
            }

            return new QuizResponse
            {
                Success = true,
                Message = "Quiz found",
                Quiz = MapToQuizDetailDto(quiz)
            };
        }

        public async Task<QuizResponse> GetQuizzesByCourseAsync(Guid courseId)
        {
            var quizzes = await _repository.FindByCourseIdListAsync(courseId);
            return new QuizResponse
            {
                Success = true,
                Message = $"Found {quizzes.Count} quizzes",
                Quiz = new QuizDto
                {
                    QuizId = Guid.Empty,
                    CourseId = courseId,
                    Title = "Multiple Quizzes"
                }
            };
        }

        public async Task<QuizResponse> GetQuizByLessonAsync(Guid lessonId)
        {
            var quiz = await _repository.FindByLessonIdAsync(lessonId);
            if (quiz == null)
            {
                return new QuizResponse
                {
                    Success = false,
                    Message = "Quiz not found"
                };
            }

            return new QuizResponse
            {
                Success = true,
                Message = "Quiz found",
                Quiz = MapToQuizDetailDto(quiz)
            };
        }

        public async Task<QuizResponse> UpdateQuizAsync(Guid quizId, UpdateQuizRequest request)
        {
            var quiz = await _repository.FindByQuizIdAsync(quizId);
            if (quiz == null)
            {
                return new QuizResponse
                {
                    Success = false,
                    Message = "Quiz not found"
                };
            }

            if (request.Title != null) quiz.Title = request.Title;
            if (request.Description != null) quiz.Description = request.Description;
            if (request.TimeLimitMinutes.HasValue) quiz.TimeLimitMinutes = request.TimeLimitMinutes.Value;
            if (request.PassingScore.HasValue) quiz.PassingScore = request.PassingScore.Value;
            if (request.MaxAttempts.HasValue) quiz.MaxAttempts = request.MaxAttempts.Value;
            if (request.Questions != null) quiz.Questions = JsonSerializer.Serialize(request.Questions);

            var updatedQuiz = await _repository.UpdateAsync(quiz);

            return new QuizResponse
            {
                Success = true,
                Message = "Quiz updated successfully",
                Quiz = MapToQuizDto(updatedQuiz)
            };
        }

        public async Task<QuizResponse> DeleteQuizAsync(Guid quizId)
        {
            var quiz = await _repository.FindByQuizIdAsync(quizId);
            if (quiz == null)
            {
                return new QuizResponse
                {
                    Success = false,
                    Message = "Quiz not found"
                };
            }

            await _repository.DeleteAsync(quiz);

            return new QuizResponse
            {
                Success = true,
                Message = "Quiz deleted successfully"
            };
        }

        public async Task<QuizResponse> PublishQuizAsync(Guid quizId)
        {
            var quiz = await _repository.FindByQuizIdAsync(quizId);
            if (quiz == null)
            {
                return new QuizResponse
                {
                    Success = false,
                    Message = "Quiz not found"
                };
            }

            quiz.IsPublished = true;
            var updatedQuiz = await _repository.UpdateAsync(quiz);

            return new QuizResponse
            {
                Success = true,
                Message = "Quiz published successfully",
                Quiz = MapToQuizDto(updatedQuiz)
            };
        }

        public async Task<QuizAttemptResponse> StartAttemptAsync(Guid studentId, StartAttemptRequest request)
        {
            var quiz = await _repository.FindByQuizIdAsync(request.QuizId);
            if (quiz == null)
            {
                return new QuizAttemptResponse
                {
                    Success = false,
                    Message = "Quiz not found"
                };
            }

            if (!quiz.IsPublished)
            {
                return new QuizAttemptResponse
                {
                    Success = false,
                    Message = "Quiz is not published"
                };
            }

            var attemptCount = await _repository.CountAttemptsAsync(studentId, request.QuizId);
            if (attemptCount >= quiz.MaxAttempts)
            {
                return new QuizAttemptResponse
                {
                    Success = false,
                    Message = $"Maximum attempts ({quiz.MaxAttempts}) reached"
                };
            }

            var attempt = new QuizAttempt
            {
                AttemptId = Guid.NewGuid(),
                QuizId = request.QuizId,
                StudentId = studentId,
                Score = 0,
                IsPassed = false,
                StartedAt = DateTime.UtcNow,
                SubmittedAt = null,
                Answers = string.Empty
            };

            var createdAttempt = await _repository.AddAttemptAsync(attempt);

            return new QuizAttemptResponse
            {
                Success = true,
                Message = "Quiz attempt started",
                Attempt = MapToQuizAttemptDto(createdAttempt)
            };
        }

        public async Task<QuizAttemptResponse> SubmitAttemptAsync(Guid attemptId, SubmitAttemptRequest request)
        {
            var attempt = await _repository.FindAttemptByIdAsync(attemptId);
            if (attempt == null)
            {
                return new QuizAttemptResponse
                {
                    Success = false,
                    Message = "Attempt not found"
                };
            }

            if (attempt.SubmittedAt.HasValue)
            {
                return new QuizAttemptResponse
                {
                    Success = false,
                    Message = "Attempt already submitted"
                };
            }

            var quiz = await _repository.FindByQuizIdAsync(attempt.QuizId);
            if (quiz == null)
            {
                return new QuizAttemptResponse
                {
                    Success = false,
                    Message = "Quiz not found"
                };
            }

            // Calculate score
            var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(quiz.Questions);
            int correctAnswers = 0;

            foreach (var question in questions!)
            {
                if (request.Answers.TryGetValue(question.QuestionId, out int selectedAnswer))
                {
                    if (selectedAnswer == question.CorrectAnswer)
                    {
                        correctAnswers++;
                    }
                }
            }

            int score = questions.Count > 0 ? (correctAnswers * 100) / questions.Count : 0;
            bool isPassed = score >= quiz.PassingScore;

            attempt.Score = score;
            attempt.IsPassed = isPassed;
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.Answers = JsonSerializer.Serialize(request.Answers);

            var updatedAttempt = await _repository.UpdateAttemptAsync(attempt);

            return new QuizAttemptResponse
            {
                Success = true,
                Message = $"Quiz submitted. Score: {score}%. {(isPassed ? "Passed" : "Failed")}",
                Attempt = MapToQuizAttemptDto(updatedAttempt)
            };
        }

        public async Task<AttemptsResponse> GetAttemptsByStudentAsync(Guid studentId, Guid quizId)
        {
            var attempts = await _repository.FindAttemptsByStudentAndQuizAsync(studentId, quizId);

            return new AttemptsResponse
            {
                Success = true,
                Message = $"Found {attempts.Count} attempts",
                Attempts = attempts.Select(MapToQuizAttemptDto).ToList()
            };
        }

        public async Task<QuizAttemptResponse> GetBestAttemptAsync(Guid studentId, Guid quizId)
        {
            var attempt = await _repository.FindBestAttemptAsync(studentId, quizId);
            if (attempt == null)
            {
                return new QuizAttemptResponse
                {
                    Success = false,
                    Message = "No attempts found"
                };
            }

            return new QuizAttemptResponse
            {
                Success = true,
                Message = "Best attempt found",
                Attempt = MapToQuizAttemptDto(attempt)
            };
        }

        public async Task<CountResponse> GetAttemptCountAsync(Guid studentId, Guid quizId)
        {
            var count = await _repository.CountAttemptsAsync(studentId, quizId);

            return new CountResponse
            {
                Success = true,
                Message = "Attempt count retrieved",
                Count = count
            };
        }

        private QuizDto MapToQuizDto(Quiz quiz)
        {
            return new QuizDto
            {
                QuizId = quiz.QuizId,
                CourseId = quiz.CourseId,
                LessonId = quiz.LessonId,
                Title = quiz.Title,
                Description = quiz.Description,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                PassingScore = quiz.PassingScore,
                MaxAttempts = quiz.MaxAttempts,
                IsPublished = quiz.IsPublished,
                CreatedAt = quiz.CreatedAt
            };
        }

        private QuizDetailDto MapToQuizDetailDto(Quiz quiz)
        {
            var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(quiz.Questions) ?? new List<QuizQuestion>();
            return new QuizDetailDto
            {
                QuizId = quiz.QuizId,
                CourseId = quiz.CourseId,
                LessonId = quiz.LessonId,
                Title = quiz.Title,
                Description = quiz.Description,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                PassingScore = quiz.PassingScore,
                MaxAttempts = quiz.MaxAttempts,
                IsPublished = quiz.IsPublished,
                CreatedAt = quiz.CreatedAt,
                Questions = questions
            };
        }

        private QuizAttemptDto MapToQuizAttemptDto(QuizAttempt attempt)
        {
            Dictionary<int, int>? answers = null;
            if (!string.IsNullOrEmpty(attempt.Answers))
            {
                answers = JsonSerializer.Deserialize<Dictionary<int, int>>(attempt.Answers);
            }

            return new QuizAttemptDto
            {
                AttemptId = attempt.AttemptId,
                QuizId = attempt.QuizId,
                StudentId = attempt.StudentId,
                Score = attempt.Score,
                IsPassed = attempt.IsPassed,
                StartedAt = attempt.StartedAt,
                SubmittedAt = attempt.SubmittedAt,
                Answers = answers
            };
        }
    }
}
