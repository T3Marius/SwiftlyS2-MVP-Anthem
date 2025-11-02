using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;

namespace MVP_Anthem;

public class CommandsManager
{
    private readonly ISwiftlyCore _core;
    private readonly PluginConfig _config;
    private readonly MenuManager _menuManager;

    public CommandsManager(ISwiftlyCore core, IOptions<PluginConfig> config, MenuManager MenuManager)
    {
        _core = core;
        _config = config.Value;
        _menuManager = MenuManager;
    }
    public void RegisterCommands()
    {
        foreach (var cmd in _config.Commands.MVPCommands)
        {
            _core.Command.RegisterCommand(cmd, OnMvpCmd);
        }
    }
    public void OnMvpCmd(ICommandContext context)
    {
        if (context.Sender == null)
            return;


        _menuManager.ShowMVP(context.Sender);
    }
}
