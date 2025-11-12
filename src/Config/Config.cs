namespace MVP_Anthem;

public class PluginConfig
{
    public bool ShakePlayerScreen { get; set; } = true;
    public bool GiveRandomMVPOnFirstConnect { get; set; } = true;
    public bool FreezePlayerInMenu { get; set; } = true;
    public bool EnableMenuSounds { get; set; } = true;
    public float DefaultVolume { get; set; } = 0.2f;
    public float CenterHTMLTimer { get; set; } = 10.0f;
    public float CenterTimer { get; set; } = 10.0f;
    public float AlertTimer { get; set; } = 10.0f;
    public List<int> MenuColor { get; set; } = [255, 0, 0];
    public bool GradientTitleColor { get; set; } = true;
    public Commands_Config Commands { get; set; } = new();
    public Dictionary<string, Dictionary<string, MVP_Settings>> MVPSettings { get; set; } = new()
    {
        {
            "PUBLIC MVP", new Dictionary<string, MVP_Settings>
            {
                {
                    "mvp.1", new MVP_Settings
                    {
                        MVPName = "Flawless",
                        MVPPath = "mvp_sounds/flawless.mp3",
                        EnablePreview = true,
                        PrintToAlert = false,
                        PrintToCenter = false,
                        PrintToChat = true,
                        PrintToHtml = true
                    }
                },
                {
                    "mvp.2", new MVP_Settings
                    {
                        MVPName = "Protection Charm",
                        MVPPath = "mvp_sounds/protectioncharm.mp3",
                        EnablePreview = true,
                        PrintToAlert = false,
                        PrintToCenter = false,
                        PrintToChat = true,
                        PrintToHtml = true,
                        SteamID = "76561199478674655",
                    }
                }
            }
        }
    };
}
public class Commands_Config
{
    public List<string> MVPCommands { get; set; } = ["mvp", "music"];
    public List<string> VolumeCommands { get; set; } = ["mvpvol", "vol"];
}

public class MVP_Settings
{
    public string MVPName { get; set; } = string.Empty;
    public string MVPPath { get; set; } = string.Empty;
    public bool EnablePreview { get; set; } = true;
    public bool PrintToAlert { get; set; } = false;
    public bool PrintToCenter { get; set; } = false;
    public bool PrintToHtml { get; set; } = true;
    public bool PrintToChat { get; set; } = true;
    public string SteamID { get; set; } = string.Empty;
    public List<string> Flags { get; set; } = new List<string>();
}
