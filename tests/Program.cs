using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.exceptions;
using iTextSharp.text.pdf;

[TestClass]
public class PdfReaderSmokeTests
{
    [TestMethod]
    public void FlateDecode_RejectsOutputAboveLimit()
    {
        using var compressed = new MemoryStream();
        using (var deflater = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            var block = new byte[1024 * 1024];
            for (var i = 0; i < 1024; i++)
                deflater.Write(block, 0, block.Length);
        }

        var exception = Assert.ThrowsExactly<InvalidPdfException>(() => PdfReader.FlateDecode(compressed.ToArray()));
        StringAssert.Contains(exception.Message, "maximum permitted size");
    }

    [TestMethod]
    public void DecodePredictor_RejectsOversizedRow()
    {
        var parameters = new PdfDictionary();
        parameters.Put(PdfName.PREDICTOR, new PdfNumber(15));
        parameters.Put(PdfName.COLUMNS, new PdfNumber(int.MaxValue));

        var exception = Assert.ThrowsExactly<InvalidPdfException>(() => PdfReader.DecodePredictor(Array.Empty<byte>(), parameters));
        StringAssert.Contains(exception.Message, "Predictor row");
    }

    [TestMethod]
    public void ImageGetInstance_RejectsMetadataServiceAddress()
    {
        var exception = Assert.ThrowsExactly<IOException>(() => Image.GetInstance(new Uri("http://169.254.169.254/")));
        StringAssert.Contains(exception.Message, "HTTPS");
    }

    [TestMethod]
    public void ImageGetInstance_RejectsPrivateHttpsAddress()
    {
        var exception = Assert.ThrowsExactly<IOException>(() => Image.GetInstance(new Uri("https://169.254.169.254/")));
        StringAssert.Contains(exception.Message, "non-public");
    }

    [TestMethod]
    public void ImageGetInstance_RejectsFileUri()
    {
        var exception = Assert.ThrowsExactly<IOException>(() => Image.GetInstance(new Uri("file:///etc/passwd")));
        StringAssert.Contains(exception.Message, "HTTPS");
    }

    [TestMethod]
    public void GetStreamBytesRaw_RejectsLengthPastFileEnd()
    {
        using var pdfBytes = new MemoryStream();
        var document = new iTextSharp.text.Document();
        PdfWriter.GetInstance(document, pdfBytes);
        document.Open();
        document.Add(new iTextSharp.text.Paragraph("test"));
        document.Close();

        var pdfData = pdfBytes.ToArray();
        var reader = new PdfReader(pdfData);
        var invalidStream = new PRStream(reader, pdfData.Length - 1);
        invalidStream.Length = 2;
        var rawFile = new RandomAccessFileOrArray(pdfData);
        try
        {
            var exception = Assert.ThrowsExactly<InvalidPdfException>(() => PdfReader.GetStreamBytesRaw(invalidStream, rawFile));
            StringAssert.Contains(exception.Message, "outside the file");
        }
        finally
        {
            rawFile.Close();
        }
    }

    [TestMethod]
    public void ReadPdf_RejectsSelfReferencingPrevPromptly()
    {
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"
        };

        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.Append("xref\n0 4\n0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
            pdf.Append(offsets[i].ToString("D10")).Append(" 00000 n \n");
        pdf.Append("trailer\n<< /Size 4 /Root 1 0 R /Prev ").Append(xrefOffset).Append(" >>\n");
        pdf.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");

        var timer = Stopwatch.StartNew();
        InvalidPdfException exception;
        try
        {
            _ = new PdfReader(Encoding.ASCII.GetBytes(pdf.ToString()));
            Assert.Fail("Expected a repeated xref offset to be rejected.");
            return;
        }
        catch (InvalidPdfException ex)
        {
            exception = ex;
        }
        timer.Stop();
        StringAssert.Contains(exception.Message, "repeated offset");
        Assert.IsLessThan(TimeSpan.FromSeconds(2), timer.Elapsed, "The xref cycle should be rejected promptly.");
    }
}
