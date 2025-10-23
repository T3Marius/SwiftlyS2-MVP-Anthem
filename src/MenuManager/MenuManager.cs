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
        IMenu menu = _core.Menus.CreateMenu(_core.Translation.GetPlayerLocalizer(player)["mvp_main_menu_title"]);
        menu.RenderColor = new(0, 255, 0);

        menu.ShouldFreeze = _config.FreezePlayerInMenu;
        menu.HasSound = _config.EnableMenuSounds;

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

        menu.Builder.AddText(_core.Translation.GetPlayerLocalizer(player)["mvp_current_volume_option", mvpSettings.Volume]);
        if (!string.IsNullOrEmpty(mvpSettings.MVPName))
        {
            menu.Builder.AddText(_core.Translation.GetPlayerLocalizer(player)["mvp_current_mvp_option", mvpSettings.MVPName]);
            menu.Builder.AddSubmenu(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_mvp_option"], () => ShowRemoveMvpMenu(player, menu, mvpSettings.MVPName));
        }

        menu.Builder.AddSlider(_core.Translation.GetPlayerLocalizer(player)["mvp_change_volume_option"], 0, 100, mvpSettings.Volume, 20, (p, value) =>
        {
            _core.Scheduler.NextTick(async () =>
            {
                await _databaseManager.SaveMvp(p, null, null, (float)value / 100.0f);
            });

            p.SendMessage(MessageType.Chat, _core.Translation.GetPlayerLocalizer(player)["prefix"] + _core.Translation.GetPlayerLocalizer(player)["volume_selected", value]);
        });

        menu.Builder.AddSubmenu(_core.Translation.GetPlayerLocalizer(player)["mvp_select_mvp_option"], () => ShowCategoryMenu(player, menu));

        _core.Menus.OpenMenu(player, menu);
    }
    private IMenu ShowCategoryMenu(IPlayer player, IMenu parentMenu)
    {
        IMenu menu = _core.Menus.CreateMenu(_core.Translation.GetPlayerLocalizer(player)["mvp_categories_title"]);
        menu.Parent = parentMenu;
        menu.RenderColor = new(0, 255, 0);

        menu.ShouldFreeze = _config.FreezePlayerInMenu;
        menu.HasSound = _config.EnableMenuSounds;

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

            menu.Builder.AddSubmenu(categoryName, () => ShowMVPListMenu(player, menu, categoryName, accessibleMVPs));
        }

        return menu;
    }
    private IMenu ShowMVPListMenu(IPlayer player, IMenu parentMenu, string categoryName, List<KeyValuePair<string, MVP_Settings>> mvps)
    {
        IMenu menu = _core.Menus.CreateMenu(categoryName);
        menu.Parent = parentMenu;
        menu.RenderColor = new(0, 255, 0);

        menu.ShouldFreeze = _config.FreezePlayerInMenu;
        menu.HasSound = _config.EnableMenuSounds;

        foreach (var mvpEntry in mvps)
        {
            string mvpKey = mvpEntry.Key;
            MVP_Settings mvpSettings = mvpEntry.Value;

            menu.Builder.AddSubmenu(mvpSettings.MVPName, () => ShowConfirmMenu(player, menu, mvpKey, mvpSettings));
        }

        return menu;
    }
    private IMenu ShowConfirmMenu(IPlayer player, IMenu parentMenu, string mvpKey, MVP_Settings mvpSettings)
    {
        IMenu menu = _core.Menus.CreateMenu(_core.Translation.GetPlayerLocalizer(player)["mvp_confirm_menu_title", mvpSettings.MVPName]);
        menu.Parent = parentMenu;
        menu.RenderColor = new(0, 255, 0);

        menu.ShouldFreeze = _config.FreezePlayerInMenu;
        menu.HasSound = _config.EnableMenuSounds;

        var playerSettings = _databaseManager.GetMvp(player);

        menu.Builder.AddButton(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_yes"], (p) =>
        {
            string mvpName = mvpSettings.MVPName;
            string mvpSound = mvpSettings.MVPSound;

            _core.Scheduler.NextTick(async () =>
            {
                await _databaseManager.SaveMvp(p, mvpName, mvpSound, null);
            });

            p.SendMessage(MessageType.Chat, _core.Translation.GetPlayerLocalizer(player)["prefix"] + _core.Translation.GetPlayerLocalizer(player)["mvp_selected", mvpName]);

            menu.Close(p);
        });

        menu.Builder.AddButton(_core.Translation.GetPlayerLocalizer(player)["mvp_preview_option"], (p) =>
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

        menu.Builder.AddButton(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_no"], (p) =>
        {
            menu.Close(p);
        });

        return menu;
    }
    private IMenu ShowRemoveMvpMenu(IPlayer player, IMenu parentMenu, string mvpName)
    {
        IMenu menu = _core.Menus.CreateMenu(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_mvp_title", mvpName]);
        menu.Parent = parentMenu;
        menu.RenderColor = new(0, 255, 0);

        menu.ShouldFreeze = _config.FreezePlayerInMenu;
        menu.HasSound = _config.EnableMenuSounds;

        menu.Builder.AddButton(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_yes"], (p) =>
        {
            _core.Scheduler.NextTick(async () =>
            {
                await _databaseManager.RemoveMvp(p);
            });

            menu.Close(p);
        });

        menu.Builder.AddButton(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_no"], (p) =>
        {
            menu.Close(p);
        });
        return menu;
    }
}