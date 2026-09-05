using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs;
using Lumina.Excel.Sheets;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using TextToTalk.Backends;
using TextToTalk.Backends.Azure;
using TextToTalk.Backends.ElevenLabs;
using TextToTalk.Backends.OpenAI;
using TextToTalk.Backends.FishAudio;
using TextToTalk.Data.Model;
using TextToTalk.GameEnums;
using TextToTalk.Services;
using static TextToTalk.Backends.Azure.AzureClient;
using System.Text.RegularExpressions;
using TextToTalk.Resources;

namespace TextToTalk.UI.Windows
{
    public interface IVoiceStylesWindow
    {
        void Draw(IConfigUIDelegates helpers);
    }

    public interface IWindowController
    {
        void ToggleStyle();
    }

    public class VoiceStyles : Window
    {
        private readonly VoiceBackendManager backendManager;
        private readonly IConfigUIDelegates helpers;
        private readonly PluginConfiguration config;
        private readonly Dictionary<Type, IVoiceStylesWindow> componentCache = new();
        public static VoiceStyles? Instance { get; private set; }
        private string currentPreview = "";

        public string BuildWrappedPattern(string delimiter)
        {
            // Regex.Escape ensures characters like '$' or '*' don't break the pattern
            string escapedDelimiter = Regex.Escape(delimiter);

            // Using string interpolation to build: \$(.*?)\$
            return $"{escapedDelimiter}(.*?){escapedDelimiter}";
        }


        public VoiceStyles(VoiceBackendManager backendManager, IConfigUIDelegates helpers, PluginConfiguration config)
            : base(Strings.Get("VoiceStylesTitle"), ImGuiWindowFlags.None)

        {
            Instance = this;

            this.backendManager = backendManager;
            this.helpers = helpers;
            this.config = config;
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(40, 30),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void CopyStyleToClipboard(string style)
        {
            ImGui.SetClipboardText($"{config.StyleTag}{style}{config.StyleTag}");
        }

        public void ToggleStyle()
        {
            this.IsOpen = !this.IsOpen;
        }
        public override void Draw()
        {
            var stylesTag = config.StyleTag;
            var activeBackend = backendManager.Backend;
            if (activeBackend == null) return;
            var component = GetOrCreateComponent(activeBackend, config);
            if (component != null)
            {
                if (ImGui.CollapsingHeader($"{Strings.Get("VoiceStylesConfigureAdHocTags")}##{MemoizedId.Create()}"))
                {
                    ConfigComponents.ToggleAdHocStyleTagsEnabled(Strings.Get("VoiceStylesEnableAdHocTags"), this.config);
                    Components.HelpTooltip(Strings.Get("VoiceStylesAdHocTagsHelp"));
                    if (config.AdHocStyleTagsEnabled == true)
                    {
                        ImGui.Text(Strings.Get("VoiceStylesDelimiter"));
                        ImGui.SetNextItemWidth(35.0f);
                        if (ImGui.InputTextWithHint("##DynamicInput", Strings.Get("VoiceStylesDelimiterHint"), ref stylesTag, 30))
                        {
                            config.StyleTag = stylesTag;
                            config.StyleRegex = BuildWrappedPattern(stylesTag);
                            config.Save();
                        }
                        ImGui.SameLine();

                        ImGui.Text(string.Format(Strings.Get("VoiceStylesExampleFormat"), stylesTag));
                    }

                }
                component.Draw(helpers);
            }
            else
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey3, Strings.Get("VoiceStylesUnsupportedBackend"));
            }
        }

        public override void PreDraw()
        {
            WindowName = Strings.Get("VoiceStylesTitle");
        }

        private IVoiceStylesWindow? GetOrCreateComponent(VoiceBackend backend, PluginConfiguration config)
        {
            var type = backend.GetType();
            if (componentCache.TryGetValue(type, out var existing)) return existing;
            IVoiceStylesWindow? newComponent = backend switch
            {
                AzureBackend azure => new AzureVoiceStyles(azure, config, this),
                ElevenLabsBackend eleven => new ElevenLabsVoiceStyles(eleven, config),
                OpenAiBackend openai => new OpenAIVoiceStyles(openai, config),
                FishAudioBackend fish => new FishAudioVoiceStyles(fish, config),
                _ => null
            };

            if (newComponent != null) componentCache[type] = newComponent;
            return newComponent;
        }
    }
}
