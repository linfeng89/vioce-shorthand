namespace VoiceDiary.Services;

public class AudioCompressor : IAudioCompressor
{
    public Task<(string m4aPath, bool success)> CompressToM4aAsync(string wavPath)
    {
        return Task.Run(async () =>
        {
            try
            {
                var m4aPath = Path.ChangeExtension(wavPath, ".m4a");
                await CompressAudioPlatform(wavPath, m4aPath);
                return (m4aPath, true);
            }
            catch
            {
                return (string.Empty, false);
            }
        });
    }

    public Task<bool> ValidateM4aAsync(string m4aPath)
    {
        try
        {
            if (!File.Exists(m4aPath))
                return Task.FromResult(false);

            var fileInfo = new FileInfo(m4aPath);
            if (fileInfo.Length == 0)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private async Task CompressAudioPlatform(string wavPath, string m4aPath)
    {
        await Task.CompletedTask;
    }
}
