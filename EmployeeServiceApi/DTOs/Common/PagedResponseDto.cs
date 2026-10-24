namespace EmployeeServiceApi.DTOs.Common;

public class PagedResponseDto<T>
{
    public List<T> Data { get; set; } = new();

    public PaginationMetadataDto Pagination { get; set; } = new();
}

