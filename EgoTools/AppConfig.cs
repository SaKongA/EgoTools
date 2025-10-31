using System;
using System.IO;

public class KeyboardSettings
{
    public bool WirelessFunc { get; set; }

    public int KeyboardBattery { get; set; }
}

public class ColorManagement
{
    public string CurrentMode { get; set; } = default!;
    public string CurrentProfile { get; set; } = default!;
    public string IgcFile { get; set; } = default!;
    public string _3dlutFile { get; set; } = default!;
}
public class BatteryControl
{
    public int ChargeLimit { get; set; }

    public bool AutoCharge { get; set; }
}

public class ConfigPath
{
    private static string AppDataRoot { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string AppConfigDirectory { get; } = Path.Combine(AppDataRoot, "EGOBox");
    public static string MainConfigFilePath { get; } = Path.Combine(AppConfigDirectory, "AppConfig.json");
    public static void EnsureConfigDirectoryExists()
    {
        Directory.CreateDirectory(AppConfigDirectory);
    }
}

public class AppConfig
{
    public KeyboardSettings KeyboardSettings { get; set; } = new();
    public ColorManagement ColorManagement { get; set; } = new();
    public BatteryControl BatteryControl { get; set; } = new();
} 