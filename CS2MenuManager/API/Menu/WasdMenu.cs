﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Interface;
using System.Text;
using static CounterStrikeSharp.API.Core.Listeners;
using static CS2MenuManager.API.Class.Buttons;
using static CS2MenuManager.API.Class.ConfigManager;

namespace CS2MenuManager.API.Menu;

/// <summary>
/// Represents a WASD menu with customizable colors and options.
/// </summary>
/// <param name="title">The title of the menu.</param>
/// <param name="plugin">The plugin associated with the menu.</param>
public class WasdMenu(string title, BasePlugin plugin) : BaseMenu(title, plugin)
{
    static WasdMenu()
    {
        LoadConfig();
    }

    /// <summary>
    /// Gets or sets the color of the title.
    /// </summary>
    public string TitleColor { get; set; } = Config.WasdMenu.TitleColor;

    /// <summary>
    /// Gets or sets the color of the scroll up/down buttons.
    /// </summary>
    public string ScrollUpDownKeyColor { get; set; } = Config.WasdMenu.ScrollUpDownKeyColor;

    /// <summary>
    /// Gets or sets the color of the select button.
    /// </summary>
    public string SelectKeyColor { get; set; } = Config.WasdMenu.SelectKeyColor;

    /// <summary>
    /// Gets or sets the color of the prev button.
    /// </summary>
    public string PrevKeyColor { get; set; } = Config.WasdMenu.PrevKeyColor;

    /// <summary>
    /// Gets or sets the color of the exit button.
    /// </summary>
    public string ExitKeyColor { get; set; } = Config.WasdMenu.ExitKeyColor;

    /// <summary>
    /// Gets or sets the color of the selected option.
    /// </summary>
    public string SelectedOptionColor { get; set; } = Config.WasdMenu.SelectedOptionColor;

    /// <summary>
    /// Gets or sets the color of the options.
    /// </summary>
    public string OptionColor { get; set; } = Config.WasdMenu.OptionColor;

    /// <summary>
    /// Gets or sets the color of the disabled options.
    /// </summary>
    public string DisabledOptionColor { get; set; } = Config.WasdMenu.DisabledOptionColor;

    /// <summary>
    /// Gets or sets the color of the arrows.
    /// </summary>
    public string ArrowColor { get; set; } = Config.WasdMenu.ArrowColor;

    /// <summary>
    /// Gets or sets a value indicating whether the player is frozen while the menu is open.
    /// </summary>
    public bool FreezePlayer { get; set; } = Config.WasdMenu.FreezePlayer;

    /// <summary>
    /// The key binding used to scroll up in the menu.
    /// </summary>
    public string ScrollUpKey { get; set; } = Config.Buttons.ScrollUp;

    /// <summary>
    /// The key binding used to scroll down in the menu.
    /// </summary>
    public string ScrollDownKey { get; set; } = Config.Buttons.ScrollDown;

    /// <summary>
    /// The key binding used to select the currently highlighted menu option.
    /// </summary>
    public string SelectKey { get; set; } = Config.Buttons.Select;

    /// <summary>
    /// The key binding used to navigate to the previous page or option in the menu.
    /// </summary>
    public string PrevKey { get; set; } = Config.Buttons.Prev;

    /// <summary>
    /// The key binding used to close the menu.
    /// </summary>
    public string ExitKey { get; set; } = Config.Buttons.Exit;

    /// <summary>
    /// Displays the menu to the specified player for a specified duration.
    /// </summary>
    /// <param name="player">The player to whom the menu is displayed.</param>
    /// <param name="time">The duration for which the menu is displayed.</param>
    public override void Display(CCSPlayerController player, int time)
    {
        MenuTime = time;
        MenuManager.OpenMenu(player, this, null, (p, m) => new WasdMenuInstance(p, m));
    }

    /// <summary>
    /// Displays the menu to the specified player for a specified duration, starting from the given item.
    /// </summary>
    /// <param name="player">The player to whom the menu is displayed.</param>
    /// <param name="firstItem">The index of the first item to display.</param>
    /// <param name="time">The duration for which the menu is displayed.</param>
    public override void DisplayAt(CCSPlayerController player, int firstItem, int time)
    {
        MenuTime = time;
        MenuManager.OpenMenu(player, this, firstItem, (p, m) => new WasdMenuInstance(p, m));
    }
}

