using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using TextToTalk.UI;
using TextToTalk.Resources;

namespace TextToTalk.Backends;

public static class BackendUI
{
    public static void GenderedPresetConfig(string uniq, TTSBackend backend, PluginConfiguration config,
        List<VoicePreset> presets)
    {
        var voiceConfig = config.GetVoiceConfig();
        var ungenderedVoices = voiceConfig.GetUngenderedPresets(backend);
        var maleVoices = voiceConfig.GetMalePresets(backend);
        var femaleVoices = voiceConfig.GetFemalePresets(backend);

        if (ImGuiPresetCombo($"{Strings.Get("BackendUngenderedPresets")}##{MemoizedId.Create(uniq: uniq)}", ungenderedVoices, presets))
        {
            config.Save();
        }
        Components.HelpTooltip(Strings.Get("BackendUngenderedHelp"));

        if (!ungenderedVoices.Any())
        {
            ImGui.TextColored(ImColor.Red, Strings.Get("BackendNoUngenderedPresets"));
        }

        if (ImGuiPresetCombo($"{Strings.Get("BackendMalePresets")}##{MemoizedId.Create(uniq: uniq)}", maleVoices, presets))
        {
            config.Save();
        }

        if (!maleVoices.Any())
        {
            ImGui.TextColored(ImColor.Red, Strings.Get("BackendNoMalePresets"));
        }

        if (ImGuiPresetCombo($"{Strings.Get("BackendFemalePresets")}##{MemoizedId.Create(uniq: uniq)}", femaleVoices, presets))
        {
            config.Save();
        }

        if (!femaleVoices.Any())
        {
            ImGui.TextColored(ImColor.Red, Strings.Get("BackendNoFemalePresets"));
        }

        ImGuiMultiVoiceHint();
    }

    public static void NewPresetButton<TPreset>(string label, PluginConfiguration config)
        where TPreset : VoicePreset, new()
    {
        if (ImGui.Button(label) && config.TryCreateVoicePreset<TPreset>(out var newPreset))
        {
            config.SetCurrentVoicePreset(newPreset.Id);
            config.Save();
        }
    }

    public static void DeletePresetButton(string label, VoicePreset preset, TTSBackend backend,
        PluginConfiguration config)
    {
        if (ImGui.Button(label))
        {
            var voiceConfig = config.GetVoiceConfig();

            var otherPreset = voiceConfig.VoicePresets.First(p => p.Id != preset.Id);
            config.SetCurrentVoicePreset(otherPreset.Id);

            // Use TryGetValue to safely access the inner dictionary for the specific backend
            if (voiceConfig.UngenderedVoicePresets.TryGetValue(backend, out var ungendered))
            {
                ungendered.Remove(preset.Id);
            }

            if (voiceConfig.MaleVoicePresets.TryGetValue(backend, out var male))
            {
                male.Remove(preset.Id);
            }

            if (voiceConfig.FemaleVoicePresets.TryGetValue(backend, out var female))
            {
                female.Remove(preset.Id);
            }

            voiceConfig.VoicePresets.Remove(preset);

            config.Save();
        }
    }

    public static void ImGuiVoiceNotSupported()
    {
        ImGui.TextColored(ImColor.Red, Strings.Get("BackendVoiceUnsupported"));
    }

    public static void ImGuiVoiceNotSelected()
    {
        ImGui.TextColored(ImColor.Red, Strings.Get("BackendNoVoiceSelected"));
    }

    public static void ImGuiMultiVoiceHint()
    {
        ImGui.TextColored(ImColor.HintColor,
            Strings.Get("BackendMultiplePresetsHint"));
    }

    public static bool ImGuiPresetCombo(string label, SortedSet<int> selectedPresets, List<VoicePreset> presets)
    {
        var selectedPresetNames =
            presets.Where(preset => selectedPresets.Contains(preset.Id)).Select(preset => preset.Name);
        if (!ImGui.BeginCombo(label, string.Join(", ", selectedPresetNames)))
        {
            return false;
        }

        var didPresetsChange = false;

        foreach (var preset in presets)
        {
            var isPresetSelected = selectedPresets.Contains(preset.Id);
            if (ImGui.Selectable(preset.Name, ref isPresetSelected, ImGuiSelectableFlags.DontClosePopups))
            {
                if (isPresetSelected)
                {
                    selectedPresets.Add(preset.Id);
                }
                else
                {
                    selectedPresets.Remove(preset.Id);
                }

                didPresetsChange = true;
            }
        }

        ImGui.EndCombo();
        return didPresetsChange;
    }
}
