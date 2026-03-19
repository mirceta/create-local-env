using System.Text.Json;

namespace PromptManager;

public class AgentLabel
{
    public string Name { get; set; } = "";
}

public class Prompt
{
    public string Text { get; set; } = "";
    public string? SentToAgent { get; set; }
    public DateTime? SentAt { get; set; }
}

public class TaskItem
{
    public string Name { get; set; } = "";
    public string RepoPath { get; set; } = "";
    public List<Prompt> Prompts { get; set; } = new();
    public List<AgentLabel> Agents { get; set; } = new();
}

public class AppState
{
    public List<TaskItem> Tasks { get; set; } = new();
}

public static class Persistence
{
    private static string FilePath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "promptmanager_data.json");

    public static AppState Load()
    {
        if (!File.Exists(FilePath)) return new AppState();
        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public static void Save(AppState state)
    {
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
