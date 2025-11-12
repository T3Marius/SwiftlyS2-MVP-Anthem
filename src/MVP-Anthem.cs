using AudioApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.Sounds;

namespace MVP_Anthem;

[PluginMetadata(
  Id = "MVP_Anthem",
  Version = "1.0.4",
  Name = "MVP-Anthem",
  Author = "T3Marius",
  Description = "Simple MVP plugin"
)]
public partial class MVP_Anthem : BasePlugin
{
  private ServiceProvider? _provider;
  private DatabaseManager? DatabaseManager;
  private PluginConfig? _config;
  private Library? _libraryManager;
  public static IAudioApi AudioApi = null!;
  public MVP_Anthem(ISwiftlyCore core) : base(core) { }
  public override void UseSharedInterface(IInterfaceManager interfaceManager)
  {
    AudioApi = interfaceManager.GetSharedInterface<IAudioApi>("audio");
  }
  public override void Load(bool hotReload)
  {
    Core.Configuration
        .InitializeJsonWithModel<PluginConfig>("config.jsonc", "Main")
        .Configure(builder =>
        {
          builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: true);
        });

    ServiceCollection services = new();
    services.AddSwiftly(Core).AddSingleton<DatabaseManager>().AddSingleton<CommandsManager>().AddSingleton<MenuManager>().AddSingleton<Library>();
    services.AddOptionsWithValidateOnStart<PluginConfig>().BindConfiguration("Main");

    _provider = services.BuildServiceProvider();

    _config = _provider.GetRequiredService<IOptions<PluginConfig>>().Value;
    var library = _provider.GetRequiredService<Library>();
    _libraryManager = library;

    var db = _provider.GetRequiredService<DatabaseManager>();
    Task.Run(async () =>
    {
      await db.CreateTable();
    });

    DatabaseManager = db;

    var commands = _provider.GetRequiredService<CommandsManager>();
    commands.RegisterCommands();
  }
  public override void Unload()
  {

  }
  [GameEventHandler(HookMode.Post)]
  public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
  {
    IPlayer? player = Core.PlayerManager.GetPlayer(@event.UserId);
    if (player == null || !player.IsValid || player.IsFakeClient || DatabaseManager == null || _config == null)
      return HookResult.Continue;

    var playerMvp = DatabaseManager.GetMvp(player);
    if (playerMvp == null && _config.GiveRandomMVPOnFirstConnect)
    {
      var rnd = new Random();

      var allMvps = new List<(string key, MVP_Settings settings)>();

      foreach (var category in _config.MVPSettings.Values)
      {
        foreach (var (key, settings) in category)
        {
          if (_libraryManager!.ValidateMVP(player, settings))
          {
            allMvps.Add((key, settings));
          }
        }
      }
      if (allMvps.Count > 0)
      {
        var randomMvp = allMvps[rnd.Next(allMvps.Count)];

        Task.Run(async () =>
        {
          await DatabaseManager.SaveMvp(player, randomMvp.settings.MVPName, randomMvp.settings.MVPPath, _config.DefaultVolume);
        });
      }
      else
      {
        Task.Run(async () =>
        {
          await DatabaseManager.SaveMvp(player, null, null, _config.DefaultVolume);
        });
      }

    }
    else if (playerMvp == null)
    {
      Task.Run(async () =>
      {
        await DatabaseManager.SaveMvp(player, null, null, _config.DefaultVolume);
      });
    }
    return HookResult.Continue;
  }
  [GameEventHandler(HookMode.Pre)]
  public HookResult EventRoundMvp(EventRoundMvp @event)
  {
    IPlayer? player = Core.PlayerManager.GetPlayer(@event.UserId);

    if (player == null || !player.IsValid || player.IsFakeClient || DatabaseManager == null)
      return HookResult.Continue;

    Core.Scheduler.NextTick(() =>
    {
      var playerMvp = DatabaseManager.GetMvp(player);
      if (playerMvp == null)
        return;

      var (mvpSettings, mvpKey) = FindMVPSettingsWIthKey(playerMvp.MVPSound);

      if (mvpSettings != null && !string.IsNullOrEmpty(mvpKey))
      {
        _libraryManager?.AnnounceMVP(player, playerMvp, mvpSettings, mvpKey);
      }

      foreach (var otherPlayer in Core.PlayerManager.GetAllPlayers())
      {
        var otherPlayerMvp = DatabaseManager.GetMvp(otherPlayer);

        if (otherPlayerMvp?.Volume > 0)
        {
          Core.Scheduler.NextTick(() =>
          {
            _libraryManager?.PlaySound(player, playerMvp.MVPSound, otherPlayerMvp.Volume);
          });
        }
      }
    });

    return HookResult.Continue;
  }
  private (MVP_Settings?, string) FindMVPSettingsWIthKey(string mvpSound)
  {
    if (_config == null || _config.MVPSettings == null)
      return new(null, string.Empty);

    foreach (var category in _config.MVPSettings.Values)
    {
      foreach (var (key, settings) in category)
      {
        if (settings.MVPPath == mvpSound)
          return (settings, key);
      }
    }

    return new(null, string.Empty);
  }
}
