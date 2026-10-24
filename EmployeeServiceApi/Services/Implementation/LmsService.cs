using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.DTOs.LMS;
using EmployeeSelfServiceApi.Entities;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeSelfServiceApi.Utilities;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Services;

public class LmsService : ILmsService
{
    private const decimal PassingScorePercentage = 70m;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<LmsService> _logger;

    public LmsService(
        ApplicationDbContext context,
        ILogger<LmsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 1. Create a course
    public async Task<CourseResponseDto> CreateCourseAsync(
        CreateCourseDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
            await EnsureRoleAsync(
                actorEmployeeId,
                cancellationToken,
                "HR",
                "Admin");

            var title = request.Title.Trim();

            var duplicate = await _context.Courses.AnyAsync(
                c => c.Title.ToLower() == title.ToLower(),
                cancellationToken);

            if (duplicate)
                throw new InvalidOperationException(
                    "A course with this title already exists.");

            var now = DateTime.UtcNow;

            var course = new Course
            {
                Title = title,
                Description = request.Description.Trim(),
                Category = request.Category?.Trim(),
                Status = CourseStatus.Draft,
                CreatedByEmployeeId = actorEmployeeId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _context.Courses.AddAsync(course, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return MapCourse(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create an LMS course for employee {EmployeeId}.",
                actorEmployeeId);
            throw;
        }
    }

    // 2. Update a draft course
    public async Task<CourseResponseDto> UpdateCourseAsync(
        int courseId,
        UpdateCourseDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
            await EnsureRoleAsync(
                actorEmployeeId,
                cancellationToken,
                "HR",
                "Admin");

            var course = await _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.Questions)
                .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

            if (course is null)
                throw new KeyNotFoundException("Course was not found.");

            if (course.Status != CourseStatus.Draft)
                throw new InvalidOperationException(
                    "Only draft courses can be edited.");

            var title = request.Title.Trim();

            var duplicate = await _context.Courses.AnyAsync(
                c => c.Id != courseId &&
                     c.Title.ToLower() == title.ToLower(),
                cancellationToken);

            if (duplicate)
                throw new InvalidOperationException(
                    "Another course already uses this title.");

            course.Title = title;
            course.Description = request.Description.Trim();
            course.Category = request.Category?.Trim();
            course.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return MapCourse(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update course {CourseId}.",
                courseId);
            throw;
        }
    }

