using Ai.AdCreative.Assessment;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

var url = "https://picsum.photos/200/300?random=1";
var downloaded = 0;

#region Configuration
var jsonText = File.ReadAllText("config.json");
var config = JsonSerializer.Deserialize<Dictionary<string,object>>(jsonText);
var count = config.ContainsKey("Count") ? int.Parse(config["Count"].ToString()) : 0;
var parallelism  = config.ContainsKey("Parallelism") ? int.Parse(config["Parallelism"].ToString()) : 0;
var path = config.ContainsKey("SavePath") ? config["SavePath"].ToString() : "";
var maxAttempts = 3;

if (count == 0)
{
    Console.Write("Enter the number of images to download : ");
    count = int.Parse(Console.ReadLine());
}

if (parallelism == 0)
{
    Console.Write("Enter the maximum parallel download limit : ");
    parallelism = int.Parse(Console.ReadLine());
}

if (string.IsNullOrEmpty(path))
{
    Console.Write("Enter the save path (default: ./outputs) : ");
    path = Console.ReadLine();
}
#endregion End Of Configuration

#region Synchronization And Thread Management
var cancellationTokenSource = new CancellationTokenSource();
var semaphoreSlim = new SemaphoreSlim(parallelism, parallelism);
var syncLock = new object();
#endregion End Of Synchronization And Thread Management

#region Prepare Output Folder and Handle Cancellation
if (!Directory.Exists(path))
{
    Directory.CreateDirectory(path);
}

Console.CancelKeyPress += (sender, args) => {
    cancellationTokenSource.Cancel();
    Directory.Delete(path, true);
    Environment.Exit(0);
};
#endregion End Of Prepare Output Folder and Handle Cancellation

Console.WriteLine($"Downloading {count} images({parallelism} parallel downloads at most)");
Console.Write($"Progress: {downloaded}/{count}");

var hashList = new ConcurrentBag<string>();
var tasks2BeCompleted = new List<Task>();

for (var index = 0; index < count; index++)
{
    var downloader = new Downloader(semaphoreSlim);
    downloader.TaskFinished += (sender, args) =>
    {
        if (args.HttpResponseMessage.StatusCode == HttpStatusCode.OK)
        {
            lock (syncLock)
            {
                downloaded++;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"Progress: {downloaded}/{count}");
            }
        }
        else if (args.HttpResponseMessage.StatusCode == HttpStatusCode.Found)
        {
            var location = args.HttpResponseMessage.Headers.Location.ToString();
            var hmac = Regex.Match(location, "(?<=hmac=)(.*?)$").Value;
            if (!hashList.Contains(hmac))
            {
                hashList.Add(hmac);
                args.ProceedRedirection();
            }
            else
            {
                if (args.Instance.RequestCount < maxAttempts)
                {
                    args.ReloadUrl();
                }
            }
        }
        else
        {
            if (args.Instance.RequestCount < maxAttempts)
            {
                args.ReloadUrl();
            }
        }
    };
    var task = downloader.DownloadFileAsync(url, $"{path}/{index + 1}.jpg", cancellationTokenSource.Token);
    tasks2BeCompleted.Add(task);
}
Task.WaitAll(tasks2BeCompleted.ToArray());
