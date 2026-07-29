using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.IO.enums;

namespace LearnHub.Helpers
{
    public static class CertificatePdfGenerator
    {
        private static readonly string TemplatePath = Path.Combine(AppContext.BaseDirectory, "Assets", "certificate_template.pdf");

        public static byte[] Generate(string studentName, string courseTitle, string instructorName, DateTime issuedAt)
        {
            using var document = PdfReader.Open(TemplatePath, PdfDocumentOpenMode.Modify);
            var page = document.Pages[0]; // 841.89 x 595.28 pt (A4 landscape), confirmed via /MediaBox
            using var gfx = XGraphics.FromPdfPage(page); // top-down: Y=0 at the top, increasing downward
            var nameFont = new XFont("Arial", 18, XFontStyle.Bold);
            var smallFont = new XFont("Arial", 12, XFontStyle.Regular);

            gfx.DrawString(studentName, nameFont, XBrushes.Black,
                new XRect(0, 205, page.Width, 30), XStringFormats.Center);

            gfx.DrawString(courseTitle, nameFont, XBrushes.Black,
                new XRect(0, 283, page.Width, 30), XStringFormats.Center);

            // Instructor sits above the "INSTRUCTOR" underline (centered on that line, not the whole left half);
            // Date sits above the "DATE ISSUED" underline (centered on that line, not the whole right half).
            gfx.DrawString(instructorName, smallFont, XBrushes.Black,
                new XRect(289 - 90, 505, 180, 20), XStringFormats.Center);

            gfx.DrawString(issuedAt.ToString("MMMM d, yyyy"), smallFont, XBrushes.Black,
                new XRect(549 - 90, 505, 180, 20), XStringFormats.Center);

            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        }
    }
}
