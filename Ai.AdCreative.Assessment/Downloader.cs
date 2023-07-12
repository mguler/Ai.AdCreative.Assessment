using System.Net;

namespace Ai.AdCreative.Assessment
{
    public delegate void TaskFinishedEventHandler(object sender, TaskFinishedEventArgs httpResponseMessage);
    public class Downloader
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _syncContext;

        public int RequestCount { get; set; }
        public event TaskFinishedEventHandler TaskFinished;
        public Downloader(SemaphoreSlim syncContext)
        {
            _syncContext = syncContext;
            _httpClient = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                UseCookies = true,
                AllowAutoRedirect = false
            });

        }
        public async Task DownloadFileAsync(string url, string localFileName, CancellationToken cancellationToken = default)
        {
            await _syncContext.WaitAsync();
            await DownloadFile(url, localFileName, cancellationToken);
            _syncContext.Release();
        }

        private async Task DownloadFile(string url, string localFileName, CancellationToken cancellationToken)
        {
            RequestCount++;
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var eventArgs = new TaskFinishedEventArgs(this,response,
            () =>
            {
                var location = response.Headers.Location.ToString();
                DownloadFile(location, localFileName, cancellationToken).Wait();
            }, 
            () =>
            {
                 DownloadFile(url, localFileName, cancellationToken).Wait();
            });

            if (response.StatusCode == HttpStatusCode.OK)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var stream = response.Content.ReadAsStream();
                using (var fileStream = new FileStream(localFileName, FileMode.Create))
                {
                    stream.CopyTo(fileStream);
                    fileStream.Flush();
                }
            }
            TaskFinished?.Invoke(this, eventArgs);
        }
    }
}