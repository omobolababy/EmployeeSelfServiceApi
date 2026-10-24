using EmployeeSelfServiceApi.DTOs.Common;
using EmployeeServiceApi.DTOs.Common;

namespace EmployeeSelfServiceApi.Services;

public static class PaginationService
{
    public static PagedResponseDto<T> CreateResponse<T>(
        IEnumerable<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "Page number must be at least 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be between 1 and 100.");
        }

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        return new PagedResponseDto<T>
        {
            Data = items.ToList(),
            Pagination = new PaginationMetadataDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
    }
}