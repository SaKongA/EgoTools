public class KeyboardSettings
{
    public bool KeyboardDetachment { get; set; }
}
public class ColorManagement
{
    public string CurrentMode { get; set; } = default!;
    public string CurrentProfile { get; set; } = default!;
    public string IgcFile { get; set; } = default!;
    public string _3dlutFile { get; set; } = default!;
}
public class PowerThreshold
{
    public int ChargeLimit { get; set; }
}
public class AppearanceSettings
{
    public string Theme { get; set; } = "Default"; // Default, Light, Dark
}
public class AppConfig
{
    public KeyboardSettings KeyboardSettings { get; set; } = default!;
    public ColorManagement ColorManagement { get; set; } = default!;
    public PowerThreshold PowerThreshold { get; set; } = default!;
    public AppearanceSettings AppearanceSettings { get; set; } = default!;
} 