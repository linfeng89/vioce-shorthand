namespace VoiceDiary.Services;

public class ModelDownloader
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task<bool> DownloadModelAsync(string destinationPath, IProgress<double>? progress = null)
    {
        try
        {
            var modelUrl = "https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-base.en.tar.bz2";
            var tempFile = Path.Combine(Path.GetTempPath(), "sherpa-model.tar.bz2");

            await DownloadFileAsync(modelUrl, tempFile, progress);

            await ExtractModelAsync(tempFile, destinationPath);

            File.Delete(tempFile);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"模型下载失败：{ex.Message}");
            return false;
        }
    }

    private static async Task DownloadFileAsync(string url, string destination, IProgress<double>? progress)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var downloadedBytes = 0;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write);

        var buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            downloadedBytes += bytesRead;

            if (totalBytes > 0 && progress != null)
            {
                var percent = (double)downloadedBytes / totalBytes * 100;
                progress.Report(percent);
            }
        }
    }

    private static Task ExtractModelAsync(string archivePath, string destinationPath)
    {
        // TODO: 实现 tar.bz2 解压
        // 目前需要手动下载并解压到指定目录
        throw new NotImplementedException("需要手动下载模型文件");
    }
}
