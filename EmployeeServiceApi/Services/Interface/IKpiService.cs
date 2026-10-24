using EmployeeSelfServiceApi.DTOs.KPI;
using EmployeeServiceApi.DTOs.Common;
using EmployeeServiceApi.DTOs.KPI;
using EmployeeServiceApi.DTOs.KPIDto;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public interface IKpiService
{
    // HR
    Task<KpiPlanResponseDto> CreatePlanAsync(CreateKpiPlanDto request, int actorEmployeeId, CancellationToken cancellationToken = default);
    Task<KpiPlanResponseDto> UpdatePlanAsync(int planId, UpdateKpiPlanDto request, int actorEmployeeId, CancellationToken cancellationToken = default);
    Task<KpiPlanResponseDto> ActivatePlanAsync(int planId, ActivateKpiPlanDto request, int actorEmployeeId, CancellationToken cancellationToken = default);
    Task<KpiPlanResponseDto> ReopenPlanAsync(int planId, ReopenKpiPlanDto request, int actorEmployeeId, CancellationToken cancellationToken = default);
    Task<KpiReviewPeriodResponseDto> CreateReviewPeriodAsync(CreateReviewPeriodDto request, int actorEmployeeId, CancellationToken cancellationToken = default);
    Task<PagedResponseDto<KpiPlanResponseDto>> GetAllPlansAsync(PaginationRequestDto pagination, int actorEmployeeId, CancellationToken cancellationToken = default);

    // Employee
    Task<KpiPlanResponseDto> UpdateProgressAsync(int planId, UpdateKpiProgressDto request, int employeeId, CancellationToken cancellationToken = default);
    Task<KpiPlanResponseDto> SubmitPlanAsync(int planId, int employeeId, CancellationToken cancellationToken = default);
    Task<PagedResponseDto<KpiPlanResponseDto>> GetMyPlansAsync(int employeeId, PaginationRequestDto pagination, CancellationToken cancellationToken = default);

    // Reviewer
    Task<KpiPlanResponseDto> ReviewPlanAsync(int planId, ReviewKpiPlanDto request, int reviewerEmployeeId, CancellationToken cancellationToken = default);
    Task<PagedResponseDto<KpiPlanResponseDto>> GetReviewQueueAsync(int reviewerEmployeeId, PaginationRequestDto pagination, CancellationToken cancellationToken = default);

    // Shared
    Task<KpiPlanResponseDto> GetPlanAsync(int planId, int actorEmployeeId, CancellationToken cancellationToken = default);
}
