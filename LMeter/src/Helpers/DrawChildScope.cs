using ImGuiNET;
using System;
using System.Numerics;


namespace LMeter.Helpers;

public class DrawChildScope : IDisposable
{
    public readonly bool Success;

    public DrawChildScope(string label, Vector2 size, bool border)
    {
        Success = ImGui.BeginChild(label, size, border);
    }

    public void Dispose()
    {
        ImGui.EndChild();
        GC.SuppressFinalize(this);
    }
}
