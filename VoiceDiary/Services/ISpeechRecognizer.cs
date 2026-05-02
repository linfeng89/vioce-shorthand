namespace VoiceDiary.Services;

public interface ISpeechRecognizer
{
    bool IsReady { get; }
    Task InitializeAsync();
    Task<string?> RecognizeAsync(string audioFilePath);
    void Release();
}
