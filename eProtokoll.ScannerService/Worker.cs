using System.Net;
using System.Text;
using System.Text.Json;
using NTwain;
using NTwain.Data;

namespace eProtokoll.ScannerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HttpListener _listener;
        private readonly HttpClient _httpClient;
        private const string ServerUrl = "https://localhost:7263/api/scan/upload";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:7331/");
            _httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _listener.Start();
            _logger.LogInformation("Scanner Service nisur në http://localhost:7331/");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context), stoppingToken);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gabim në listener");
                }
            }

            _listener.Stop();
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            var path = context.Request.Url?.AbsolutePath;
            var method = context.Request.HttpMethod;

            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (method == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                context.Response.Close();
                return;
            }

            if (path == "/scanners" && method == "GET")
            {
                try
                {
                    var scanners = GetAvailableScanners();
                    var json = JsonSerializer.Serialize(scanners);
                    var responseBytes = Encoding.UTF8.GetBytes(json);
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 200;
                    await context.Response.OutputStream.WriteAsync(responseBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gabim duke marrë skanerët");
                    context.Response.StatusCode = 500;
                }
                finally
                {
                    context.Response.Close();
                }
            }
            else if (path == "/scan" && method == "POST")
            {
                _logger.LogInformation("Kërkesë skanimi marrë");

                try
                {
                    string subfolder = context.Request.QueryString["subfolder"] ?? "incoming";
                    string scannerName = context.Request.QueryString["scanner"] ?? "";

                    byte[] imageBytes = TryScanWithNTwain(scannerName) ?? await SimulateScan();
                    var sessionKey = await UploadToServer(imageBytes, subfolder);

                    var responseData = JsonSerializer.Serialize(new { success = true, sessionKey });
                    var responseBytes = Encoding.UTF8.GetBytes(responseData);
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 200;
                    await context.Response.OutputStream.WriteAsync(responseBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gabim gjatë skanimit");
                    context.Response.StatusCode = 500;
                }
                finally
                {
                    context.Response.Close();
                }
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
            }
        }
        private async Task<byte[]> SimulateScan()
        {
            _logger.LogInformation("Scanner fizik nuk u gjet — duke përdorur simulim");
            await Task.Delay(1000);
            return Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
            );
        }
        private List<string> GetAvailableScanners()
        {
            try
            {
                var app = new TwainSession(TWIdentity.CreateFromAssembly(DataGroups.Image,
                    System.Reflection.Assembly.GetExecutingAssembly()));

                var rc = app.Open();
                if (rc != ReturnCode.Success) return new List<string> { "Simulim - Scanner Virtual" };

                var scanners = app.Select(s => s.Name).ToList();
                app.Close();

                if (scanners.Count == 0)
                    return new List<string> { "Simulim - Scanner Virtual" };

                return scanners;
            }
            catch
            {
                return new List<string> { "Simulim - Scanner Virtual" };
            }
        }

        private byte[]? TryScanWithNTwain(string scannerName)
        {
            try
            {
                var app = new TwainSession(TWIdentity.CreateFromAssembly(DataGroups.Image,
                    System.Reflection.Assembly.GetExecutingAssembly()));

                byte[]? scannedBytes = null;

                app.TransferReady += (s, e) => { };
                app.DataTransferred += (s, e) =>
                {
                    if (e.NativeData != IntPtr.Zero)
                    {
                        var size = NativeMethods.GlobalSize(e.NativeData).ToInt32();
                        var ptr = NativeMethods.GlobalLock(e.NativeData);
                        scannedBytes = new byte[size];
                        System.Runtime.InteropServices.Marshal.Copy(ptr, scannedBytes, 0, size);
                        NativeMethods.GlobalUnlock(e.NativeData);
                    }
                };

                var rc = app.Open();
                if (rc != ReturnCode.Success) return null;

                var source = string.IsNullOrEmpty(scannerName)
                    ? app.FirstOrDefault()
                    : app.FirstOrDefault(s => s.Name == scannerName) ?? app.FirstOrDefault();

                if (source == null) { app.Close(); return null; }

                source.Open();
                source.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero);

                app.Close();
                return scannedBytes;
            }
            catch
            {
                return null;
            }
        }


        private async Task<string?> UploadToServer(byte[] imageBytes, string subfolder)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(imageBytes), "file", "scan.png");
            content.Add(new StringContent(subfolder), "subfolder");

            var response = await _httpClient.PostAsync(ServerUrl, content);
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Upload rezultat: {status} {json}", response.StatusCode, json);

            var result = JsonSerializer.Deserialize<ScanUploadResult>(json);
            return result?.sessionKey;
        }
    }

    internal class ScanUploadResult
    {
        public bool success { get; set; }
        public string? sessionKey { get; set; }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool GlobalUnlock(IntPtr hMem);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr GlobalSize(IntPtr hMem);
    }
}