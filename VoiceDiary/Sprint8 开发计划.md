# Sprint 8 开发计划

**Sprint**: 云备份与完善  
**状态**: 🔄 开发中  
**日期**: 2026-05-03  
**开发者**: linfeng89

---

## 开发目标

实现完整的数据备份功能，支持本地 ZIP 备份和云存储集成，完善导出设置。

---

## 开发内容

### P0 核心功能（必须完成）

| 任务 | 优先级 | 预计工时 | 状态 | 说明 |
|------|--------|----------|------|------|
| ZIP 备份服务 | P0 | 3h | ⏳ | 压缩全部数据 |
| ZIP 恢复服务 | P0 | 3h | ⏳ | 从 ZIP 恢复 |
| 备份设置页 | P0 | 2h | ⏳ | 备份配置 UI |
| 自动备份计划 | P0 | 2h | ⏳ | 定期备份 |

### P1 增强功能

| 任务 | 优先级 | 预计工时 | 状态 | 说明 |
|------|--------|----------|------|------|
| iCloud 集成 | P1 | 3h | ⏳ | iOS 云备份 |
| OneDrive 集成 | P1 | 3h | ⏳ | 微软云备份 |
| 导出设置 | P1 | 1h | ⏳ | 默认格式配置 |

---

## 技术设计

### 1. ZIP 备份服务

```csharp
public class BackupService : IBackupService
{
    private readonly IDatabaseService _databaseService;
    private readonly IStorageService _storageService;
    
    public async Task<string> CreateFullBackupAsync()
    {
        var backupPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Backups",
            $"VoiceDiary_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
        
        using var zip = ZipOutputStream.Create(backupPath);
        
        // 1. 导出数据库到 JSON
        var entries = await _databaseService.GetAllEntriesAsync();
        var dbJson = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        zip.AddEntry("database.json", dbJson);
        
        // 2. 添加音频文件
        foreach (var entry in entries)
        {
            if (File.Exists(entry.AudioFilePath))
            {
                var audioPath = $"audio/{Path.GetFileName(entry.AudioFilePath)}";
                zip.AddFile(entry.AudioFilePath, audioPath);
            }
        }
        
        // 3. 添加应用设置
        var settings = await GetAppSettingsAsync();
        var settingsJson = JsonSerializer.Serialize(settings);
        zip.AddEntry("settings.json", settingsJson);
        
        return backupPath;
    }
}
```

### 2. ZIP 恢复服务

```csharp
public async Task<bool> RestoreFromBackupAsync(string zipFilePath)
{
    try
    {
        using var zip = ZipInputStream.Open(zipFilePath);
        
        // 1. 恢复数据库
        var dbJson = zip.ReadEntryText("database.json");
        var entries = JsonSerializer.Deserialize<List<DiaryEntry>>(dbJson);
        
        foreach (var entry in entries)
        {
            await _databaseService.InsertEntryAsync(entry);
        }
        
        // 2. 恢复音频文件
        foreach (var entry in entries)
        {
            var audioEntry = zip.GetEntry($"audio/{Path.GetFileName(entry.AudioFilePath)}");
            if (audioEntry != null)
            {
                var audioPath = await _storageService.GetAudioFilePathAsync(entry.AudioFileName);
                audioEntry.Extract(audioPath);
            }
        }
        
        // 3. 恢复设置
        var settingsJson = zip.ReadEntryText("settings.json");
        var settings = JsonSerializer.Deserialize<AppSettings>(settingsJson);
        await SaveAppSettingsAsync(settings);
        
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Restore failed: {ex.Message}");
        return false;
    }
}
```

### 3.  iCloud 集成 (iOS)

```csharp
public class iCloudBackupService : ICloudBackupService
{
    private readonly string _ubiquityContainer = "iCloud.com.voicediary.app";
    
    public async Task<bool> UploadToCloudAsync(string backupFilePath)
    {
        var ubiquityPath = NSFileManager.UrlForUbiquityContainer(_ubiquityContainer);
        if (ubiquityPath == null)
            throw new InvalidOperationException("iCloud not available");
        
        var cloudPath = Path.Combine(ubiquityPath.Path, "Backups", Path.GetFileName(backupFilePath));
        
        try
        {
            var nsUrl = NSUrl.FromFilename(cloudPath);
            var fileManager = NSFileManager.DefaultManager;
            
            if (fileManager.FileExists(nsUrl.Path))
                await Task.Run(() => fileManager.Remove(nsUrl));
            
            var localUrl = NSUrl.FromFilename(backupFilePath);
            var result = fileManager.SetUbiquitous(localUrl, nsUrl, out var error);
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"iCloud upload failed: {ex.Message}");
            return false;
        }
    }
    
    public async Task<List<string>> ListCloudBackupsAsync()
    {
        var ubiquityPath = NSFileManager.UrlForUbiquityContainer(_ubiquityContainer);
        // 列出备份文件...
        return new List<string>();
    }
}
```

