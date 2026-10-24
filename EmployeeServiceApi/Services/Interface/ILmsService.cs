using EmployeeSelfServiceApi.DTOs.LMS;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public interface ILmsService
{
    Task<CourseResponseDto> CreateCourseAsync(
        CreateCourseDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<CourseResponseDto> UpdateCourseAsync(
        int courseId,
        UpdateCourseDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<CourseResponseDto> PublishCourseAsync(
        int courseId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<LessonResponseDto> AddLessonAsync(
        int courseId,
        CreateLessonDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<LessonResponseDto> UpdateLessonAsync(
        int courseId,
        int lessonId,
        UpdateLessonDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<AssessmentQuestionResponseDto> AddQuestionAsync(
        int courseId,
        CreateQuestionDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<CourseAssignmentResponseDto> AssignCourseAsync(
        AssignCourseDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<CourseAssignmentResponseDto> AssignCourseAsync(
        int courseId,
        AssignCourseDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task CompleteLessonAsync(
        int assignmentId,
        int lessonId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<CourseAssignmentResponseDto> SubmitAssessmentAsync(
        int assignmentId,
        SubmitAssessmentDto request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task ResetAssignmentAsync(
        int assignmentId,
        string reason,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<CourseAssignmentResponseDto> GetAssignmentAsync(
        int assignmentId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<EmployeeServiceApi.DTOs.Common.PagedResponseDto<CourseResponseDto>> GetCoursesAsync(
        EmployeeServiceApi.DTOs.Common.PaginationRequestDto pagination,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<EmployeeServiceApi.DTOs.Common.PagedResponseDto<CourseAssignmentResponseDto>> GetMyAssignmentsAsync(
        int employeeId,
        EmployeeServiceApi.DTOs.Common.PaginationRequestDto pagination,
        CancellationToken cancellationToken = default);

    Task<AssignmentContentDto> GetAssignmentContentAsync(
        int assignmentId,
        int employeeId,
        CancellationToken cancellationToken = default);
}