/// <summary>
/// Represents an instance of a WASD menu with player-specific data.
/// </summary>
public class WasdMenuInstance : BaseMenuInstance
{
    private readonly Dictionary<string, Action> Buttons = [];

    /// <summary>
    /// Gets the number of items displayed per page.
    /// </summary>
    public override int NumPerPage => 5;

    /// <summary>
    /// Gets or sets the display string for the menu.
    /// </summary>
    public string DisplayString = "";

    /// <summary>
    /// Gets or sets the previous button state.
    /// </summary>
    public PlayerButtons OldButton;

    /// <summary>
    /// Initializes a new instance of the <see cref="WasdMenuInstance"/> class.
    /// </summary>
    /// <param name="player">The player associated with this menu instance.</param>
    /// <param name="menu">The menu associated with this instance.</param>
    public WasdMenuInstance(CCSPlayerController player, IMenu menu) : base(player, menu)
    {
        if (Menu is not WasdMenu wasdMenu)
            return;

        Menu.Plugin.RegisterListener<OnTick>(OnTick);

        if (wasdMenu.FreezePlayer)
            Player.Freeze();

        Buttons = new Dictionary<string, Action>()
        {
            { wasdMenu.ScrollUpKey, ScrollUp },
            { wasdMenu.ScrollDownKey, ScrollDown },
            { wasdMenu.SelectKey, Choose },
            { wasdMenu.PrevKey, PrevSubMenu },
            { wasdMenu.ExitKey, () => { if (Menu.ExitButton) Close(); } }
        };
    }

    /// <summary>
    /// Displays the menu to the player.
    /// </summary>
    public override void Display()
    {
        if (Menu is not WasdMenu wasdMenu) return;

        string leftArrow = $"<font color='{wasdMenu.ArrowColor}'>▶ [</font>";
        string rightArrow = $"<font color='{wasdMenu.ArrowColor}'> ] ◀</font>";

        StringBuilder builder = new();
        int totalPages = (int)Math.Ceiling((double)Menu.ItemOptions.Count / MenuItemsPerPage);
        int currentPage = Page + 1;
        builder.Append($"<font color='{wasdMenu.TitleColor}'>{Menu.Title}</font> ({currentPage}/{totalPages})<br>");

        int keyOffset = 1;
        int maxIndex = Math.Min(CurrentOffset + MenuItemsPerPage, Menu.ItemOptions.Count);
        for (int i = CurrentOffset; i < maxIndex; i++)
        {
            ItemOption option = Menu.ItemOptions[i];
            if (i == CurrentChoiceIndex)
            {
                builder.AppendLine(option.DisableOption switch
                {
                    DisableOption.None =>
                        $"{leftArrow} <font color='{wasdMenu.SelectedOptionColor}'>{option.Text}</font> {rightArrow}<br>",
                    DisableOption.DisableShowNumber or DisableOption.DisableHideNumber =>
                        $"{leftArrow} <font color='{wasdMenu.DisabledOptionColor}'>{option.Text}</font> {rightArrow}<br>",
                    _ => string.Empty
                });
            }
            else
            {
                builder.AppendLine(option.DisableOption switch
                {
                    DisableOption.None =>
                        $"<font color='{wasdMenu.OptionColor}'>{option.Text}</font><br>",
                    DisableOption.DisableShowNumber or DisableOption.DisableHideNumber =>
                        $"<font color='{wasdMenu.DisabledOptionColor}'>{option.Text}</font><br>",
                    _ => string.Empty
                });
            }
            keyOffset++;
        }

        List<string> buttomText = [];
        buttomText.Add($"<font class='fontSize-s' color='{wasdMenu.ScrollUpDownKeyColor}'>{Player.Localizer("ScrollKey", wasdMenu.ScrollUpKey, wasdMenu.ScrollDownKey)}</font>");
        buttomText.Add($"<font class='fontSize-s' color='{wasdMenu.SelectKeyColor}'>{Player.Localizer("SelectKey", wasdMenu.SelectKey)}</font>");

        if (wasdMenu.PrevMenu != null)
            buttomText.Add($"<font class='fontSize-s' color='{wasdMenu.PrevKeyColor}'>{Player.Localizer("PrevKey", wasdMenu.PrevKey)}</font>");

        if (HasExitButton)
            buttomText.Add($"<font class='fontSize-s' color='{wasdMenu.ExitKeyColor}'>{Player.Localizer("ExitKey", wasdMenu.ExitKey)}</font>");

        builder.AppendLine(string.Join(" | ", buttomText));

        DisplayString = builder.ToString();
    }

