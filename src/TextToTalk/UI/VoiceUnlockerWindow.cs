using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using R3;
using TextToTalk.Resources;

namespace TextToTalk.UI
{
    public class VoiceUnlockerWindow : Window, IDisposable
    {
        private readonly Subject<string> onResult;
        private readonly VoiceUnlockerRunner voiceUnlockerRunner;

        public VoiceUnlockerWindow(VoiceUnlockerRunner voiceUnlockerRunner) : base(Strings.Get("VoiceUnlockerTitle"))
        {
            this.onResult = new Subject<string>();
            this.voiceUnlockerRunner = voiceUnlockerRunner;

            Size = new Vector2(480, 320);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public Observable<string> OnResult()
        {
            return this.onResult;
        }

        public override void PreDraw()
        {
            WindowName = Strings.Get("VoiceUnlockerTitle");
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, ImColor.Red);
            ImGui.PushStyleColor(ImGuiCol.CheckMark, ImColor.Red);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImColor.LightRed);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImColor.DarkRed);
        }

        public override void Draw()
        {
            var manualTutorialText = Strings.Get("VoiceUnlockerManualTutorial");
            var enableAllText = Strings.Get("VoiceUnlockerEnableAll");
            ImGui.TextWrapped(Strings.Get("VoiceUnlockerWindows10Warning"));
            ImGui.TextWrapped(string.Format(Strings.Get("VoiceUnlockerManualWarningFormat"), manualTutorialText));
            ImGui.TextWrapped(string.Format(Strings.Get("VoiceUnlockerAutomaticWarningFormat"), enableAllText));

            ImGui.Spacing();

            if (ImGui.Button($"{manualTutorialText}##{MemoizedId.Create()}"))
            {
                WebBrowser.Open(
                    "https://www.reddit.com/r/Windows10/comments/96dx8z/how_unlock_all_windows_10_hidden_tts_voices_for/");
            }

            ImGui.Spacing();

            if (ImGui.Button($"{enableAllText}##{MemoizedId.Create()}"))
            {
                var resultText = this.voiceUnlockerRunner.Execute()
                    ? Strings.Get("VoiceUnlockerSuccess")
                    : Strings.Get("VoiceUnlockerStartFailed");

                IsOpen = false;
                this.onResult.OnNext(resultText);
            }

            ImGui.TextColored(ImColor.HintColor, Strings.Get("VoiceUnlockerAdminRequired"));
        }

        public override void PostDraw()
        {
            ImGui.PopStyleColor(4);
        }

        public void Dispose()
        {
            onResult.Dispose();
        }
    }
}
