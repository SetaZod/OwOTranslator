using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using System.Linq;

namespace OwOTranslator;

public class ConfigWindow : Window
{
    private readonly Configuration config;
    private readonly Action onAutoSendSettingsChanged;
    private string autoSendMessagesBuffer;

    public ConfigWindow(Configuration config, Action onAutoSendSettingsChanged) : base(
        "OwO Translator - Settings###OwOTranslatorConfig",
        ImGuiWindowFlags.NoScrollbar)
    {
        this.config = config;
        this.onAutoSendSettingsChanged = onAutoSendSettingsChanged;
        this.Size = new Vector2(440, 540);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.autoSendMessagesBuffer = string.Join('\n', this.config.AutoSendMessages);
    }

    public override void Draw()
    {
        var enabled = this.config.Enabled;
        if (ImGui.Checkbox("Enable owo/nyan transformation", ref enabled))
        {
            this.config.Enabled = enabled;
            this.config.Save();
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Translation style:");

        var mode = this.config.Mode;
        if (ImGui.RadioButton("owo/uwu", mode == TranslatorMode.Owo))
        {
            this.config.Mode = TranslatorMode.Owo;
            this.config.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("nyan", mode == TranslatorMode.Nyan))
        {
            this.config.Mode = TranslatorMode.Nyan;
            this.config.Save();
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Intensity:");

        var intensity = (int)this.config.Intensity;
        if (ImGui.RadioButton("Light", intensity == (int)Owoifier.Intensity.Leve))
        {
            this.config.Intensity = Owoifier.Intensity.Leve;
            this.config.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Normal", intensity == (int)Owoifier.Intensity.Normal))
        {
            this.config.Intensity = Owoifier.Intensity.Normal;
            this.config.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Extreme", intensity == (int)Owoifier.Intensity.Extremo))
        {
            this.config.Intensity = Owoifier.Intensity.Extremo;
            this.config.Save();
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Auto-send:");

        var autoEnabled = this.config.AutoSendEnabled;
        if (ImGui.Checkbox("Send a random message every so often", ref autoEnabled))
        {
            this.config.AutoSendEnabled = autoEnabled;
            this.config.Save();
            this.onAutoSendSettingsChanged();
        }

        var interval = this.config.AutoSendIntervalMinutes;
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt("Interval (minutes)", ref interval))
        {
            if (interval < 1) interval = 1;
            if (interval > 240) interval = 240;
            this.config.AutoSendIntervalMinutes = interval;
            this.config.Save();
            this.onAutoSendSettingsChanged();
        }

        ImGui.TextUnformatted("Auto-send channel:");
        DrawChannelRadio("Say##auto", this.config.AutoSendChannel, c => this.config.AutoSendChannel = c, ChatChannel.Say);
        ImGui.SameLine();
        DrawChannelRadio("Party##auto", this.config.AutoSendChannel, c => this.config.AutoSendChannel = c, ChatChannel.Party);
        ImGui.SameLine();
        DrawChannelRadio("FC##auto", this.config.AutoSendChannel, c => this.config.AutoSendChannel = c, ChatChannel.FreeCompany);
        DrawChannelRadio("Yell##auto", this.config.AutoSendChannel, c => this.config.AutoSendChannel = c, ChatChannel.Yell);
        ImGui.SameLine();
        DrawChannelRadio("Shout##auto", this.config.AutoSendChannel, c => this.config.AutoSendChannel = c, ChatChannel.Shout);

        ImGui.TextUnformatted("Phrases (one per line, one is picked at random on each send):");
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextMultiline("##autosendmessages", ref this.autoSendMessagesBuffer, 4000, new Vector2(0, 100)))
        {
            this.config.AutoSendMessages = this.autoSendMessagesBuffer
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            this.config.Save();
        }

        ImGui.Separator();
        ImGui.TextWrapped(
            "While the plugin is ENABLED, everything you type into the game's chat box - " +
            "Say, Party, FC, Yell, Shout, Tell, and more - gets transformed before other " +
            "players see it. Channel commands such as /p, /y, /tell also have their " +
            "message translated (the /tell target itself is left untouched). Any other " +
            "commands and macros are NEVER modified.");
        ImGui.Spacing();
        ImGui.TextWrapped(
            "The '/owo' command (or its shortcut '/o') also works as a quick way to send " +
            "a one-off translated message to a specific channel: '/owo p ...', " +
            "'/owo fc ...', '/owo y ...', '/owo sh ...', '/owo s ...'. Without a channel " +
            "prefix it sends to your default channel (Say). Typing '/owo' alone opens or " +
            "closes this settings window. '/owo on', '/owo off' and '/owo toggle' switch " +
            "the transformation on/off (same as the checkbox above).");
    }

    private static void DrawChannelRadio(string label, ChatChannel current, Action<ChatChannel> setter, ChatChannel channel)
    {
        if (ImGui.RadioButton(label, current == channel))
            setter(channel);
    }
}
