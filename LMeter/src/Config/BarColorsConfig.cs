using ImGuiNET;
using LMeter.Helpers;
using System.Numerics;


namespace LMeter.Config;

public class BarColorsConfig : IConfigPage
{
    public string Name => "Colors";

    public IConfigPage GetDefault() =>
        new BarColorsConfig();

    public ConfigColor PLDColor = new (r: 168f / 255f, g: 210f / 255f, b: 230f / 255f, a: 1f);
    public ConfigColor DRKColor = new (r: 209f / 255f, g: 38f / 255f, b: 204f / 255f, a: 1f);
    public ConfigColor WARColor = new (r: 207f / 255f, g: 38f / 255f, b: 33f / 255f, a: 1f);
    public ConfigColor GNBColor = new (r: 121f / 255f, g: 109f / 255f, b: 48f / 255f, a: 1f);
    public ConfigColor GLAColor = new (r: 168f / 255f, g: 210f / 255f, b: 230f / 255f, a: 1f);
    public ConfigColor MRDColor = new (r: 207f / 255f, g: 38f / 255f, b: 33f / 255f, a: 1f);

    public ConfigColor SCHColor = new (r: 134f / 255f, g: 87f / 255f, b: 255f / 255f, a: 1f);
    public ConfigColor WHMColor = new (r: 255f / 255f, g: 240f / 255f, b: 220f / 255f, a: 1f);
    public ConfigColor ASTColor = new (r: 255f / 255f, g: 231f / 255f, b: 74f / 255f, a: 1f);
    public ConfigColor SGEColor = new (r: 144f / 255f, g: 176f / 255f, b: 255f / 255f, a: 1f);
    public ConfigColor CNJColor = new (r: 255f / 255f, g: 240f / 255f, b: 220f / 255f, a: 1f);

    public ConfigColor MNKColor = new (r: 214f / 255f, g: 156f / 255f, b: 0f / 255f, a: 1f);
    public ConfigColor NINColor = new (r: 175f / 255f, g: 25f / 255f, b: 100f / 255f, a: 1f);
    public ConfigColor DRGColor = new (r: 65f / 255f, g: 100f / 255f, b: 205f / 255f, a: 1f);
    public ConfigColor SAMColor = new (r: 228f / 255f, g: 109f / 255f, b: 4f / 255f, a: 1f);
    public ConfigColor RPRColor = new (r: 150f / 255f, g: 90f / 255f, b: 144f / 255f, a: 1f);
    public ConfigColor PGLColor = new (r: 214f / 255f, g: 156f / 255f, b: 0f / 255f, a: 1f);
    public ConfigColor ROGColor = new (r: 175f / 255f, g: 25f / 255f, b: 100f / 255f, a: 1f);
    public ConfigColor LNCColor = new (r: 65f / 255f, g: 100f / 255f, b: 205f / 255f, a: 1f);

    public ConfigColor BRDColor = new (r: 145f / 255f, g: 186f / 255f, b: 94f / 255f, a: 1f);
    public ConfigColor MCHColor = new (r: 110f / 255f, g: 225f / 255f, b: 214f / 255f, a: 1f);
    public ConfigColor DNCColor = new (r: 226f / 255f, g: 176f / 255f, b: 175f / 255f, a: 1f);
    public ConfigColor ARCColor = new (r: 145f / 255f, g: 186f / 255f, b: 94f / 255f, a: 1f);

    public ConfigColor BLMColor = new (r: 165f / 255f, g: 121f / 255f, b: 214f / 255f, a: 1f);
    public ConfigColor SMNColor = new (r: 45f / 255f, g: 155f / 255f, b: 120f / 255f, a: 1f);
    public ConfigColor RDMColor = new (r: 232f / 255f, g: 123f / 255f, b: 123f / 255f, a: 1f);
    public ConfigColor BLUColor = new (r: 0f / 255f, g: 185f / 255f, b: 247f / 255f, a: 1f);
    public ConfigColor THMColor = new (r: 165f / 255f, g: 121f / 255f, b: 214f / 255f, a: 1f);
    public ConfigColor ACNColor = new (r: 45f / 255f, g: 155f / 255f, b: 120f / 255f, a: 1f);

    public ConfigColor UKNColor = new (r: 218f / 255f, g: 157f / 255f, b: 46f / 255f, a: 1f);

