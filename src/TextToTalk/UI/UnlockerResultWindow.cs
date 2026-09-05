using System.Numerics;
using Dalamud.Interface.Windowing;
using TextToTalk.Resources;
using Dalamud.Bindings.ImGui;

namespace TextToTalk.UI
{
    public class UnlockerResultWindow : Window
    {
        public string? Text { get; set; }

        public UnlockerResultWindow() : base(Strings.Get("VoiceUnlockerResultTitle"))
        {
            Size = new Vector2(320, 90);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public override void Draw()
        {
            ImGui.TextWrapped(Text ?? "");
        }

        public override void PreDraw()
        {
            WindowName = Strings.Get("VoiceUnlockerResultTitle");
        }
    }
}
