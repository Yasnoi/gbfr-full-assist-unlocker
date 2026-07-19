using System.Text.Json;

namespace GBFR.InfinityFullAssist.Configuration;

internal sealed class ConfigStore : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _path;
    private readonly FileSystemWatcher _watcher;
    private readonly object _sync = new();

    public Config Current { get; private set; }
    public event Action<Config>? Changed;

    public ConfigStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "Config.json");
        Current = ReadOrCreate();

        _watcher = new FileSystemWatcher(directory, Path.GetFileName(_path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnChanged;
    }

    private Config ReadOrCreate()
    {
        Config config;
        if (File.Exists(_path))
        {
            var json = File.ReadAllBytes(_path);
            config = Config.Deserialize(json);
            if (!ContainsCurrentSettings(json))
            {
                Write(config);
            }
        }
        else
        {
            config = new Config();
            Write(config);
        }

        return config;
    }

    private static bool ContainsCurrentSettings(ReadOnlyMemory<byte> json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return root.TryGetProperty(nameof(Config.EnableAssistMode), out _) &&
               root.TryGetProperty(nameof(Config.EnableOnlineSessions), out _);
    }

    private void Write(Config config) =>
        File.WriteAllText(
            _path,
            JsonSerializer.Serialize(config, SerializerOptions));

    private void OnChanged(object sender, FileSystemEventArgs args)
    {
        _ = sender;
        _ = args;

        lock (_sync)
        {
            try
            {
                var updated = ReadOrCreate();
                Current = updated;
                Changed?.Invoke(updated);
            }
            catch (IOException)
            {
                // The launcher may still be writing. Keep the last known-good config.
            }
            catch (JsonException)
            {
                // Invalid or incomplete user edits must not alter the active behavior.
            }
        }
    }

    public void Dispose() => _watcher.Dispose();
}
