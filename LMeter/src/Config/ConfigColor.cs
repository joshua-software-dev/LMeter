using ImGuiNET;
using Newtonsoft.Json;
using System.Numerics;


namespace LMeter.Config;

public class ConfigColor
{
    [JsonIgnore]
    public uint Base { get; private set; }

    [JsonIgnore]
    public uint Background { get; private set; }

    [JsonIgnore]
    public uint TopGradient { get; private set; }

    [JsonIgnore]
    public uint BottomGradient { get; private set; }

    [JsonIgnore]
    private Vector4 _vector;
    public Vector4 Vector
    {
        get => _vector;
        set
        {
            if (_vector == value)
            {
                return;
            }

            _vector = value;

            Update();
        }
    }

    // Constructor for deserialization
    public ConfigColor() : this(Vector4.Zero) { }

    public ConfigColor
    (
        float r,
        float g,
        float b,
        float a
    ) : this(new Vector4(r, g, b, a)) { }

    public ConfigColor(Vector4 vector) =>
        this.Vector = vector;

    private void Update() =>
        Base = ImGui.ColorConvertFloat4ToU32(_vector);
}
