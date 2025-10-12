using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;

namespace MVP_Anthem;

public class MenuManager
{
    private readonly ISwiftlyCore _core;
    private readonly PluginConfig _config;
    private readonly DatabaseManager _databaseManager;
    private readonly Library _libraryManager;
    public MenuManager(ISwiftlyCore core, IOptions<PluginConfig> config, DatabaseManager DatabaseManager, Library LibraryManager)
    {
        _core = core;
        _config = config.Value;
        _databaseManager = DatabaseManager;
        _libraryManager = LibraryManager;
    }
    public void ShowMVP(IPlayer player)
    {
        IMenu menu = _core.Menus.CreateMenu(_core.Translation.GetPlayerLocalizer(player)["mvp_main_menu_title"], _config.FreezePlayerInMenu, _config.EnableMenuSounds, true);
        menu.Color = new(0, 255, 0);

        var mvpSettings = _databaseManager.GetMvp(player);
        float defaultVolume = _config.DefaultVolume;

        if (mvpSettings == null)
        {
            Task.Run(async () =>
            {
                await _databaseManager.LoadMvp(player, defaultVolume);
            });
            return;
        }

        menu.AddOption(_core.Translation.GetPlayerLocalizer(player)["mvp_current_volume_option", mvpSettings.Volume], (p, o, m) => { }, true);
        if (!string.IsNullOrEmpty(mvpSettings.MVPName))
        {
            menu.AddOption(_core.Translation.GetPlayerLocalizer(player)["mvp_current_mvp_option", mvpSettings.MVPName], (p, o, m) => { }, true);
            menu.AddOption(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_mvp_option"], (p, o, m) =>
            {
                ShowRemoveMvpMenu(p, menu, mvpSettings.MVPName);
            });
        }

        List<object> volumeValues = new List<object> { 0f, 20f, 40f, 60f, 80f, 100f };
        menu.AddSliderOption(_core.Translation.GetPlayerLocalizer(player)["mvp_change_volume_option"], volumeValues, mvpSettings.Volume, 3, (p, o, m, index, value) =>
        {
            _core.Scheduler.NextTick(async () =>
            {
                await _databaseManager.SaveMvp(p, null, null, (float)value / 100.0f);
            });

            p.SendMessage(MessageType.Chat, _core.Translation.GetPlayerLocalizer(player)["prefix"] + _core.Translation.GetPlayerLocalizer(player)["volume_selected", value]);
        });

        menu.AddOption(_core.Translation.GetPlayerLocalizer(player)["mvp_select_mvp_option"], (p, o, m) =>
        {
            ShowCategoryMenu(p, menu);
        });

        _core.Menus.OpenMenu(player, menu);
    }
    private void ShowCategoryMenu(IPlayer player, IMenu parentMenu)
    {
        IMenu menu = _core.Menus.CreateMenu(_core.Translation.GetPlayerLocalizer(player)["mvp_categories_title"], _config.FreezePlayerInMenu, _config.EnableMenuSounds, true);
        menu.ParentMenu = parentMenu;
        menu.Color = new(0, 255, 0);

        Dictionary<string, List<KeyValuePair<string, MVP_Settings>>> accessibleMVPsByCategory = new();

        foreach (var category in _config.MVPSettings)
        {
            var accessibleMvps = new List<KeyValuePair<string, MVP_Settings>>();

            foreach (var mvpEntry in category.Value)
            {
                if (_libraryManager.ValidateMVP(player, mvpEntry.Value))
                    accessibleMvps.Add(mvpEntry);
            }

            if (accessibleMvps.Count > 0)
                accessibleMVPsByCategory[category.Key] = accessibleMvps;
        }

        foreach (var categoryEntry in accessibleMVPsByCategory)
        {
            string categoryName = categoryEntry.Key;
            var accessibleMVPs = categoryEntry.Value;

            menu.AddOption(categoryName, (p, o, m) =>
            {
                ShowMVPListMenu(p, m, categoryName, accessibleMVPs);
            });
        }

        _core.Menus.OpenSubMenu(player, menu);
    }
    private void ShowMVPListMenu(IPlayer player, IMenu parentMenu, string categoryName, List<KeyValuePair<string, MVP_Settings>> mvps)
    {
        IMenu menu = _core.Menus.CreateMenu(categoryName, _config.FreezePlayerInMenu, _config.EnableMenuSounds, true);
        menu.ParentMenu = parentMenu;
        menu.Color = new(0, 255, 0);

        foreach (var mvpEntry in mvps)
        {
            string mvpKey = mvpEntry.Key;
            MVP_Settings mvpSettings = mvpEntry.Value;

            menu.AddOption(mvpSettings.MVPName, (p, o, m) =>
            {
                ShowConfirmMenu(p, m, mvpKey, mvpSettings);
            });
        }

        _core.Menus.OpenSubMenu(player, menu);
    }
    private void ShowConfirmMenu(IPlayer player, IMenu parentMenu, string mvpKey, MVP_Settings mvpSettings)
    {
        IMenu menu = _core.Menus.CreateMenu(_core.Translation.GetPlayerLocalizer(player)["mvp_confirm_menu_title", mvpSettings.MVPName], _config.FreezePlayerInMenu, _config.EnableMenuSounds, true);
        menu.ParentMenu = parentMenu;
        menu.Color = new(0, 255, 0);

        var playerSettings = _databaseManager.GetMvp(player);

        menu.AddOption(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_yes"], (p, o, m) =>
        {
            string mvpName = mvpSettings.MVPName;
            string mvpSound = mvpSettings.MVPSound;

            _core.Scheduler.NextTick(async () =>
            {
                await _databaseManager.SaveMvp(p, mvpName, mvpSound, null);
            });

            p.SendMessage(MessageType.Chat, _core.Translation.GetPlayerLocalizer(player)["prefix"] + _core.Translation.GetPlayerLocalizer(player)["mvp_selected", mvpName]);

            _core.Menus.CloseMenu(p);
        });

        menu.AddOption(_core.Translation.GetPlayerLocalizer(player)["mvp_preview_option"], (p, o, m) =>
        {
            if (playerSettings == null)
                return;

            var soundEvent = new SoundEvent()
            {
                Name = mvpSettings.MVPSound,
                Volume = playerSettings.Volume
            };

            if (playerSettings.Volume > 0)
            {
                soundEvent.Recipients.AddRecipient(p.PlayerID);
                soundEvent.SourceEntityIndex = -1;
                soundEvent.Emit();
                soundEvent.Recipients.RemoveRecipient(p.PlayerID);

                p.SendMessage(MessageType.Chat, _core.Translation.GetPlayerLocalizer(player)["prefix"] + _core.Translation.GetPlayerLocalizer(player)["mvp_preview", mvpSettings.MVPName]);
            }
            else
                p.SendMessage(MessageType.Chat, _core.Translation.GetPlayerLocalizer(player)["prefix"] + _core.Translation.GetPlayerLocalizer(player)["mvp_preview_0_volume"]);
        });

        menu.AddOption(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_no"], (p, o, m) =>
        {
            ShowMVP(p);
        });

        _core.Menus.OpenSubMenu(player, menu);
    }
    private void ShowRemoveMvpMenu(IPlayer player, IMenu parentMenu, string mvpName)
    {
        IMenu menu = _core.Menus.CreateMenu(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_mvp_title", mvpName], _config.FreezePlayerInMenu, _config.EnableMenuSounds, true);
        menu.ParentMenu = parentMenu;
        menu.Color = new(0, 255, 0);

        menu.AddOption(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_yes"], (p, o, m) =>
        {
            _core.Scheduler.NextTick(async () =>
            {
                await _databaseManager.RemoveMvp(p);
            });

            _core.Menus.CloseMenu(p);
        });

        menu.AddOption(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_no"], (p, o, m) =>
        {
            _core.Menus.CloseMenu(p);
        });

        _core.Menus.OpenSubMenu(player, menu);
    }
}