### 4. OneDrive 集成

```csharp
public class OneDriveBackupService : ICloudBackupService
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _driveId = "me";
    
    public OneDriveBackupService(string accessToken)
    {
        var authProvider = new DelegateAuthenticationProvider(
            requestMessage =>
            {
                requestMessage.Headers.Authorization = 
                    new AuthenticationHeaderValue("Bearer", accessToken);
                return Task.CompletedTask;
            });
        
        _graphClient = new GraphServiceClient(authProvider);
    }
    
    public async Task<bool> UploadToCloudAsync(string backupFilePath)
    {
        try
        {
            using var stream = File.OpenRead(backupFilePath);
            var fileName = Path.GetFileName(backupFilePath);
            
            await _graphClient.Drive[_driveId]
                .Root
                .ItemByPath($"/VoiceDiary/Backups/{fileName}")
                .Content
                .Request()
                .PutAsync<DriveItem>(stream);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OneDrive upload failed: {ex.Message}");
            return false;
        }
    }
}
```

### 5. 自动备份计划

```csharp
public class AutomaticBackupService
{
    private readonly IBackupService _backupService;
    private readonly IPreferences _preferences;
    private Timer? _backupTimer;
    
    public void StartAutomaticBackup()
    {
        var autoBackupEnabled = _preferences.Get("AutoBackupEnabled", false);
        var backupInterval = _preferences.Get("BackupIntervalDays", 7);
        
        if (!autoBackupEnabled)
            return;
        
        _backupTimer = new Timer(
            async _ => await PerformAutomaticBackupAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromDays(backupInterval));
    }
    
    private async Task PerformAutomaticBackupAsync()
    {
        try
        {
            var backupPath = await _backupService.CreateFullBackupAsync();
            
            // 上传到云存储
            var cloudService = GetConfiguredCloudService();
            await cloudService?.UploadToCloudAsync(backupPath);
            
            // 清理旧备份（保留最近 3 个）
            await CleanupOldBackupsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Automatic backup failed: {ex.Message}");
        }
    }
}
```

---

## 验收标准

### ZIP 备份
- [ ] 创建完整备份（数据库 + 音频）
- [ ] 压缩率 > 50%
- [ ] 备份文件命名规范
- [ ] 存储空间检查

### ZIP 恢复
- [ ] 从备份完整恢复
- [ ] 冲突处理（覆盖/跳过）
- [ ] 恢复进度显示
- [ ] 恢复后验证

### 云备份
- [ ] iCloud 上传下载
- [ ] OneDrive 上传下载
- [ ] 云备份列表
- [ ] 选择恢复版本

### 自动备份
- [ ] 定时触发
- [ ] 后台执行
- [ ] 失败重试
- [ ] 通知提醒

---

## 依赖项

```
备份服务
├── ZIP 压缩/解压
├── 数据库导出/导入
├── 音频文件打包
│
云存储
├── iCloud (iOS)
└── OneDrive (跨平台)

自动备份
└── 定时器
    └── 配置检查
    └── 清理策略
```

---

## 风险评估

| 风险 | 影响 | 概率 | 应对 |
|------|------|------|------|
| iCloud 不可用 | 高 | 低 | 降级到本地备份 |
| OneDrive API 变更 | 中 | 低 | 使用稳定 SDK |
| 大备份文件失败 | 中 | 中 | 分片上传 |
| 数据冲突 | 高 | 低 | 版本号管理 |

---

## 交付物

- ✅ BackupService.cs (ZIP 备份)
- ✅ iCloudBackupService.cs (iOS)
- ✅ OneDriveBackupService.cs (跨平台)
- ✅ AutomaticBackupService.cs
- ✅ BackupSettingsPage.xaml

---

**文档版本**: v1.0  
**创建时间**: 2026-05-03  
**最后更新**: 2026-05-03