    // 3. Publish a course
    public async Task<CourseResponseDto> PublishCourseAsync(
        int courseId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
            await EnsureRoleAsync(
                actorEmployeeId,
                cancellationToken,
                "HR",
                "Admin");

            var course = await _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

            if (course is null)
                throw new KeyNotFoundException("Course was not found.");

            if (course.Status != CourseStatus.Draft)
                throw new InvalidOperationException(
                    "Only draft courses can be published.");

            if (!course.Lessons.Any())
                throw new InvalidOperationException(
                    "Add at least one lesson before publishing.");

            if (!course.Questions.Any())
                throw new InvalidOperationException(
                    "Add at least one assessment question before publishing.");

            foreach (var question in course.Questions)
            {
                if (question.Options.Count < 2 ||
                    question.Options.Count(o => o.IsCorrect) != 1)
                {
                    throw new InvalidOperationException(
                        $"Question {question.OrderNumber} must have at least two options and exactly one correct answer.");
                }
            }

            course.Status = CourseStatus.Published;
            course.PublishedAtUtc = DateTime.UtcNow;
            course.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return MapCourse(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish course {CourseId}.",
                courseId);
            throw;
        }
    }

    // 4. Add a lesson
    public async Task<LessonResponseDto> AddLessonAsync(
        int courseId,
        CreateLessonDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
            await EnsureRoleAsync(
                actorEmployeeId,
                cancellationToken,
                "HR",
                "Admin");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

            if (course is null)
                throw new KeyNotFoundException("Course was not found.");

            if (course.Status != CourseStatus.Draft)
                throw new InvalidOperationException(
                    "Lessons can only be added to draft courses.");

            var orderExists = await _context.CourseLessons.AnyAsync(
                l => l.CourseId == courseId &&
                     l.OrderNumber == request.OrderNumber,
                cancellationToken);

            if (orderExists)
                throw new InvalidOperationException(
                    "A lesson already uses this order number.");

            var now = DateTime.UtcNow;

            var lesson = new CourseLesson
            {
                CourseId = courseId,
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                VideoUrl = request.VideoUrl?.Trim(),
                OrderNumber = request.OrderNumber,
                IsRequired = request.IsRequired,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _context.CourseLessons.AddAsync(lesson, cancellationToken);

            course.UpdatedAtUtc = now;

            await _context.SaveChangesAsync(cancellationToken);

            return MapLesson(lesson);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to add a lesson to course {CourseId}.",
                courseId);
            throw;
        }
    }

    // 5. Update a lesson
    public async Task<LessonResponseDto> UpdateLessonAsync(
        int courseId,
        int lessonId,
        UpdateLessonDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
            await EnsureRoleAsync(
                actorEmployeeId,
                cancellationToken,
                "HR",
                "Admin");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

            if (course is null)
                throw new KeyNotFoundException("Course was not found.");

            if (course.Status != CourseStatus.Draft)
                throw new InvalidOperationException(
                    "Lessons can only be edited while the course is a draft.");

            var lesson = await _context.CourseLessons.FirstOrDefaultAsync(
                l => l.Id == lessonId && l.CourseId == courseId,
                cancellationToken);

            if (lesson is null)
                throw new KeyNotFoundException("Lesson was not found.");

            var orderExists = await _context.CourseLessons.AnyAsync(
                l => l.CourseId == courseId &&
                     l.Id != lessonId &&
                     l.OrderNumber == request.OrderNumber,
                cancellationToken);

            if (orderExists)
                throw new InvalidOperationException(
                    "Another lesson already uses this order number.");

            lesson.Title = request.Title.Trim();
            lesson.Content = request.Content.Trim();
            lesson.VideoUrl = request.VideoUrl?.Trim();
            lesson.OrderNumber = request.OrderNumber;
            lesson.IsRequired = request.IsRequired;
            lesson.UpdatedAtUtc = DateTime.UtcNow;
            course.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return MapLesson(lesson);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update lesson {LessonId} in course {CourseId}.",
                lessonId,
                courseId);
            throw;
        }
    }

    // 6. Add an assessment question
    public async Task<AssessmentQuestionResponseDto> AddQuestionAsync(
        int courseId,
        CreateQuestionDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
            await EnsureRoleAsync(
                actorEmployeeId,
                cancellationToken,
                "HR",
                "Admin");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

            if (course is null)
                throw new KeyNotFoundException("Course was not found.");

            if (course.Status != CourseStatus.Draft)
                throw new InvalidOperationException(
                    "Questions can only be added to draft courses.");

            if (request.Options.Count < 2)
                throw new InvalidOperationException(
                    "A question must have at least two answer options.");

            if (request.Options.Count(o => o.IsCorrect) != 1)
                throw new InvalidOperationException(
                    "Exactly one answer option must be marked correct.");

            var orderExists = await _context.AssessmentQuestions.AnyAsync(
                q => q.CourseId == courseId &&
                     q.OrderNumber == request.OrderNumber,
                cancellationToken);

            if (orderExists)
                throw new InvalidOperationException(
                    "A question already uses this order number.");

            var question = new AssessmentQuestion
            {
                CourseId = courseId,
                QuestionText = request.QuestionText.Trim(),
                OrderNumber = request.OrderNumber,
                Marks = request.Marks,
                Options = request.Options.Select(option =>
                    new AssessmentOption
                    {
                        OptionText = option.OptionText.Trim(),
                        IsCorrect = option.IsCorrect
                    }).ToList()
            };

            await _context.AssessmentQuestions.AddAsync(
                question,
                cancellationToken);

            course.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return MapQuestion(question);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to add an assessment question to course {CourseId}.",
                courseId);
            throw;
        }
    }

    // 7. Assign a published course to an employee
    public async Task<CourseAssignmentResponseDto> AssignCourseAsync(
        AssignCourseDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        return await AssignCourseInternalAsync(request.CourseId, request, actorEmployeeId, cancellationToken);
    }

    public async Task<CourseAssignmentResponseDto> AssignCourseAsync(
        int courseId,
        AssignCourseDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        return await AssignCourseInternalAsync(courseId, request, actorEmployeeId, cancellationToken);
    }

    private async Task<CourseAssignmentResponseDto> AssignCourseInternalAsync(
        int courseId,
        AssignCourseDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
            await EnsureRoleAsync(
                actorEmployeeId,
                cancellationToken,
                "HR",
                "Admin",
                "Manager");

            if (request.DueAtUtc.HasValue &&
                request.DueAtUtc.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "The course due date must be in UTC.");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(
                    e => e.Id == request.EmployeeId,
                    cancellationToken);

            if (employee is null)
                throw new KeyNotFoundException("Employee was not found.");

            if (employee.Status != EmployeeStatus.Active)
                throw new InvalidOperationException(
                    "A course can only be assigned to an active employee.");

            var course = await _context.Courses
                .FirstOrDefaultAsync(
                    c => c.Id == courseId,
                    cancellationToken);

            if (course is null)
                throw new KeyNotFoundException("Course was not found.");

            if (course.Status != CourseStatus.Published)
                throw new InvalidOperationException(
                    "Only published courses can be assigned.");

            var actorIsManager = await HasRoleAsync(
                actorEmployeeId, "Manager", cancellationToken);

            if (actorIsManager && employee.ReportingManagerId != actorEmployeeId)
            {
                var isHrOrAdmin = await HasAnyRoleAsync(
                    actorEmployeeId,
                    cancellationToken,
                    "HR",
                    "Admin");

                if (!isHrOrAdmin)
                    throw new UnauthorizedAccessException(
                        "Managers can only assign courses to their direct reports.");
            }

            var alreadyAssigned = await _context.CourseAssignments.AnyAsync(
                a => a.EmployeeId == request.EmployeeId &&
                     a.CourseId == courseId,
                cancellationToken);

            if (alreadyAssigned)
                throw new InvalidOperationException(
                    "This course has already been assigned to the employee.");

            var assignment = new CourseAssignment
            {
                EmployeeId = request.EmployeeId,
                CourseId = courseId,
                AssignedAtUtc = DateTime.UtcNow,
                DueAtUtc = request.DueAtUtc,
                Status = CourseAssignmentStatus.NotStarted,
                AttemptsUsed = 0,
                AttemptCycle = 1,
                MaximumAttemptsPerCycle = 3
            };

            await _context.CourseAssignments.AddAsync(
                assignment,
                cancellationToken);

            // In-app notification + queued email, saved in the same
            // transaction as the assignment itself.
            NotificationHelper.Add(
                _context,
                employee,
                NotificationType.CourseAssignment,
                "New course assigned",
                request.DueAtUtc.HasValue
                    ? $"You have been assigned the course \"{course.Title}\", due {request.DueAtUtc.Value:yyyy-MM-dd}."
                    : $"You have been assigned the course \"{course.Title}\".",
                "CourseAssignment",
                null);

            await _context.SaveChangesAsync(cancellationToken);

            await _context.Entry(assignment)
                .Reference(a => a.Course)
                .LoadAsync(cancellationToken);

            return MapAssignment(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to assign course {CourseId} to employee {EmployeeId}.",
                courseId,
                request.EmployeeId);
            throw;
        }
    }

    // 8. Get an assignment
    public async Task<CourseAssignmentResponseDto> GetAssignmentAsync(
        int assignmentId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);

            var assignment = await _context.CourseAssignments
                .Include(a => a.Employee)
                .Include(a => a.Course)
                .Include(a => a.LessonCompletions)
                .Include(a => a.AssessmentAttempts)
                .FirstOrDefaultAsync(
                    a => a.Id == assignmentId,
                    cancellationToken);

            if (assignment is null)
                throw new KeyNotFoundException(
                    "Course assignment was not found.");

            var isOwner = assignment.EmployeeId == actorEmployeeId;
            var isHrOrAdmin = await HasAnyRoleAsync(
                actorEmployeeId,
                cancellationToken,
                "HR",
                "Admin");
            var isManager = await HasRoleAsync(
                actorEmployeeId, "Manager", cancellationToken);
            var managesEmployee = isManager &&
                assignment.Employee.ReportingManagerId == actorEmployeeId;

            if (!isOwner && !isHrOrAdmin && !managesEmployee)
                throw new UnauthorizedAccessException(
                    "You do not have permission to view this assignment.");

            return MapAssignment(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve course assignment {AssignmentId}.",
                assignmentId);
            throw;
        }
    }

    // 9. Mark a lesson as completed
    public async Task CompleteLessonAsync(
        int assignmentId,
        int lessonId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureActiveEmployeeAsync(employeeId, cancellationToken);

            var assignment = await _context.CourseAssignments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Lessons)
                .Include(a => a.LessonCompletions)
                .FirstOrDefaultAsync(
                    a => a.Id == assignmentId,
                    cancellationToken);

            if (assignment is null)
                throw new KeyNotFoundException(
                    "Course assignment was not found.");

            if (assignment.EmployeeId != employeeId)
                throw new UnauthorizedAccessException(
                    "You can only update your own course progress.");

            if (assignment.Status == CourseAssignmentStatus.Completed)
                throw new InvalidOperationException(
                    "This course assignment is already completed.");

            var lesson = assignment.Course.Lessons
                .FirstOrDefault(l => l.Id == lessonId);

            if (lesson is null)
                throw new KeyNotFoundException(
                    "The lesson does not belong to this course.");

            var alreadyCompleted = assignment.LessonCompletions
                .Any(l => l.CourseLessonId == lessonId);

            if (alreadyCompleted)
                return;

            assignment.LessonCompletions.Add(new LessonCompletion
            {
                CourseAssignmentId = assignmentId,
                CourseLessonId = lessonId,
                CompletedAtUtc = DateTime.UtcNow
            });

            if (assignment.Status == CourseAssignmentStatus.NotStarted)
            {
                assignment.Status = CourseAssignmentStatus.InProgress;
                assignment.StartedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to complete lesson {LessonId} for assignment {AssignmentId}.",
                lessonId,
                assignmentId);
            throw;
        }
    }

    // 10. Submit an assessment and calculate the score on the server
    public async Task<CourseAssignmentResponseDto> SubmitAssessmentAsync(
        int assignmentId,
        SubmitAssessmentDto request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureActiveEmployeeAsync(employeeId, cancellationToken);

            var assignment = await _context.CourseAssignments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Questions)
                        .ThenInclude(q => q.Options)
                .Include(a => a.AssessmentAttempts)
                .Include(a => a.LessonCompletions)
                .FirstOrDefaultAsync(
                    a => a.Id == assignmentId,
                    cancellationToken);

            if (assignment is null)
                throw new KeyNotFoundException(
                    "Course assignment was not found.");

            if (assignment.EmployeeId != employeeId)
                throw new UnauthorizedAccessException(
                    "You can only submit your own assessment.");

            if (assignment.Status == CourseAssignmentStatus.Completed)
                throw new InvalidOperationException(
                    "This course has already been completed.");

            if (assignment.AttemptsUsed >= assignment.MaximumAttemptsPerCycle)
            {
                assignment.Status = CourseAssignmentStatus.AttemptsExhausted;
                await _context.SaveChangesAsync(cancellationToken);

                throw new InvalidOperationException(
                    "You have used all assessment attempts for this cycle.");
            }

            var questions = assignment.Course.Questions
                .OrderBy(q => q.OrderNumber)
                .ToList();

            if (questions.Count == 0)
                throw new InvalidOperationException(
                    "This course has no assessment questions.");

            var submittedAnswers = request.Answers
                .GroupBy(a => a.QuestionId)
                .ToList();

            if (submittedAnswers.Any(g => g.Count() > 1))
                throw new ArgumentException(
                    "Each assessment question can only be answered once.");

            if (submittedAnswers.Count != questions.Count)
                throw new ArgumentException(
                    "Please submit an answer for every assessment question.");

            var answersByQuestion = request.Answers
                .ToDictionary(a => a.QuestionId, a => a.SelectedOptionId);

            var totalMarks = questions.Sum(q => q.Marks);

            if (totalMarks <= 0)
                throw new InvalidOperationException(
                    "The assessment has an invalid total mark value.");

            var attemptNumber = assignment.AssessmentAttempts
                .Where(a => a.AttemptCycle == assignment.AttemptCycle)
                .Select(a => a.AttemptNumber)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var attempt = new AssessmentAttempt
            {
                CourseAssignmentId = assignmentId,
                AttemptCycle = assignment.AttemptCycle,
                AttemptNumber = attemptNumber,
                StartedAtUtc = DateTime.UtcNow,
                Answers = new List<AssessmentAnswer>()
            };

            decimal score = 0;

            foreach (var question in questions)
            {
                if (!answersByQuestion.TryGetValue(
                        question.Id,
                        out var selectedOptionId))
                {
                    throw new ArgumentException(
                        $"An answer is missing for question {question.Id}.");
                }

                var selectedOption = question.Options
                    .FirstOrDefault(o => o.Id == selectedOptionId);

                if (selectedOption is null)
                {
                    throw new ArgumentException(
                        $"The selected option does not belong to question {question.Id}.");
                }

                var isCorrect = selectedOption.IsCorrect;
                var marksAwarded = isCorrect ? question.Marks : 0m;

                score += marksAwarded;

                attempt.Answers.Add(new AssessmentAnswer
                {
                    AssessmentQuestionId = question.Id,
                    SelectedOptionId = selectedOptionId,
                    IsCorrect = isCorrect,
                    MarksAwarded = marksAwarded
                });
            }

            var scorePercentage = Math.Round(
                score / totalMarks * 100m,
                2,
                MidpointRounding.AwayFromZero);

            attempt.Score = score;
            attempt.ScorePercentage = scorePercentage;
            attempt.Passed = scorePercentage >= PassingScorePercentage;
            attempt.SubmittedAtUtc = DateTime.UtcNow;

            assignment.AttemptsUsed++;
            assignment.BestScorePercentage =
                assignment.BestScorePercentage.HasValue
                    ? Math.Max(
                        assignment.BestScorePercentage.Value,
                        scorePercentage)
                    : scorePercentage;

            if (assignment.Status == CourseAssignmentStatus.NotStarted)
            {
                assignment.Status = CourseAssignmentStatus.InProgress;
                assignment.StartedAtUtc = DateTime.UtcNow;
            }

            var requiredLessons = assignment.Course.Lessons
                .Where(l => l.IsRequired)
                .Select(l => l.Id)
                .ToHashSet();

            var completedLessons = assignment.LessonCompletions
                .Select(l => l.CourseLessonId)
                .ToHashSet();

            var allRequiredLessonsCompleted =
                requiredLessons.IsSubsetOf(completedLessons);

            if (attempt.Passed && allRequiredLessonsCompleted)
            {
                assignment.Status = CourseAssignmentStatus.Completed;
                assignment.CompletedAtUtc = DateTime.UtcNow;
            }
            else if (assignment.AttemptsUsed >=
                     assignment.MaximumAttemptsPerCycle &&
                     !attempt.Passed)
            {
                assignment.Status = CourseAssignmentStatus.AttemptsExhausted;
            }
            else
            {
                assignment.Status = CourseAssignmentStatus.InProgress;
            }

            await _context.AssessmentAttempts.AddAsync(
                attempt,
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return MapAssignment(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to submit assessment for assignment {AssignmentId}.",
                assignmentId);
            throw;
        }
    }

    // 11. Reset assessment attempts for a new cycle
    public async Task ResetAssignmentAsync(
        int assignmentId,
        string reason,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
            await EnsureRoleAsync(
                actorEmployeeId,
                cancellationToken,
                "HR",
                "Admin");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException(
                    "A reason is required to reset an assignment.");

            var assignment = await _context.CourseAssignments
                .FirstOrDefaultAsync(
                    a => a.Id == assignmentId,
                    cancellationToken);

            if (assignment is null)
                throw new KeyNotFoundException(
                    "Course assignment was not found.");

            if (assignment.Status == CourseAssignmentStatus.Completed)
                throw new InvalidOperationException(
                    "A completed course assignment cannot be reset.");

            var previousCycle = assignment.AttemptCycle;
            var newCycle = previousCycle + 1;

            assignment.ResetHistory.Add(new AssignmentResetHistory
            {
                CourseAssignmentId = assignment.Id,
                ResetByEmployeeId = actorEmployeeId,
                Reason = reason.Trim(),
                PreviousAttemptCycle = previousCycle,
                NewAttemptCycle = newCycle,
                ResetAtUtc = DateTime.UtcNow
            });

            assignment.AttemptCycle = newCycle;
            assignment.AttemptsUsed = 0;
            assignment.Status = CourseAssignmentStatus.NotStarted;
            assignment.StartedAtUtc = null;
            assignment.CompletedAtUtc = null;

            // BestScorePercentage is retained as the employee's historical best.
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to reset course assignment {AssignmentId}.",
                assignmentId);
            throw;
        }
    }

    // ---------- Helper methods ----------

    private async Task EnsureActiveEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(
                e => e.Id == employeeId,
                cancellationToken);

        if (employee is null)
            throw new KeyNotFoundException("Employee was not found.");

        if (employee.Status != EmployeeStatus.Active)
            throw new UnauthorizedAccessException(
                "Your employee account is not active.");
    }

    private async Task EnsureRoleAsync(
        int employeeId,
        CancellationToken cancellationToken,
        params string[] allowedRoles)
    {
        if (!await HasAnyRoleAsync(
                employeeId,
                cancellationToken,
                allowedRoles))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to perform this action.");
        }
    }

    private async Task<bool> HasRoleAsync(
        int employeeId,
        string role,
        CancellationToken cancellationToken)
    {
        return await _context.EmployeeRoles.AnyAsync(
            er => er.EmployeeId == employeeId &&
                  er.Role.Name == role,
            cancellationToken);
    }

    private async Task<bool> HasAnyRoleAsync(
        int employeeId,
        CancellationToken cancellationToken,
        params string[] roles)
    {
        return await _context.EmployeeRoles.AnyAsync(
            er => er.EmployeeId == employeeId &&
                  roles.Contains(er.Role.Name),
            cancellationToken);
    }

    public async Task<EmployeeServiceApi.DTOs.Common.PagedResponseDto<CourseResponseDto>> GetCoursesAsync(
        EmployeeServiceApi.DTOs.Common.PaginationRequestDto pagination,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
        await EnsureRoleAsync(actorEmployeeId, cancellationToken, "HR", "Admin");

        var query = _context.Courses.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var courses = await query
            .Include(c => c.Lessons)
            .Include(c => c.Questions)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return PaginationService.CreateResponse(
            courses.Select(MapCourse).ToList(),
            pagination.PageNumber,
            pagination.PageSize,
            total);
    }

    public async Task<EmployeeServiceApi.DTOs.Common.PagedResponseDto<CourseAssignmentResponseDto>> GetMyAssignmentsAsync(
        int employeeId,
        EmployeeServiceApi.DTOs.Common.PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(employeeId, cancellationToken);

        var query = _context.CourseAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId);

        var total = await query.CountAsync(cancellationToken);

        var assignments = await query
            .Include(a => a.Course)
            .OrderByDescending(a => a.AssignedAtUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return PaginationService.CreateResponse(
            assignments.Select(MapAssignment).ToList(),
            pagination.PageNumber,
            pagination.PageSize,
            total);
    }

    public async Task<AssignmentContentDto> GetAssignmentContentAsync(
        int assignmentId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(employeeId, cancellationToken);

        var assignment = await _context.CourseAssignments
            .AsNoTracking()
            .Include(a => a.Course)
                .ThenInclude(c => c.Lessons)
            .Include(a => a.Course)
                .ThenInclude(c => c.Questions)
                    .ThenInclude(q => q.Options)
            .Include(a => a.LessonCompletions)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Course assignment was not found.");

        // Staff only ever see their own assigned content.
        if (assignment.EmployeeId != employeeId)
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to view this assignment.");
        }

        var completedIds = assignment.LessonCompletions
            .Select(l => l.CourseLessonId)
            .ToHashSet();

        var lessons = assignment.Course.Lessons
            .OrderBy(l => l.OrderNumber)
            .Select(l => new AssignmentLessonDto
            {
                Id = l.Id,
                Title = l.Title,
                Content = l.Content,
                VideoUrl = l.VideoUrl,
                OrderNumber = l.OrderNumber,
                IsRequired = l.IsRequired,
                IsCompleted = completedIds.Contains(l.Id)
            })
            .ToList();

        var assessmentOpen = lessons.All(l => l.IsCompleted);

        return new AssignmentContentDto
        {
            AssignmentId = assignment.Id,
            CourseTitle = assignment.Course.Title,
            Lessons = lessons,
            AssessmentOpen = assessmentOpen,
            Questions = assessmentOpen
                ? assignment.Course.Questions
                    .OrderBy(q => q.OrderNumber)
                    .Select(MapQuestion)
                    .ToList()
                : null
        };
    }

    private static CourseResponseDto MapCourse(Course course)
    {
        return new CourseResponseDto
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            Category = course.Category,
            Status = course.Status,
            CreatedByEmployeeId = course.CreatedByEmployeeId,
            CreatedAtUtc = course.CreatedAtUtc,
            UpdatedAtUtc = course.UpdatedAtUtc,
            PublishedAtUtc = course.PublishedAtUtc,
            LessonCount = course.Lessons?.Count ?? 0,
            QuestionCount = course.Questions?.Count ?? 0
        };
    }

    private static LessonResponseDto MapLesson(CourseLesson lesson)
    {
        return new LessonResponseDto
        {
            Id = lesson.Id,
            CourseId = lesson.CourseId,
            Title = lesson.Title,
            Content = lesson.Content,
            VideoUrl = lesson.VideoUrl,
            OrderNumber = lesson.OrderNumber,
            IsRequired = lesson.IsRequired,
            CreatedAtUtc = lesson.CreatedAtUtc,
            UpdatedAtUtc = lesson.UpdatedAtUtc
        };
    }

    private static AssessmentQuestionResponseDto MapQuestion(
        AssessmentQuestion question)
    {
        return new AssessmentQuestionResponseDto
        {
            Id = question.Id,
            QuestionText = question.QuestionText,
            OrderNumber = question.OrderNumber,
            Marks = question.Marks,
            Options = question.Options
                .Select(o => new AssessmentOptionResponseDto
                {
                    Id = o.Id,
                    OptionText = o.OptionText
                })
                .ToList()
        };
    }

    private static CourseAssignmentResponseDto MapAssignment(
        CourseAssignment assignment)
    {
        return new CourseAssignmentResponseDto
        {
            Id = assignment.Id,
            EmployeeId = assignment.EmployeeId,
            CourseId = assignment.CourseId,
            CourseTitle = assignment.Course?.Title ?? string.Empty,
            AssignedAtUtc = assignment.AssignedAtUtc,
            DueAtUtc = assignment.DueAtUtc,
            Status = assignment.Status,
            AttemptsUsed = assignment.AttemptsUsed,
            AttemptCycle = assignment.AttemptCycle,
            MaximumAttemptsPerCycle = assignment.MaximumAttemptsPerCycle,
            BestScorePercentage = assignment.BestScorePercentage,
            StartedAtUtc = assignment.StartedAtUtc,
            CompletedAtUtc = assignment.CompletedAtUtc
        };
    }
}