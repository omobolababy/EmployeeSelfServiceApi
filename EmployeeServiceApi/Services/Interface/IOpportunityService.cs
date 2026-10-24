
using EmployeeSelfServiceApi.DTOs.Opportunities;
using EmployeeServiceApi.DTOs.Common;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public interface IOpportunityService
{
    Task<OpportunityResponseDto> CreateOpportunityAsync(
        CreateOpportunityDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<OpportunityResponseDto> UpdateOpportunityAsync(
        int opportunityId,
        UpdateOpportunityDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<OpportunityResponseDto> PublishOpportunityAsync(
        int opportunityId,
        PublishOpportunityDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<OpportunityResponseDto> GetOpportunityByIdAsync(
        int opportunityId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<OpportunityResponseDto>> GetOpenOpportunitiesAsync(
        PaginationRequestDto pagination,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<JobApplicationResponseDto> ApplyForOpportunityAsync(
        int opportunityId,
        ApplyForOpportunityDto request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<JobApplicationResponseDto> GetApplicationByIdAsync(
        int applicationId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<JobApplicationResponseDto>> GetMyApplicationsAsync(
        int employeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default);

    Task<JobApplicationResponseDto> UpdateApplicationStatusAsync(
        int applicationId,
        UpdateApplicationStatusDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<JobApplicationResponseDto> WithdrawApplicationAsync(
        int applicationId,
        WithdrawApplicationDto request,
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<OpportunityResponseDto> CloseOpportunityAsync(
        int opportunityId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<JobApplicationResponseDto>> GetApplicantsAsync(
        int opportunityId,
        PaginationRequestDto pagination,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);
}
