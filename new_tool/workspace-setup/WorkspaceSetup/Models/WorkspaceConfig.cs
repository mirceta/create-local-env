using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkspaceSetup.Models;

public class WorkspaceConfig
{
    [JsonPropertyName("monitors")]
    public Dictionary<string, MonitorInfo> Monitors { get; set; } = new()
    {
        ["left"] = new MonitorInfo { OffsetX = 0, OffsetY = 0, Width = 2560, Height = 1440 },
        ["right"] = new MonitorInfo { OffsetX = 2560, OffsetY = 0, Width = 2560, Height = 1440 }
    };

    [JsonPropertyName("variables")]
    public Dictionary<string, string> Variables { get; set; } = new();

    [JsonPropertyName("desktops")]
    public List<DesktopConfig> Desktops { get; set; } = [new DesktopConfig { Name = "Desktop 1" }];

    // Backward compat: if old config has top-level "apps", migrate them into desktop 0
    [JsonPropertyName("apps")]
    public List<AppEntry>? LegacyApps { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static WorkspaceConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<WorkspaceConfig>(json, JsonOptions) ?? new WorkspaceConfig();

        // Migrate old flat "apps" array into first desktop
        if (config.LegacyApps is { Count: > 0 })
        {
            if (config.Desktops.Count == 0)
                config.Desktops.Add(new DesktopConfig { Name = "Desktop 1" });

            config.Desktops[0].Apps.AddRange(config.LegacyApps);
            config.LegacyApps = null;
        }

        return config;
    }

    public void Save(string path)
    {
        LegacyApps = null; // never write the old format
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }
}

public class DesktopConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Desktop";

    [JsonPropertyName("apps")]
    public List<AppEntry> Apps { get; set; } = [];

    public override string ToString() => $"{Name} ({Apps.Count} apps)";
}

public class MonitorInfo
{
    [JsonPropertyName("offsetX")]
    public int OffsetX { get; set; }

    [JsonPropertyName("offsetY")]
    public int OffsetY { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    public override string ToString() => $"{Width}x{Height} at ({OffsetX},{OffsetY})";
}

public class AppEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("executable")]
    public string Executable { get; set; } = "";

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "";

    [JsonPropertyName("monitor")]
    public string Monitor { get; set; } = "left";

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; } = 1280;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 1024;

    [JsonPropertyName("delaySeconds")]
    public int DelaySeconds { get; set; } = 3;

    public override string ToString() => $"{Name} ({Executable})";
}
