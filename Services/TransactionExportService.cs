using System.Globalization;
using CsvHelper;
using dotnetweb.DTOs;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace dotnetweb.Services;

public class TransactionExportService
{
    public byte[] ExportToCsv(List<TransactionDto> transactions)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        {
            csv.WriteRecords(transactions);
            writer.Flush();
            return memoryStream.ToArray();
        }
    }

    public byte[] ExportToPdf(List<TransactionDto> transactions, string userName)
    {
        using var memoryStream = new MemoryStream();
        var document = new Document(PageSize.A4);
        var writer = PdfWriter.GetInstance(document, memoryStream);
        document.Open();

        // Add title
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
        document.Add(new Paragraph("Transaction History", titleFont));
        document.Add(new Paragraph($"User: {userName}", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
        document.Add(new Paragraph($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
        document.Add(new Paragraph(" ")); // spacing

        // Create table
        var table = new PdfPTable(6);
        table.WidthPercentage = 100;
        table.SetWidths(new float[] { 1f, 1.5f, 1.5f, 2f, 1.5f, 1.5f });

        // Add headers
        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
        var headers = new[] { "ID", "Date", "Type", "Description", "Amount", "Category" };
        foreach (var header in headers)
        {
            table.AddCell(new PdfPCell(new Phrase(header, headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
        }

        // Add rows
        var regularFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
        foreach (var transaction in transactions)
        {
            table.AddCell(new PdfPCell(new Phrase(transaction.Id.ToString(), regularFont)));
            table.AddCell(new PdfPCell(new Phrase(transaction.Date.ToString("yyyy-MM-dd HH:mm"), regularFont)));
            table.AddCell(new PdfPCell(new Phrase(transaction.Type, regularFont)));
            table.AddCell(new PdfPCell(new Phrase(transaction.Description, regularFont)));
            table.AddCell(new PdfPCell(new Phrase(transaction.Amount.ToString("C"), regularFont)));
            table.AddCell(new PdfPCell(new Phrase(transaction.Category, regularFont)));
        }

        document.Add(table);

        // Add summary
        document.Add(new Paragraph(" "));
        var totalCredit = transactions.Where(t => t.Type == "Credit").Sum(t => t.Amount);
        var totalDebit = transactions.Where(t => t.Type == "Debit").Sum(t => t.Amount);
        var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
        document.Add(new Paragraph($"Total Credit: {totalCredit:C}", summaryFont));
        document.Add(new Paragraph($"Total Debit: {totalDebit:C}", summaryFont));

        document.Close();
        return memoryStream.ToArray();
    }
}
