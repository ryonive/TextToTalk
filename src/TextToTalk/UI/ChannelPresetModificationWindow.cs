using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using TextToTalk.Resources;

namespace TextToTalk.UI
{
    public class ChannelPresetModificationWindow : Window
    {
        private readonly PluginConfiguration config;
        
        public ChannelPresetModificationWindow(PluginConfiguration config) : base($"{Strings.Get("ChannelPresetWindowTitle")}##TTTPresetWindow", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize)
        {
            this.config = config;
        }

        public override void Draw()
        {
            var preset = this.config.GetCurrentEnabledChatTypesPreset();

            var presetName = preset.Name;
            if (ImGui.InputText($"{Strings.Get("ChannelPresetName")}##{MemoizedId.Create()}", ref presetName, 200))
            {
                preset.Name = presetName;
                this.config.Save();
            }

            var useKeybind = preset.UseKeybind;
            if (ImGui.Checkbox(Strings.EnableKeybind, ref useKeybind))
            {
                preset.UseKeybind = useKeybind;
                this.config.Save();
            }

            if (useKeybind)
            {
                ImGui.PushItemWidth(100f);
                var kItem1 = VirtualKey.EnumToIndex(preset.ModifierKey);
                if (ImGui.Combo($"##{MemoizedId.Create()}", ref kItem1, VirtualKey.DisplayNames.Take(3).ToArray(), 3))
                {
                    preset.ModifierKey = VirtualKey.IndexToEnum(kItem1);
                    this.config.Save();
                }
                ImGui.SameLine();
                var kItem2 = VirtualKey.EnumToIndex(preset.MajorKey) - 3;
                if (ImGui.Combo($"{Strings.Get("ChannelPresetEnableKeybind")}##{MemoizedId.Create()}", ref kItem2, VirtualKey.DisplayNames.Skip(3).ToArray(), VirtualKey.DisplayNames.Length - 3))
                {
                    preset.MajorKey = VirtualKey.IndexToEnum(kItem2 + 3);
                    this.config.Save();
                }
                ImGui.PopItemWidth();
            }

            ImGui.Spacing();

            if (ImGui.Button($"{Strings.Get("ActionClose")}###{MemoizedId.Create()}"))
            {
                IsOpen = false;
            }
        }

        public override void PreDraw()
        {
            WindowName = $"{Strings.Get("ChannelPresetWindowTitle")}##TTTPresetWindow";
        }
    }
}
