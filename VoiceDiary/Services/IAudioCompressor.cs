namespace VoiceDiary.Services;

public interface IAudioCompressor
{
    Task<(string m4aPath, bool success)> CompressToM4aAsync(string wavPath);
    Task<bool> ValidateM4aAsync(string m4aPath);
}
