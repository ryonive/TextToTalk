using System;
using System.Globalization;
using System.Linq;
using Dalamud.Plugin.Services;
using TextToTalk.Backends;
using TextToTalk.UI;
using TextToTalk.UI.Windows;

namespace TextToTalk.CommandModules;

public class MainCommandModule : CommandModule
{
    private readonly IChatGui chat;

    private readonly PluginConfiguration config;
    private readonly VoiceBackendManager backendManager;
    private readonly ConfigurationWindow configurationWindow;
    private readonly IConfigUIDelegates configUIDelegates;
    private readonly VoiceStyles StylesWindow;

    public MainCommandModule(ICommandManager commandManager, IChatGui chat, PluginConfiguration config,
        VoiceBackendManager backendManager, ConfigurationWindow configurationWindow, IConfigUIDelegates configUIDelegates, VoiceStyles StylesWindow) : base(commandManager) //ElevenLabsStylesWindow elevenLabsStylesWindow)
    {
        this.chat = chat;

        this.config = config;
        this.backendManager = backendManager;
        this.configurationWindow = configurationWindow;
        this.configUIDelegates = configUIDelegates;
        this.StylesWindow = StylesWindow;

        AddCommand("/canceltts", CancelTts, "Cancel all queued TTS messages.");
        AddCommand("/toggletts", ToggleTts, "Toggle TextToTalk's text-to-speech.");
        AddCommand("/disabletts", DisableTts, "Disable TextToTalk's text-to-speech.");
        AddCommand("/enabletts", EnableTts, "Enable TextToTalk's text-to-speech.");
        AddCommand("/tttconfig", ToggleConfig, "Toggle TextToTalk's configuration window.");
        AddCommand("/tttstyles", ToggleStyles, "Toggle TextToTalk's styles window.");
        AddCommand("/tttpreset", SwitchPreset,
            "Switch the active channel settings preset by name. Run with no arguments to show the current preset and list available presets.");
        AddCommand("/tttvolume", AdjustVolume,
            "Adjust the global TTS volume. Pass a percentage (0-200) to set, or +N/-N to adjust relatively. Run with no arguments to show the current volume.");
    }

    public void CancelTts(string command = "", string args = "")
    {
        this.backendManager.CancelAllSpeech();
    }

    public void ToggleTts(string command = "", string args = "")
    {
        if (this.config.Enabled)
            DisableTts();
        else
            EnableTts();
    }

    public void DisableTts(string command = "", string args = "")
    {
        this.config.Enabled = false;
        CancelTts();
        this.chat.Print("TTS disabled.");
        DetailedLog.Info("TTS disabled.");
    }

    public void EnableTts(string command = "", string args = "")
    {
        this.config.Enabled = true;
        this.chat.Print("TTS enabled.");
        DetailedLog.Info("TTS enabled.");
    }

    public void ToggleConfig(string command = "", string args = "")
    {
        this.configurationWindow.Toggle();
    }
    public void ToggleStyles(string command = "", string args = "")
    {
        this.StylesWindow.Toggle();
    }

    public void SwitchPreset(string command = "", string args = "")
    {
        var trimmed = (args ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            var current = this.config.GetCurrentEnabledChatTypesPreset();
            this.chat.Print($"Current preset: {current?.Name ?? "(unnamed)"}");
            var names = string.Join(", ",
                this.config.EnabledChatTypesPresets.Select(p => p.Name ?? $"#{p.Id}"));
            this.chat.Print($"Available presets: {names}");
            return;
        }

        var match = this.config.EnabledChatTypesPresets
            .FirstOrDefault(p => string.Equals(p.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            this.chat.Print($"No preset named \"{trimmed}\" exists.");
            return;
        }

        this.config.SetCurrentEnabledChatTypesPreset(match.Id);
        this.config.Save();
        this.chat.Print($"TextToTalk preset -> {match.Name}");
        DetailedLog.Info($"TextToTalk preset -> {match.Name}");
    }

    public void AdjustVolume(string command = "", string args = "")
    {
        var trimmed = (args ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            this.chat.Print($"Current volume: {this.config.GlobalVolume * 100f:0}%");
            return;
        }

        var current = this.config.GlobalVolume * 100f;
        float target;
        if (trimmed[0] is '+' or '-')
        {
            if (!float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var delta))
            {
                this.chat.Print($"Invalid volume adjustment: \"{trimmed}\". Use +N or -N.");
                return;
            }
            target = current + delta;
        }
        else
        {
            if (!float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                this.chat.Print($"Invalid volume: \"{trimmed}\". Use a percentage (0-200), +N, or -N.");
                return;
            }
            target = value;
        }

        var clamped = Math.Clamp(target, 0f, 200f);
        this.config.GlobalVolume = clamped / 100f;
        this.config.Save();
        this.chat.Print($"Volume -> {clamped:0}%");
        DetailedLog.Info($"Volume -> {clamped:0}%");
    }
}