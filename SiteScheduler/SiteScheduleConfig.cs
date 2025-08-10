using System.Text.Json;

namespace SiteScheduler;

public class SiteScheduleConfig
{
    public string SiteName { get; set; } = string.Empty;
    public string SiteAddress { get; set; } = string.Empty;
    public int Period { get; set; } // in seconds
}

public class SchedulerConfigLoader
{
    public static List<SiteScheduleConfig> Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<SiteScheduleConfig>>(json) ?? new List<SiteScheduleConfig>();
    }
}
