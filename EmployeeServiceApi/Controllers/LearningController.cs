using EmployeeSelfServiceApi.DTOs.LMS;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceApi.Controllers;

[Route("api/v1")]
public class LearningController : ApiControllerBase
{
    private readonly ILmsService _lmsService;

    public LearningController(ILmsService lmsService)
    {
        _lmsService = lmsService;
    }

    public class ResetAssignmentRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    // ---- HR: courses ----

    [Authorize(Roles = "HR,Admin")]
    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _lmsService.GetCoursesAsync(pagination, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse(
        [FromBody] CreateCourseDto request, CancellationToken ct)
        => Ok(await _lmsService.CreateCourseAsync(request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPut("courses/{courseId:int}")]
    public async Task<IActionResult> UpdateCourse(
        int courseId, [FromBody] UpdateCourseDto request, CancellationToken ct)
        => Ok(await _lmsService.UpdateCourseAsync(courseId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPost("courses/{courseId:int}/publish")]
    public async Task<IActionResult> PublishCourse(int courseId, CancellationToken ct)
        => Ok(await _lmsService.PublishCourseAsync(courseId, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPost("courses/{courseId:int}/lessons")]
    public async Task<IActionResult> AddLesson(
        int courseId, [FromBody] CreateLessonDto request, CancellationToken ct)
        => Ok(await _lmsService.AddLessonAsync(courseId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPut("courses/{courseId:int}/lessons/{lessonId:int}")]
    public async Task<IActionResult> UpdateLesson(
        int courseId, int lessonId, [FromBody] UpdateLessonDto request, CancellationToken ct)
        => Ok(await _lmsService.UpdateLessonAsync(courseId, lessonId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPost("courses/{courseId:int}/questions")]
    public async Task<IActionResult> AddQuestion(
        int courseId, [FromBody] CreateQuestionDto request, CancellationToken ct)
        => Ok(await _lmsService.AddQuestionAsync(courseId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR,Admin,Manager")]
    [HttpPost("courses/{courseId:int}/assignments")]
    public async Task<IActionResult> AssignCourse(
        int courseId, [FromBody] AssignCourseDto request, CancellationToken ct)
        => Ok(await _lmsService.AssignCourseAsync(courseId, request, CurrentEmployeeId, ct));

    // ---- Staff: my learning ----

    [HttpGet("assignments/my")]
    public async Task<IActionResult> GetMyAssignments(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _lmsService.GetMyAssignmentsAsync(CurrentEmployeeId, pagination, ct));

    [HttpGet("assignments/{assignmentId:int}")]
    public async Task<IActionResult> GetAssignment(int assignmentId, CancellationToken ct)
        => Ok(await _lmsService.GetAssignmentAsync(assignmentId, CurrentEmployeeId, ct));

    /// Lessons (and, once all lessons are done, questions without answer keys)
    [HttpGet("assignments/{assignmentId:int}/content")]
    public async Task<IActionResult> GetContent(int assignmentId, CancellationToken ct)
        => Ok(await _lmsService.GetAssignmentContentAsync(assignmentId, CurrentEmployeeId, ct));

    [HttpPost("assignments/{assignmentId:int}/lessons/{lessonId:int}/complete")]
    public async Task<IActionResult> CompleteLesson(
        int assignmentId, int lessonId, CancellationToken ct)
    {
        await _lmsService.CompleteLessonAsync(assignmentId, lessonId, CurrentEmployeeId, ct);
        return Ok(new { responseMessage = "Lesson completed." });
    }

    /// The score is calculated by the server
    [HttpPost("assignments/{assignmentId:int}/assessment")]
    public async Task<IActionResult> SubmitAssessment(
        int assignmentId, [FromBody] SubmitAssessmentDto request, CancellationToken ct)
        => Ok(await _lmsService.SubmitAssessmentAsync(assignmentId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPost("assignments/{assignmentId:int}/reset")]
    public async Task<IActionResult> ResetAssignment(
        int assignmentId, [FromBody] ResetAssignmentRequest request, CancellationToken ct)
    {
        await _lmsService.ResetAssignmentAsync(assignmentId, request.Reason, CurrentEmployeeId, ct);
        return Ok(new { responseMessage = "Assignment reset into a new attempt cycle." });
    }
}
