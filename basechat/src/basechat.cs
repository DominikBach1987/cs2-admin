using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using static BaseChat.Library;

namespace BaseChat;

public class BaseChat : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Basic Chat";
    public override string ModuleVersion => "1.8";
    public override string ModuleAuthor => "schwarper";
    public override string ModuleDescription => "Basic Communication Commands";

    public static BaseChat Instance { get; set; } = new BaseChat();
    public Config Config { get; set; } = new Config();
    public FakeConVar<bool> css_chat_mode = new("css_chat_mode", "Allows player's to send messages to admin chat.", true);

    public override void Load(bool hotReload)
    {
        Instance = this;

        AddCommandListener("say_team", Command_SayTeam, HookMode.Pre);
        AddCommandListener("say", OnChatHugCommand, HookMode.Pre); //<<Hier

    }

    public override void Unload(bool hotReload)
    {
        RemoveCommandListener("say_team", Command_SayTeam, HookMode.Pre);
        RemoveCommandListener("say", OnChatHugCommand, HookMode.Pre); //<<Hier
    }

    public void OnConfigParsed(Config config)
    {
        config.Tag = StringExtensions.ReplaceColorTags(config.Tag);
        Config = config;
    }

    [ConsoleCommand("css_say")]
    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, usage: "sends message to all players")]
    public void Command_Say(CCSPlayerController? player, CommandInfo info)
    {
        string adminname = player?.PlayerName ?? Localizer["Console"];
        string message = info.ArgString;

        SendMessageToAllPlayers(HudDestination.Chat, "css_say", adminname, message);
    }

    [ConsoleCommand("css_csay")]
    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, "<message> - sends centered message to all players")]
    public void Command_CSay(CCSPlayerController? player, CommandInfo info)
    {
        string adminname = player?.PlayerName ?? Localizer["Console"];
        string message = info.ArgString;

        SendMessageToAllPlayers(HudDestination.Center, "css_csay", adminname, message);
    }

    [ConsoleCommand("css_hsay")]
    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, "<message>  - sends hud message to all players")]
    public void Command_HSay(CCSPlayerController? player, CommandInfo info)
    {
        string adminname = player?.PlayerName ?? Localizer["Console"];
        string message = info.ArgString;

        SendMessageToAllPlayers(HudDestination.Alert, "css_hsay", adminname, message);
    }

    [ConsoleCommand("css_asay")]
    [ConsoleCommand("css_chat")]
    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, "<message> - sends message to admins")]
    public void Command_ASay(CCSPlayerController? player, CommandInfo info)
    {
        string adminname = player?.PlayerName ?? Localizer["Console"];
        string message = info.ArgString;

        SendMessageToAdmins(adminname, message);
    }

    [ConsoleCommand("css_psay")]
    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 2, usage: "<#userid|name> <message> - sends public message")]
    public void Command_PSay(CCSPlayerController? player, CommandInfo info)
    {
        string[] args = info.ArgString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        TargetResult targetResult = new Target(args[0]).GetTarget(player);

        if (targetResult.Players.Count == 0)
        {
            SendMessageToReplyToCommand(info, true, "No matching client");
            return;
        }
        else if (targetResult.Players.Count > 1)
        {
            SendMessageToReplyToCommand(info, true, "More than one client matched");
            return;
        }

        CCSPlayerController target = targetResult.Players[0];

        string targetname = target.PlayerName;
        string adminname = player?.PlayerName ?? Localizer["Console"];
        string message = string.Join(" ", args[1..]);

        SendMessageToReplyToCommand(info, false, "css_psay", adminname, targetname, message);
        SendMessageToPlayer(target, HudDestination.Chat, "css_psay", adminname, targetname, message);
    }

    public HookResult Command_SayTeam(CCSPlayerController? player, CommandInfo info)
    {
        if (!css_chat_mode.Value)
        {
            return HookResult.Continue;
        }

        string arg = info.ArgString.Trim('"');

        if (!arg.StartsWith('@') || arg.Length <= 1 || arg[1] != ' ')
        {
            return HookResult.Continue;
        }

        string username = player?.PlayerName ?? Localizer["Console"];
        string message = arg[2..].Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            return HookResult.Continue;
        }

        SendMessageToAdmins(username, message);
        return HookResult.Stop;
    }
    // Console command: css_hug
    // Allows an admin to send a hug to a player via console or command line
    [ConsoleCommand("css_hug")]
    [RequiresPermissions("@css/chat")]
    [CommandHelper(minArgs: 1, usage: "hug_usage")]
    public void Command_Hug(CCSPlayerController? player, CommandInfo info)
    {
        string[] args = info.ArgString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Find the target player using input
        TargetResult targetResult = new Target(args[0]).GetTarget(player);

        if (targetResult.Players.Count == 0)
        {
            // No player found
            SendMessageToReplyToCommand(info, true, "hug_no_target");
            return;
        }
        else if (targetResult.Players.Count > 1)
        {
            // More than one player matched
            SendMessageToReplyToCommand(info, true, "hug_too_many");
            return;
        }

        // Send hug message
        CCSPlayerController target = targetResult.Players[0];
        string sender = player?.PlayerName ?? "Console";
        string targetName = target.PlayerName;

        SendMessageToAllPlayers(HudDestination.Chat, "hug_message", sender, targetName);
    }

    // Chat command: /hug
    // Allows a player to hug someone by typing "/hug <name>" in chat
    public HookResult OnChatHugCommand(CCSPlayerController? player, CommandInfo info)
    {
        string arg = info.ArgString.Trim('"');

        // Only continue if the message starts with "/hug "
        if (!arg.StartsWith("/hug "))
        {
            return HookResult.Continue;
        }

        // Get name from input
        string[] args = arg.Substring(5).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (args.Length == 0)
        {
            // No input given after /hug
            SendMessageToPlayer(player, HudDestination.Chat, "hug_usage");
            return HookResult.Stop;
        }

        // Try to find the target player
        TargetResult targetResult = new Target(args[0]).GetTarget(player);

        if (targetResult.Players.Count == 0)
        {
            SendMessageToPlayer(player, HudDestination.Chat, "hug_no_target");
            return HookResult.Stop;
        }
        else if (targetResult.Players.Count > 1)
        {
            SendMessageToPlayer(player, HudDestination.Chat, "hug_too_many");
            return HookResult.Stop;
        }

        // Target is found → send hug message
        CCSPlayerController target = targetResult.Players[0];
        string sender = player?.PlayerName ?? "Console";
        string targetName = target.PlayerName;

        SendMessageToAllPlayers(HudDestination.Chat, "hug_message", sender, targetName);
        return HookResult.Stop;
    }
}