    /// <summary>
    /// Closes the menu.
    /// </summary>
    public override void Close()
    {
        base.Close();
        Menu.Plugin.RemoveListener<OnTick>(OnTick);
        Player.PrintToCenterHtml(" ");

        if (((WasdMenu)Menu).FreezePlayer)
            Player.Unfreeze();

        if (!string.IsNullOrEmpty(Config.Sound.Exit))
            Player.ExecuteClientCommand($"play {Config.Sound.Exit}");
    }

    /// <summary>
    /// Handles the tick event for the menu.
    /// </summary>
    public void OnTick()
    {
        PlayerButtons button = Player.Buttons;

        foreach (KeyValuePair<string, Action> kvp in Buttons)
        {
            if (ButtonMapping.TryGetValue(kvp.Key, out PlayerButtons mappedBtn))
            {
                if ((button & mappedBtn) == 0 && (OldButton & mappedBtn) != 0)
                {
                    kvp.Value.Invoke();
                    break;
                }
            }
        }

        OldButton = button;

        if (!string.IsNullOrEmpty(DisplayString))
            Player.PrintToCenterHtml(DisplayString);
    }

    /// <summary>
    /// Chooses the currently selected option.
    /// </summary>
    public void Choose()
    {
        if (CurrentChoiceIndex < 0 || CurrentChoiceIndex >= Menu.ItemOptions.Count)
            return;

        ItemOption option = Menu.ItemOptions[CurrentChoiceIndex];

        if (option.DisableOption != DisableOption.None)
        {
            Player.PrintToChat(Player.Localizer("WarnDisabledItem"));
            return;
        }

        if (!string.IsNullOrEmpty(Config.Sound.Select))
            Player.ExecuteClientCommand($"play {Config.Sound.Select}");

        switch (Menu.PostSelectAct)
        {
            case PostSelectAct.Close:
                Close();
                break;
            case PostSelectAct.Reset:
                Close();
                Display();
                break;
            case PostSelectAct.Nothing:
                break;
        }

        option.OnSelect?.Invoke(Player, option);
    }

    /// <summary>
    /// Scrolls down to the next option.
    /// </summary>
    public void ScrollDown()
    {
        int start = CurrentChoiceIndex;
        if (start == Menu.ItemOptions.Count - 1) return;

        CurrentChoiceIndex = (CurrentChoiceIndex + 1) % Menu.ItemOptions.Count;
        if (CurrentChoiceIndex == start) return;

        if (CurrentChoiceIndex >= CurrentOffset + NumPerPage)
            NextPage();
        else
            Display();

        if (!string.IsNullOrEmpty(Config.Sound.ScrollDown))
            Player.ExecuteClientCommand($"play {Config.Sound.ScrollDown}");
    }

    /// <summary>
    /// Scrolls up to the previous option.
    /// </summary>
    public void ScrollUp()
    {
        int start = CurrentChoiceIndex;
        if (start == 0) return;

        CurrentChoiceIndex = (CurrentChoiceIndex - 1 + Menu.ItemOptions.Count) % Menu.ItemOptions.Count;
        if (CurrentChoiceIndex == start) return;

        if (CurrentChoiceIndex < CurrentOffset)
            PrevPage();
        else
            Display();

        if (!string.IsNullOrEmpty(Config.Sound.ScrollUp))
            Player.ExecuteClientCommand($"play {Config.Sound.ScrollUp}");
    }
}
