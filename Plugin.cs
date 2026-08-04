using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Shell;

namespace OwOTranslator;

public sealed unsafe class Plugin : IDalamudPlugin
{
    public string Name => "OwO Translator";

    private const string CommandName = "/owo";
    private const string ShortCommandName = "/o";
    private const int MaxMessageLength = 480;
    private unsafe delegate void ExecuteCommandInnerDelegate(ShellCommandModule* thisPtr, Utf8String* command, UIModule* uiModule);
    private Hook<ExecuteCommandInnerDelegate>? executeCommandInnerHook;

    private static readonly Random Rng = new();

    private static readonly Dictionary<string, string> ChannelPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["s"] = "/say",
        ["say"] = "/say",
        ["p"] = "/p",
        ["party"] = "/p",
        ["fc"] = "/fc",
        ["y"] = "/yell",
        ["yell"] = "/yell",
        ["sh"] = "/shout",
        ["shout"] = "/shout",
    };

    private static readonly Dictionary<ChatChannel, string> DefaultChannelSlash = new()
    {
        [ChatChannel.Say] = "/say",
        [ChatChannel.Party] = "/p",
        [ChatChannel.FreeCompany] = "/fc",
        [ChatChannel.Yell] = "/yell",
        [ChatChannel.Shout] = "/shout",
    };

    private static readonly Dictionary<string, (string Canonical, int TargetTokens)> KnownChannelCommands
        = BuildKnownChannelCommands();

    private static Dictionary<string, (string, int)> BuildKnownChannelCommands()
    {
        var dict = new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
        {
            ["say"] = ("/say", 0),
            ["s"] = ("/say", 0),
            ["yell"] = ("/yell", 0),
            ["y"] = ("/yell", 0),
            ["shout"] = ("/shout", 0),
            ["sh"] = ("/shout", 0),
            ["party"] = ("/p", 0),
            ["p"] = ("/p", 0),
            ["alliance"] = ("/alliance", 0),
            ["a"] = ("/alliance", 0),
            ["freecompany"] = ("/fc", 0),
            ["fc"] = ("/fc", 0),
            ["novice"] = ("/novice", 0),
            ["n"] = ("/novice", 0),
            ["pvpteam"] = ("/pvpteam", 0),
            ["pt"] = ("/pvpteam", 0),
            ["tell"] = ("/tell", 2),
            ["t"] = ("/tell", 2),
        };

        for (var i = 1; i <= 8; i++)
        {
            dict[$"linkshell{i}"] = ($"/linkshell{i}", 0);
            dict[$"l{i}"] = ($"/linkshell{i}", 0);
            dict[$"cwlinkshell{i}"] = ($"/cwlinkshell{i}", 0);
            dict[$"cwl{i}"] = ($"/cwlinkshell{i}", 0);
        }

        return dict;
    }

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private readonly Configuration config;
    private readonly WindowSystem windowSystem = new("OwOTranslator");
    private readonly ConfigWindow configWindow;

    private DateTime nextAutoSendAt = DateTime.MinValue;

    public Plugin()
    {
        ECommonsMain.Init(PluginInterface, this);

        this.config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.config.Initialize(PluginInterface);

        this.configWindow = new ConfigWindow(this.config, this.ResetAutoSendSchedule);
        this.windowSystem.AddWindow(this.configWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Sends your message in owo/nyan style. Ex: '/owo hey guys', " +
                          "'/owo p let's go do the duty', '/owo on' / '/owo off' to toggle."
        });
        if (!CommandManager.Commands.ContainsKey(ShortCommandName))
        {
            CommandManager.AddHandler(ShortCommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Shortcut for /owo.",
                ShowInHelp = false,
            });
        }

        PluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += () => this.configWindow.IsOpen = true;

        var executeCommandInnerAddress = new nint(ShellCommandModule.MemberFunctionPointers.ExecuteCommandInner);
        this.executeCommandInnerHook = GameInteropProvider.HookFromAddress<ExecuteCommandInnerDelegate>(
            executeCommandInnerAddress, this.ExecuteCommandInnerDetour);
        this.executeCommandInnerHook.Enable();

        Framework.Update += this.OnFrameworkUpdate;

        Log.Information("OwO Translator loaded owo");
    }

    public void Dispose()
    {
        Framework.Update -= this.OnFrameworkUpdate;

        this.executeCommandInnerHook?.Disable();
        this.executeCommandInnerHook?.Dispose();

        CommandManager.RemoveHandler(CommandName);
        if (CommandManager.Commands.ContainsKey(ShortCommandName))
            CommandManager.RemoveHandler(ShortCommandName);
        this.windowSystem.RemoveAllWindows();
        ECommonsMain.Dispose();
    }

    private void ExecuteCommandInnerDetour(ShellCommandModule* thisPtr, Utf8String* command, UIModule* uiModule)
    {
        try
        {
            if (this.config.Enabled && command != null)
            {
                var original = command->ToString();
                if (!string.IsNullOrEmpty(original))
                {
                    var transformed = this.ApplyTranslation(original);
                    if (!string.IsNullOrEmpty(transformed) && transformed != original)
                        command->SetString(transformed);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in OwO Translator's chat hook (original message was sent unmodified)");
        }

        this.executeCommandInnerHook!.Original(thisPtr, command, uiModule);
    }

    private string ApplyTranslation(string original)
    {
        if (TryExtractChatChannelMessage(original, out var prefix, out var message))
        {
            var transformedMessage = Translator.Transform(message, this.config.Mode, this.config.Intensity);
            return $"{prefix}{transformedMessage}";
        }

        if (original.TrimStart().StartsWith('/'))
            return original;

        return Translator.Transform(original, this.config.Mode, this.config.Intensity);
    }

    private static bool TryExtractChatChannelMessage(string original, out string prefix, out string message)
    {
        prefix = string.Empty;
        message = string.Empty;

        var trimmed = original.TrimStart();
        if (!trimmed.StartsWith('/'))
            return false;

        var firstSpace = trimmed.IndexOf(' ');
        if (firstSpace < 0)
            return false;

        var word = trimmed[1..firstSpace];
        if (!KnownChannelCommands.TryGetValue(word, out var info))
            return false;

        var rest = trimmed[(firstSpace + 1)..];
        var targetTokens = string.Empty;

        for (var i = 0; i < info.TargetTokens; i++)
        {
            var nextSpace = rest.IndexOf(' ');
            if (nextSpace < 0)
                return false;

            targetTokens += rest[..(nextSpace + 1)];
            rest = rest[(nextSpace + 1)..];
        }

        if (rest.Length == 0)
            return false;

        prefix = $"{info.Canonical} {targetTokens}";
        message = rest;
        return true;
    }

    private void OnCommand(string command, string rawArgs)
    {
        var args = rawArgs.Trim();

        if (args.Length == 0)
        {
            this.configWindow.IsOpen = !this.configWindow.IsOpen;
            return;
        }

        if (args.Equals("toggle", StringComparison.OrdinalIgnoreCase))
        {
            this.config.Enabled = !this.config.Enabled;
            this.config.Save();
            ChatGui.Print($"[OwO Translator] {(this.config.Enabled ? "Enabled" : "Disabled")}");
            return;
        }

        if (args.Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            this.config.Enabled = true;
            this.config.Save();
            ChatGui.Print("[OwO Translator] Enabled");
            return;
        }

        if (args.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            this.config.Enabled = false;
            this.config.Save();
            ChatGui.Print("[OwO Translator] Disabled");
            return;
        }

        var slashCommand = DefaultChannelSlash[this.config.DefaultChannel];
        var message = args;

        var firstSpace = args.IndexOf(' ');
        if (firstSpace > 0)
        {
            var firstWord = args[..firstSpace];
            if (ChannelPrefixes.TryGetValue(firstWord, out var explicitSlash))
            {
                slashCommand = explicitSlash;
                message = args[(firstSpace + 1)..].Trim();
            }
        }

        if (message.Length == 0)
        {
            ChatGui.PrintError("[OwO Translator] You didn't type a message.");
            return;
        }

        var toSend = this.config.Enabled
            ? Translator.Transform(message, this.config.Mode, this.config.Intensity)
            : message;

        if (toSend.TrimStart().StartsWith('/'))
        {
            ChatGui.PrintError("[OwO Translator] The message can't start with '/'.");
            return;
        }

        if (toSend.Length > MaxMessageLength)
        {
            ChatGui.PrintError(
                $"[OwO Translator] Message too long ({toSend.Length} characters, " +
                $"max {MaxMessageLength}). Shorten it and try again.");
            return;
        }

        try
        {
            Chat.SendMessage($"{slashCommand} {toSend}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send message via OwO Translator");
            ChatGui.PrintError("[OwO Translator] Couldn't send the message. Check /xllog for details.");
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!this.config.AutoSendEnabled)
        {
            this.nextAutoSendAt = DateTime.MinValue;
            return;
        }

        if (this.nextAutoSendAt == DateTime.MinValue)
        {
            this.ScheduleNextAutoSend();
            return;
        }

        if (DateTime.UtcNow < this.nextAutoSendAt)
            return;

        this.SendRandomAutoMessage();
        this.ScheduleNextAutoSend();
    }

    private void ResetAutoSendSchedule()
    {
        this.nextAutoSendAt = DateTime.MinValue;
    }

    private void ScheduleNextAutoSend()
    {
        var minutes = Math.Max(1, this.config.AutoSendIntervalMinutes);
        var jitterFactor = 0.5 + (Rng.NextDouble() * 1.0);
        var delay = TimeSpan.FromMinutes(minutes * jitterFactor);
        this.nextAutoSendAt = DateTime.UtcNow + delay;
    }

    private void SendRandomAutoMessage()
    {
        var pool = this.config.AutoSendMessages?.Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
        if (pool == null || pool.Count == 0)
            return;

        var phrase = pool[Rng.Next(pool.Count)];

        var toSend = this.config.Enabled
            ? Translator.Transform(phrase, this.config.Mode, this.config.Intensity)
            : phrase;

        if (toSend.TrimStart().StartsWith('/'))
            return;

        if (toSend.Length > MaxMessageLength)
            toSend = toSend[..MaxMessageLength];

        var slashCommand = DefaultChannelSlash[this.config.AutoSendChannel];

        try
        {
            Chat.SendMessage($"{slashCommand} {toSend}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Auto MSG Fail");
        }
    }
}
