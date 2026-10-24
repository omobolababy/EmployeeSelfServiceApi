
using EmployeeSelfServiceApi.DTOs.Payroll;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EmployeeSelfServiceApi.Services.Implementation;

public class PayslipPdfService : IPayslipPdfService
{
    private readonly ILogger<PayslipPdfService> _logger;

    public PayslipPdfService(ILogger<PayslipPdfService> logger)
    {
        _logger = logger;
    }

    public byte[] Generate(
        PayslipResponseDto payslip,
        PayslipVersionResponseDto version)
    {
        try
        {
            if (version.Status != PayslipStatus.Published &&
                version.Status != PayslipStatus.Superseded)
            {
                throw new InvalidOperationException(
                    "Only published payslip versions can be downloaded.");
            }

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Column(column =>
                        {
                            column.Item()
                                .Text("EMPLOYEE PAYSLIP")
                                .FontSize(22)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            column.Item()
                                .Text("Confidential payroll statement")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);

                            column.Item()
                                .PaddingTop(10)
                                .LineHorizontal(1)
                                .LineColor(Colors.Grey.Lighten1);
                        });

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            column.Spacing(12);

                            column.Item().Text("EMPLOYEE DETAILS")
                                .Bold().FontSize(12);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Element(CellStyle)
                                    .Text($"Employee: {payslip.EmployeeName}");

                                table.Cell().Element(CellStyle)
                                    .Text($"Employee Number: {payslip.EmployeeNumber}");

                                table.Cell().Element(CellStyle)
                                    .Text($"Payroll Month: {payslip.PayrollMonth}");

                                table.Cell().Element(CellStyle)
                                    .Text($"Payroll Year: {payslip.PayrollYear}");

                                table.Cell().Element(CellStyle)
                                    .Text($"Payslip Version: {version.VersionNumber}");

                                table.Cell().Element(CellStyle)
                                    .Text($"Currency: {version.Currency}");
                            });

                            column.Item().PaddingTop(8)
                                .Text("EARNINGS AND DEDUCTIONS")
                                .Bold().FontSize(12);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderStyle)
                                        .Text("Description");

                                    header.Cell().Element(HeaderStyle)
                                        .Text("Category");

                                    header.Cell().Element(HeaderStyle)
                                        .AlignRight().Text("Amount (NGN)");
                                });

                                foreach (var item in version.Items)
                                {
                                    table.Cell().Element(CellStyle)
                                        .Text(item.Name);

                                    table.Cell().Element(CellStyle)
                                        .Text(item.Type.ToString());

                                    table.Cell().Element(CellStyle)
                                        .AlignRight()
                                        .Text(item.Amount.ToString("N2"));
                                }
                            });

                            column.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                });

                                AddTotalRow(table, "Basic Salary",
                                    version.BasicSalary);

                                AddTotalRow(table, "Gross Pay",
                                    version.GrossPay);

                                AddTotalRow(table, "Total Deductions",
                                    version.TotalDeductions);

                                table.Cell().ColumnSpan(2)
                                    .PaddingVertical(4)
                                    .LineHorizontal(1)
                                    .LineColor(Colors.Grey.Medium);

                                table.Cell().Element(TotalStyle)
                                    .Text("NET PAY").Bold().FontSize(13);

                                table.Cell().Element(TotalStyle)
                                    .AlignRight()
                                    .Text($"NGN {version.NetPay:N2}")
                                    .Bold().FontSize(13)
                                    .FontColor(Colors.Blue.Darken2);
                            });

                            column.Item().PaddingTop(15)
                                .Text($"Status: {version.Status}");

                            if (!string.IsNullOrWhiteSpace(
                                    version.RevisionReason))
                            {
                                column.Item().Text(
                                    $"Revision reason: {version.RevisionReason}");
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("This is a system-generated payslip.")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate PDF for payslip {PayslipId}, version {VersionId}.",
                payslip.Id, version.Id);

            throw;
        }
    }

    private static IContainer CellStyle(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(6)
            .PaddingHorizontal(4);

    private static IContainer HeaderStyle(IContainer container) =>
        container
            .Background(Colors.Grey.Lighten3)
            .Padding(6)
            .DefaultTextStyle(x => x.Bold());

    private static IContainer TotalStyle(IContainer container) =>
        container.PaddingVertical(6).PaddingHorizontal(4);

    private static void AddTotalRow(
        TableDescriptor table,
        string label,
        decimal amount)
    {
        table.Cell().Element(TotalStyle).Text(label);

        table.Cell().Element(TotalStyle)
            .AlignRight().Text($"NGN {amount:N2}");
    }
}
