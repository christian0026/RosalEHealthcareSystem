using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RosalEHealthcare.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using SysDrawing = System.Drawing;
using System.IO;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class MedicineExportService
    {
        public MedicineExportService()
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            // Set QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        #region Excel Export

        /// <summary>
        /// Export medicines to Excel
        /// </summary>
        public string ExportToExcel(List<Medicine> medicines, string filePath = null)
        {
            if (filePath == null)
            {
                string fileName = $"MedicineInventory_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Medicine Inventory");

                // Header styling
                var headerRange = worksheet.Cells[1, 1, 1, 12];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(SysDrawing.Color.FromArgb(46, 125, 50));
                headerRange.Style.Font.Color.SetColor(SysDrawing.Color.White);
                headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Row(1).Height = 30;

                // Headers
                worksheet.Cells[1, 1].Value = "Medicine ID";
                worksheet.Cells[1, 2].Value = "Medicine Name";
                worksheet.Cells[1, 3].Value = "Generic Name";
                worksheet.Cells[1, 4].Value = "Brand";
                worksheet.Cells[1, 5].Value = "Category";
                worksheet.Cells[1, 6].Value = "Type";
                worksheet.Cells[1, 7].Value = "Strength";
                worksheet.Cells[1, 8].Value = "Stock";
                worksheet.Cells[1, 9].Value = "Min. Stock";
                worksheet.Cells[1, 10].Value = "Price";
                worksheet.Cells[1, 11].Value = "Expiry Date";
                worksheet.Cells[1, 12].Value = "Status";

                // Data
                int row = 2;
                foreach (var med in medicines)
                {
                    worksheet.Cells[row, 1].Value = med.MedicineId;
                    worksheet.Cells[row, 2].Value = med.Name;
                    worksheet.Cells[row, 3].Value = med.GenericName;
                    worksheet.Cells[row, 4].Value = med.Brand;
                    worksheet.Cells[row, 5].Value = med.Category;
                    worksheet.Cells[row, 6].Value = med.Type;
                    worksheet.Cells[row, 7].Value = med.Strength;
                    worksheet.Cells[row, 8].Value = med.Stock;
                    worksheet.Cells[row, 9].Value = med.MinimumStockLevel;
                    worksheet.Cells[row, 10].Value = med.Price;
                    worksheet.Cells[row, 10].Style.Numberformat.Format = "₱#,##0.00";
                    worksheet.Cells[row, 11].Value = med.ExpiryDate.ToString("MMM yyyy");
                    worksheet.Cells[row, 12].Value = med.Status;

                    // Status color coding
                    var statusCell = worksheet.Cells[row, 12];
                    switch (med.Status)
                    {
                        case "Available":
                            statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            statusCell.Style.Fill.BackgroundColor.SetColor(SysDrawing.Color.FromArgb(232, 245, 233));
                            statusCell.Style.Font.Color.SetColor(SysDrawing.Color.FromArgb(46, 125, 50));
                            break;
                        case "Low Stock":
                            statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            statusCell.Style.Fill.BackgroundColor.SetColor(SysDrawing.Color.FromArgb(255, 243, 224));
                            statusCell.Style.Font.Color.SetColor(SysDrawing.Color.FromArgb(245, 124, 0));
                            break;
                        case "Out of Stock":
                        case "Expired":
                            statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            statusCell.Style.Fill.BackgroundColor.SetColor(SysDrawing.Color.FromArgb(255, 235, 238));
                            statusCell.Style.Font.Color.SetColor(SysDrawing.Color.FromArgb(211, 47, 47));
                            break;
                    }

                    row++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Add borders
                var dataRange = worksheet.Cells[1, 1, row - 1, 12];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                // Save file
                FileInfo fileInfo = new FileInfo(filePath);
                package.SaveAs(fileInfo);
            }

            return filePath;
        }

        /// <summary>
        /// Create Excel template for bulk import
        /// </summary>
        public string CreateImportTemplate(string filePath = null)
        {
            if (filePath == null)
            {
                string fileName = "MedicineImportTemplate.xlsx";
                filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Medicine Import Template");

                // Instructions
                worksheet.Cells[1, 1].Value = "MEDICINE IMPORT TEMPLATE";
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.Font.Size = 14;

                worksheet.Cells[2, 1].Value = "Instructions:";
                worksheet.Cells[3, 1].Value = "1. Fill in all required fields (marked with *)";
                worksheet.Cells[4, 1].Value = "2. Do not modify column headers";
                worksheet.Cells[5, 1].Value = "3. Use valid dates in format: MM/DD/YYYY";
                worksheet.Cells[6, 1].Value = "4. Stock and Price must be numeric values";
                worksheet.Cells[7, 1].Value = "5. Save and upload this file";

                // Headers (row 9)
                int headerRow = 9;
                worksheet.Cells[headerRow, 1].Value = "Medicine Name *";
                worksheet.Cells[headerRow, 2].Value = "Generic Name";
                worksheet.Cells[headerRow, 3].Value = "Brand";
                worksheet.Cells[headerRow, 4].Value = "Category *";
                worksheet.Cells[headerRow, 5].Value = "Type *";
                worksheet.Cells[headerRow, 6].Value = "Strength";
                worksheet.Cells[headerRow, 7].Value = "Unit";
                worksheet.Cells[headerRow, 8].Value = "Stock *";
                worksheet.Cells[headerRow, 9].Value = "Min Stock *";
                worksheet.Cells[headerRow, 10].Value = "Price *";
                worksheet.Cells[headerRow, 11].Value = "Expiry Date *";
                worksheet.Cells[headerRow, 12].Value = "Notes";

                // Style headers
                var headerRange = worksheet.Cells[headerRow, 1, headerRow, 12];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(SysDrawing.Color.FromArgb(46, 125, 50));
                headerRange.Style.Font.Color.SetColor(SysDrawing.Color.White);
                headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Row(headerRow).Height = 25;

                // Sample data
                int sampleRow = headerRow + 1;
                worksheet.Cells[sampleRow, 1].Value = "Amoxicillin 500mg";
                worksheet.Cells[sampleRow, 2].Value = "Amoxicillin";
                worksheet.Cells[sampleRow, 3].Value = "Generic Brand";
                worksheet.Cells[sampleRow, 4].Value = "Antibiotic";
                worksheet.Cells[sampleRow, 5].Value = "Capsules";
                worksheet.Cells[sampleRow, 6].Value = "500mg";
                worksheet.Cells[sampleRow, 7].Value = "Capsule";
                worksheet.Cells[sampleRow, 8].Value = 100;
                worksheet.Cells[sampleRow, 9].Value = 20;
                worksheet.Cells[sampleRow, 10].Value = 15.00;
                worksheet.Cells[sampleRow, 11].Value = DateTime.Now.AddYears(2).ToString("MM/dd/yyyy");
                worksheet.Cells[sampleRow, 12].Value = "Sample medicine entry";

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Save
                FileInfo fileInfo = new FileInfo(filePath);
                package.SaveAs(fileInfo);
            }

            return filePath;
        }

        #endregion

        #region PDF Export

        /// <summary>
        /// Export medicines to PDF
        /// </summary>
        public string ExportToPdf(List<Medicine> medicines, string generatedBy, string filePath = null)
        {
            if (filePath == null)
            {
                string fileName = $"MedicineInventory_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            }

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(40);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header()
                        .Height(80)
                        .Background(QuestPDF.Helpers.Colors.Green.Darken2)
                        .Padding(15)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("ROSAL EHEALTHCARE SYSTEM")
                                    .FontSize(20)
                                    .FontColor(QuestPDF.Helpers.Colors.White)
                                    .Bold();
                                col.Item().Text("Medicine Inventory Report")
                                    .FontSize(14)
                                    .FontColor(QuestPDF.Helpers.Colors.White);
                            });

                            row.ConstantItem(150).Column(col =>
                            {
                                col.Item().AlignRight().Text($"Generated: {DateTime.Now:MMM dd, yyyy}")
                                    .FontSize(9)
                                    .FontColor(QuestPDF.Helpers.Colors.White);
                                col.Item().AlignRight().Text($"By: {generatedBy}")
                                    .FontSize(9)
                                    .FontColor(QuestPDF.Helpers.Colors.White);
                                col.Item().AlignRight().Text($"Total: {medicines.Count} medicines")
                                    .FontSize(9)
                                    .FontColor(QuestPDF.Helpers.Colors.White);
                            });
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);  // ID
                                columns.RelativeColumn(2);    // Name
                                columns.RelativeColumn(1.5f); // Category
                                columns.ConstantColumn(50);  // Stock
                                columns.ConstantColumn(60);  // Price
                                columns.ConstantColumn(70);  // Expiry
                                columns.ConstantColumn(80);  // Status
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text("Medicine ID").FontSize(9).Bold();
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text("Medicine Name").FontSize(9).Bold();
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text("Category").FontSize(9).Bold();
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text("Stock").FontSize(9).Bold();
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text("Price").FontSize(9).Bold();
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text("Expiry Date").FontSize(9).Bold();
                                header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text("Status").FontSize(9).Bold();
                            });

                            // Data rows
                            foreach (var med in medicines)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text(med.MedicineId ?? "N/A").FontSize(8);

                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text(med.Name ?? "Unknown").FontSize(8);

                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text(med.Category ?? "N/A").FontSize(8);

                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text(med.Stock.ToString()).FontSize(8)
                                    .FontColor(med.Stock == 0 ? QuestPDF.Helpers.Colors.Red.Medium : QuestPDF.Helpers.Colors.Black);

                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text($"₱{med.Price:N2}").FontSize(8);

                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text(med.ExpiryDate.ToString("MMM yyyy")).FontSize(8);

                                // --- FIXED: Replaced C# 8.0 Switch Expression with C# 7.3 Standard Switch ---
                                string statusColor;
                                switch (med.Status)
                                {
                                    case "Available":
                                        statusColor = QuestPDF.Helpers.Colors.Green.Medium;
                                        break;
                                    case "Low Stock":
                                        statusColor = QuestPDF.Helpers.Colors.Orange.Medium;
                                        break;
                                    default:
                                        statusColor = QuestPDF.Helpers.Colors.Red.Medium;
                                        break;
                                }
                                // --------------------------------------------------------------------------

                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(5).Text(med.Status ?? "Unknown").FontSize(8)
                                    .FontColor(statusColor).Bold();
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            })
            .GeneratePdf(filePath);

            return filePath;
        }

        #endregion
    }
}