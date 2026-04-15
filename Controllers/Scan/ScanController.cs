using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.IO.Image;

namespace eProtokoll.Controllers.Scan
{
    [ApiController]
    [Route("api/scan")]
    public class ScanController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<ScanController> _logger;

        public ScanController(IMemoryCache cache, ILogger<ScanController> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string subfolder = "incoming")
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Skedari është bosh" });

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                // Konverto PNG → PDF
                var pdfBytes = ConvertImageToPdf(imageBytes);

                var sessionKey = Guid.NewGuid().ToString();
                _cache.Set(sessionKey, pdfBytes, TimeSpan.FromHours(2));

                _logger.LogInformation("Skanim u ruajt në cache: {key}", sessionKey);
                return Ok(new { success = true, sessionKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gabim gjatë ngarkimit të skanimit");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private byte[] ConvertImageToPdf(byte[] imageBytes)
        {
            using var pdfStream = new MemoryStream();
            var writer = new PdfWriter(pdfStream);
            var pdf = new PdfDocument(writer);
            var document = new iText.Layout.Document(pdf);

            var imageData = ImageDataFactory.Create(imageBytes);
            var image = new iText.Layout.Element.Image(imageData);
            image.SetAutoScale(true);

            document.Add(image);
            document.Close();

            return pdfStream.ToArray();
        }

        [HttpDelete("cancel")]
        public IActionResult Cancel([FromQuery] string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _cache.Remove(key);
                _logger.LogInformation("Cache u fshi: {key}", key);
            }
            return Ok();
        }
    }
}