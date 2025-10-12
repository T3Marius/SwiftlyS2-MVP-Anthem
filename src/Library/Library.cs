using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace MVP_Anthem;

public class Library
{
    private readonly ISwiftlyCore _core;
    private readonly PluginConfig _config;
    public Library(ISwiftlyCore core, IOptions<PluginConfig> config)
    {
        _core = core;
        _config = config.Value;
    }
    public bool ValidateMVP(IPlayer player, MVP_Settings settings)
    {
        if (!string.IsNullOrEmpty(settings.SteamID))
            return player.SteamID.ToString() == settings.SteamID;

        return true;
    }
    public void AnnounceMVP(IPlayer mvpPlayer, PlayerMvp mvpSettings, MVP_Settings configSettings, string mvpKey)
    {
        if (mvpPlayer == null)
            return;


        float htmlTimer = _config.CenterHTMLTimer;
        float centerTimer = _config.CenterTimer;
        float alertTimer = _config.AlertTimer;

        foreach (var player in _core.PlayerManager.GetAllPlayers())
        {
            if (_config.ShakePlayerScreen)
            {
                _core.NetMessage.Send<CUserMessageShake>(msg =>
                {
                    msg.Duration = _config.CenterHTMLTimer;
                    msg.Frequency = 10;
                    msg.Amplitude = 2.5f;

                    msg.Recipients.AddAllPlayers();
                });
            }
            if (configSettings.PrintToChat)
            {
                string prefix = _core.Translation.GetPlayerLocalizer(player)["prefix"];
                string chatKey = $"{mvpKey}.chat";
                string message = _core.Translation.GetPlayerLocalizer(player)[chatKey, mvpPlayer.Controller?.PlayerName!, configSettings.MVPName];
                player.SendMessage(MessageType.Chat, prefix + message);
            }
            if (configSettings.PrintToAlert)
            {
                string alertKey = $"{mvpKey}.alert";
                string message = _core.Translation.GetPlayerLocalizer(player)[alertKey, mvpPlayer.Controller?.PlayerName!, configSettings.MVPName];
                player.SendMessage(MessageType.Alert, message);

                CancellationTokenSource? alertToken = null;
                alertToken = _core.Scheduler.RepeatBySeconds(3, () =>
                {
                    alertTimer--;

                    if (alertTimer > 0)
                        player.SendMessage(MessageType.Alert, message);
                    else
                    {
                        player.SendMessage(MessageType.Alert, "");
                        alertToken?.Cancel();
                    }
                });
            }
            if (configSettings.PrintToCenter)
            {
                string centerKey = $"{mvpKey}.center";
                string message = _core.Translation.GetPlayerLocalizer(player)[centerKey, mvpPlayer.Controller?.PlayerName!, configSettings.MVPName];
                player.SendMessage(MessageType.Center, message);

                CancellationTokenSource? centerToken = null;
                centerToken = _core.Scheduler.RepeatBySeconds(3, () =>
                {
                    centerTimer--;

                    if (centerTimer > 0)
                        player.SendMessage(MessageType.Center, message);
                    else
                    {
                        player.SendMessage(MessageType.Center, "");
                        centerToken?.Cancel();
                    }
                });
            }
            if (configSettings.PrintToHtml)
            {
                string htmlKey = $"{mvpKey}.html";
                string message = _core.Translation.GetPlayerLocalizer(player)[htmlKey, mvpPlayer.Controller?.PlayerName!, configSettings.MVPName];

                CancellationTokenSource? htmlToken = null;
                htmlToken = _core.Scheduler.RepeatBySeconds(3, () =>
                {
                    htmlTimer--;

                    if (htmlTimer > 0)
                        player.SendMessage(MessageType.CenterHTML, message);
                    else
                    {
                        player.SendMessage(MessageType.CenterHTML, "");
                        htmlToken?.Cancel();
                    }
                });
            }
        }
    }
}