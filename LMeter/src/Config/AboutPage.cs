using ImGuiNET;
using LMeter.Helpers;
using System.Numerics;


namespace LMeter.Config;

public class AboutPage : IConfigPage
{
    public string Name => "About / Changelog";

    public IConfigPage GetDefault() => new AboutPage();

    public void DrawConfig(Vector2 size, float padX, float padY)
    {
        try
        {
            if (ImGui.BeginChild("##AboutPage", new Vector2(size.X, size.Y), true))
            {
                Vector2 headerSize = Vector2.Zero;
                if (Plugin.IconTexture is not null)
                {
                    Vector2 iconSize = new Vector2(Plugin.IconTexture.Width, Plugin.IconTexture.Height);
                    string versionText = 
                        $"""
                        LMeter
                        v{Plugin.Version}
                        git: {Plugin.GitHash}
                        """;
                    Vector2 textSize = ImGui.CalcTextSize(versionText);
                    headerSize = new Vector2(size.X, iconSize.Y + textSize.Y);

                    bool iconActivated = false;
                    try
                    {
                        iconActivated = ImGui.BeginChild("##Icon", headerSize, false);
                        if (iconActivated)
                        {
                            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                            Vector2 pos = ImGui.GetWindowPos().AddX(size.X / 2 - iconSize.X / 2);
                            drawList.AddImage(Plugin.IconTexture.ImGuiHandle, pos, pos + iconSize);
                            Vector2 textPos = ImGui.GetWindowPos().AddX(size.X / 2 - textSize.X / 2).AddY(iconSize.Y);
                            drawList.AddText(textPos, 0xFFFFFFFF, versionText);
                        }
                    }
                    finally
                    {
                        if (iconActivated) ImGui.EndChild();
                    }
                }

                ImGui.Text("Changelog");
                Vector2 changeLogSize = new Vector2(size.X - padX * 2, size.Y - ImGui.GetCursorPosY() - padY - 30);

                if (ImGui.BeginChild("##Changelog", changeLogSize, true))
                {
                    ImGui.Text(Plugin.Changelog);
                    ImGui.EndChild();
                }
                ImGui.NewLine();

                Vector2 buttonSize = new Vector2
                (
                    x: (size.X - padX * 2 - padX * 2) / 3, 
                    y: 30 - padY * 2
                );

                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
                ImGui.SameLine((size.X - (buttonSize.X * 2)) * 0.5f); // start buttons centered
                if (ImGui.Button("Github", buttonSize))
                {
                    Utils.OpenUrl("https://github.com/joshua-software-dev/LMeter");
                }

                ImGui.SameLine();
                if (ImGui.Button("Discord", buttonSize))
                {
                    Utils.OpenUrl("https://discord.gg/C6fptVuFzZ");
                }
                ImGui.PopStyleVar();
            }
        }
        finally
        {
            ImGui.EndChild();
        }
    }
}