    public ConfigColor GetColor(Job job) => job switch
    {
        Job.GLA => this.GLAColor,
        Job.MRD => this.MRDColor,
        Job.PLD => this.PLDColor,
        Job.WAR => this.WARColor,
        Job.DRK => this.DRKColor,
        Job.GNB => this.GNBColor,

        Job.CNJ => this.CNJColor,
        Job.WHM => this.WHMColor,
        Job.SCH => this.SCHColor,
        Job.AST => this.ASTColor,
        Job.SGE => this.SGEColor,

        Job.PGL => this.PGLColor,
        Job.LNC => this.LNCColor,
        Job.ROG => this.ROGColor,
        Job.MNK => this.MNKColor,
        Job.DRG => this.DRGColor,
        Job.NIN => this.NINColor,
        Job.SAM => this.SAMColor,
        Job.RPR => this.RPRColor,

        Job.ARC => this.ARCColor,
        Job.BRD => this.BRDColor,
        Job.MCH => this.MCHColor,
        Job.DNC => this.DNCColor,

        Job.THM => this.THMColor,
        Job.ACN => this.ACNColor,
        Job.BLM => this.BLMColor,
        Job.SMN => this.SMNColor,
        Job.RDM => this.RDMColor,
        Job.BLU => this.BLUColor,
        _       => this.UKNColor
    };

    public void DrawConfig(Vector2 size, float padX, float padY)
    {
        if (!ImGui.BeginChild($"##{this.Name}", new Vector2(size.X, size.Y), true))
        {
            ImGui.EndChild();
            return;
        }

        var vector = PLDColor.Vector;
        ImGui.ColorEdit4("PLD", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.PLDColor.Vector = vector;

        vector = WARColor.Vector;
        ImGui.ColorEdit4("WAR", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.WARColor.Vector = vector;
        vector = DRKColor.Vector;
        ImGui.ColorEdit4("DRK", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.DRKColor.Vector = vector;

        vector = GNBColor.Vector;
        ImGui.ColorEdit4("GNB", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.GNBColor.Vector = vector;

        ImGui.NewLine();

        vector = SCHColor.Vector;
        ImGui.ColorEdit4("SCH", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.SCHColor.Vector = vector;

        vector = WHMColor.Vector;
        ImGui.ColorEdit4("WHM", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.WHMColor.Vector = vector;

        vector = ASTColor.Vector;
        ImGui.ColorEdit4("AST", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.ASTColor.Vector = vector;

        vector = SGEColor.Vector;
        ImGui.ColorEdit4("SGE", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.SGEColor.Vector = vector;

        ImGui.NewLine();

        vector = MNKColor.Vector;
        ImGui.ColorEdit4("MNK", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.MNKColor.Vector = vector;

        vector = NINColor.Vector;
        ImGui.ColorEdit4("NIN", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.NINColor.Vector = vector;

        vector = DRGColor.Vector;
        ImGui.ColorEdit4("DRG", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.DRGColor.Vector = vector;

        vector = SAMColor.Vector;
        ImGui.ColorEdit4("SAM", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.SAMColor.Vector = vector;

        vector = RPRColor.Vector;
        ImGui.ColorEdit4("RPR", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.RPRColor.Vector = vector;

        ImGui.NewLine();

        vector = BRDColor.Vector;
        ImGui.ColorEdit4("BRD", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.BRDColor.Vector = vector;

        vector = MCHColor.Vector;
        ImGui.ColorEdit4("MCH", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.MCHColor.Vector = vector;

        vector = DNCColor.Vector;
        ImGui.ColorEdit4("DNC", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.DNCColor.Vector = vector;

        ImGui.NewLine();

        vector = BLMColor.Vector;
        ImGui.ColorEdit4("BLM", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.BLMColor.Vector = vector;

        vector = SMNColor.Vector;
        ImGui.ColorEdit4("SMN", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.SMNColor.Vector = vector;

        vector = RDMColor.Vector;
        ImGui.ColorEdit4("RDM", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.RDMColor.Vector = vector;

        vector = BLUColor.Vector;
        ImGui.ColorEdit4("BLU", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.BLUColor.Vector = vector;

        ImGui.NewLine();

        vector = GLAColor.Vector;
        ImGui.ColorEdit4("GLA", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.GLAColor.Vector = vector;

        vector = MRDColor.Vector;
        ImGui.ColorEdit4("MRD", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.MRDColor.Vector = vector;

        vector = CNJColor.Vector;
        ImGui.ColorEdit4("CNJ", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.CNJColor.Vector = vector;

        vector = PGLColor.Vector;
        ImGui.ColorEdit4("PGL", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.PGLColor.Vector = vector;

        vector = ROGColor.Vector;
        ImGui.ColorEdit4("ROG", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.ROGColor.Vector = vector;

        vector = LNCColor.Vector;
        ImGui.ColorEdit4("LNC", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.LNCColor.Vector = vector;

        vector = ARCColor.Vector;
        ImGui.ColorEdit4("ARC", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.ARCColor.Vector = vector;

        vector = THMColor.Vector;
        ImGui.ColorEdit4("THM", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.THMColor.Vector = vector;

        vector = ACNColor.Vector;
        ImGui.ColorEdit4("ACN", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        this.ACNColor.Vector = vector;

        ImGui.EndChild();
    }
}
