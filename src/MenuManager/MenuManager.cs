using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Core.Menus.OptionsBase;
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

    private void ApplyMenuTitle(IMenuDesignAPI builder, string title)
    {
        if (_config.GradientTitleColor)
        {
            string startColor = $"#{_config.MenuColor[0]:X2}{_config.MenuColor[1]:X2}{_config.MenuColor[2]:X2}";
            string endColor = "#FFFFFF";
            var gradientTitle = HtmlGradient.GenerateGradientText(title, startColor, endColor);
            builder.SetMenuTitle(gradientTitle);
        }
        else
        {
            builder.SetMenuTitle(title);
        }
    }

    public void ShowMVP(IPlayer player)
    {
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

        var builder = _core.MenusAPI.CreateBuilder()
            .SetPlayerFrozen(_config.FreezePlayerInMenu);

        if (_config.EnableMenuSounds)
            builder.EnableSound();

        ApplyMenuTitle(builder.Design, _core.Translation.GetPlayerLocalizer(player)["mvp_main_menu_title"]);

        builder.AddOption(new TextMenuOption(_core.Translation.GetPlayerLocalizer(player)["mvp_current_volume_option", mvpSettings.Volume]));

        if (!string.IsNullOrEmpty(mvpSettings.MVPName))
        {
            builder.AddOption(new TextMenuOption(_core.Translation.GetPlayerLocalizer(player)["mvp_current_mvp_option", mvpSettings.MVPName]));
            builder.AddOption(new SubmenuMenuOption(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_mvp_option"], () => ShowRemoveMvpMenu(player, mvpSettings.MVPName)));
        }

        var volumeSlider = new SliderMenuOption(
            text: _core.Translation.GetPlayerLocalizer(player)["mvp_change_volume_option"],
            min: 0f,
            max: 100f,
            defaultValue: mvpSettings.Volume,
            step: 20f
        );

        volumeSlider.ValueChanged += (sender, args) =>
        {
            _core.Scheduler.NextTick(async () =>
            {
                await _databaseManager.SaveMvp(args.Player, null, null, (float)args.NewValue / 100.0f);
            });

            args.Player.SendMessage(MessageType.Chat, _core.Translation.GetPlayerLocalizer(player)["prefix"] + _core.Translation.GetPlayerLocalizer(player)["volume_selected", args.NewValue]);
        };

        builder.AddOption(volumeSlider);
        builder.AddOption(new SubmenuMenuOption(_core.Translation.GetPlayerLocalizer(player)["mvp_select_mvp_option"], () => ShowCategoryMenu(player)));

        var menu = builder.Build();
        _core.MenusAPI.OpenMenuForPlayer(player, menu);
    }
    private IMenuAPI ShowCategoryMenu(IPlayer player)
    {
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

        var builder = _core.MenusAPI.CreateBuilder()
            .SetPlayerFrozen(_config.FreezePlayerInMenu);

        if (_config.EnableMenuSounds)
            builder.EnableSound();

        ApplyMenuTitle(builder.Design, _core.Translation.GetPlayerLocalizer(player)["mvp_categories_title"]);

        foreach (var categoryEntry in accessibleMVPsByCategory)
        {
            string categoryName = categoryEntry.Key;
            var accessibleMVPs = categoryEntry.Value;

            builder.AddOption(new SubmenuMenuOption(categoryName, () => ShowMVPListMenu(player, categoryName, accessibleMVPs)));
        }

        return builder.Build();
    }
    private IMenuAPI ShowMVPListMenu(IPlayer player, string categoryName, List<KeyValuePair<string, MVP_Settings>> mvps)
    {
        var builder = _core.MenusAPI.CreateBuilder()
            .SetPlayerFrozen(_config.FreezePlayerInMenu);

        if (_config.EnableMenuSounds)
            builder.EnableSound();

        ApplyMenuTitle(builder.Design, categoryName);

        foreach (var mvpEntry in mvps)
        {
            string mvpKey = mvpEntry.Key;
            MVP_Settings mvpSettings = mvpEntry.Value;

            builder.AddOption(new SubmenuMenuOption(mvpSettings.MVPName, () => ShowConfirmMenu(player, mvpKey, mvpSettings)));
        }

        return builder.Build();
    }
    private IMenuAPI ShowConfirmMenu(IPlayer player, string mvpKey, MVP_Settings mvpSettings)
    {
        var playerSettings = _databaseManager.GetMvp(player);

        var builder = _core.MenusAPI.CreateBuilder()
            .SetPlayerFrozen(_config.FreezePlayerInMenu);

        if (_config.EnableMenuSounds)
            builder.EnableSound();

        ApplyMenuTitle(builder.Design, _core.Translation.GetPlayerLocalizer(player)["mvp_confirm_menu_title", mvpSettings.MVPName]);

        var confirmButton = new ButtonMenuOption(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_yes"]);
        confirmButton.Click += async (sender, args) =>
        {
            string mvpName = mvpSettings.MVPName;
            string mvpSound = mvpSettings.MVPPath;

            _core.Scheduler.NextTick(async () =>
            {
                await _databaseManager.SaveMvp(args.Player, mvpName, mvpSound, null);
            });

            args.Player.SendMessage(MessageType.Chat, _core.Translation.GetPlayerLocalizer(player)["prefix"] + _core.Translation.GetPlayerLocalizer(player)["mvp_selected", mvpName]);

            args.CloseMenu = true;
        };
        builder.AddOption(confirmButton);

        var previewButton = new ButtonMenuOption(_core.Translation.GetPlayerLocalizer(player)["mvp_preview_option"]);
        previewButton.Click += async (sender, args) =>
        {
            if (playerSettings == null)
                return;

            if (playerSettings.Volume > 0)
            {
                _libraryManager.PlaySound(player, mvpSettings.MVPPath, playerSettings.Volume);

                args.Player.SendMessage(MessageType.Chat, _core.Translation.GetPlayerLocalizer(player)["prefix"] + _core.Translation.GetPlayerLocalizer(player)["mvp_preview", mvpSettings.MVPName]);
            }
            else
                args.Player.SendMessage(MessageType.Chat, _core.Translation.GetPlayerLocalizer(player)["prefix"] + _core.Translation.GetPlayerLocalizer(player)["mvp_preview_0_volume"]);
        };
        builder.AddOption(previewButton);

        var cancelButton = new ButtonMenuOption(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_no"]);
        cancelButton.Click += async (sender, args) =>
        {
            _core.MenusAPI.CloseMenuForPlayer(player, builder.Build());
        };
        builder.AddOption(cancelButton);

        return builder.Build();
    }
    private IMenuAPI ShowRemoveMvpMenu(IPlayer player, string mvpName)
    {
        var builder = _core.MenusAPI.CreateBuilder()
            .SetPlayerFrozen(_config.FreezePlayerInMenu);

        if (_config.EnableMenuSounds)
            builder.EnableSound();

        ApplyMenuTitle(builder.Design, _core.Translation.GetPlayerLocalizer(player)["mvp_remove_mvp_title", mvpName]);

        var yesButton = new ButtonMenuOption(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_yes"]);
        yesButton.Click += async (sender, args) =>
        {
            _core.Scheduler.NextTick(async () =>
            {
                await _databaseManager.RemoveMvp(args.Player);
            });

            args.CloseMenu = true;
        };
        builder.AddOption(yesButton);

        var noButton = new ButtonMenuOption(_core.Translation.GetPlayerLocalizer(player)["mvp_remove_no"]);
        noButton.Click += async (sender, args) =>
        {
            _core.MenusAPI.CloseMenuForPlayer(player, builder.Build());
        };
        builder.AddOption(noButton);

        return builder.Build();
    }
}
