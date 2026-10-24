
using EmployeeSelfServiceApi.DTOs.Common;
using EmployeeSelfServiceApi.DTOs.Training;
using EmployeeServiceApi.DTOs.Common;
using EmployeeServiceApi.DTOs.Training;
using EmployeeServiceApi.DTOs.TrainingDto;
using EmployeeServiceApi.Entities.LMS;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public interface ITrainingService
{
    Task<TrainingRequestResponseDto> CreateRequestAsync(
        CreateTrainingRequestDto request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<TrainingRequestResponseDto> UpdateRequestAsync(
        int requestId,
        UpdateTrainingRequestDto request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<TrainingRequestResponseDto> SubmitRequestAsync(
        int requestId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<TrainingRequestResponseDto> GetRequestByIdAsync(
        int requestId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<TrainingRequestResponseDto>> GetMyRequestsAsync(
        int employeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default);

    Task<TrainingRequestResponseDto> DecideAsync(
        int requestId,
        TrainingDecisionDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task WithdrawRequestAsync(
        int requestId,
        WithdrawTrainingRequestDto request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<EstacodeRateResponseDto> CreateEstacodeRateAsync(
        CreateEstacodeRateDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<EstacodeRateResponseDto> UpdateEstacodeRateAsync(
        int rateId,
        UpdateEstacodeRateDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<TrainingAllowanceVersionDto> CorrectAllowanceAsync(
        int requestId,
        CorrectTrainingAllowanceDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);
    
    
    Task<TrainingRequestResponseDto> ReassignReviewerAsync(
        int requestId,
        ReassignTrainingReviewerDto request,
        int adminEmployeeId,
        CancellationToken cancellationToken = default);

    Task<TrainingCompletionEvidence> SubmitCompletionEvidenceAsync(
        int requestId,
        int attachmentId,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<TrainingRequestResponseDto> ReviewCompletionEvidenceAsync(
        int requestId,
        int evidenceId,
        ReviewTrainingEvidenceDto review,
        int hrEmployeeId,
        CancellationToken cancellationToken = default);

    Task<TrainingRequestResponseDto> CloseWithoutEvidenceAsync(
        int requestId,
        string reason,
        int hrEmployeeId,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<TrainingRequestResponseDto>> GetManagerQueueAsync(
        int actorEmployeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<TrainingRequestResponseDto>> GetHrQueueAsync(
        int actorEmployeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<TrainingRequestResponseDto>> GetFinanceQueueAsync(
        int actorEmployeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<TrainingRequestResponseDto>> GetAllPendingManagerAsync(
        int actorEmployeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default);

    Task<List<TrainingAllowanceVersionDto>> GetAllowanceVersionsAsync(
        int requestId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<(byte[] Content, string FileName)> GenerateAllowancePdfAsync(
        int requestId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);
}
