using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace OwOTranslator;

public enum ChatChannel
{
    Say,
    Party,
    FreeCompany,
    Yell,
    Shout,
}

public enum TranslatorMode
{
    Owo,
    Nyan
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = true;

    public Owoifier.Intensity Intensity { get; set; } = Owoifier.Intensity.Normal;

    public TranslatorMode Mode { get; set; } = TranslatorMode.Owo;

    public ChatChannel DefaultChannel { get; set; } = ChatChannel.Say;

    public bool AutoSendEnabled { get; set; } = false;

    public int AutoSendIntervalMinutes { get; set; } = 10;

    public ChatChannel AutoSendChannel { get; set; } = ChatChannel.Say;

    public List<string> AutoSendMessages { get; set; } = new()
    {
        
    };

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pi)
    {
        this.pluginInterface = pi;
    }

    public void Save()
    {
        this.pluginInterface?.SavePluginConfig(this);
    }
}
