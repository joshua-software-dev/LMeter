using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;


namespace LMeter.Helpers
{
    public struct FontData
    {
        public string Name;
        public int Size;
        public bool Chinese;
        public bool Korean;

        public FontData(string name, int size, bool chinese, bool korean)
        {
            Name = name;
            Size = size;
            Chinese = chinese;
            Korean = korean;
        }
    }

    public class FontScope : IDisposable
    {
        private readonly bool _fontPushed;

        public FontScope(bool fontPushed)
        {
            _fontPushed = fontPushed;
        }

        public void Dispose()
        {
            if (_fontPushed)
            {
                ImGui.PopFont();
            }

            GC.SuppressFinalize(this);
        }
    }

    public class FontsManager : IDisposable
    {
        private IEnumerable<FontData> _fontData;
        private readonly Dictionary<string, ImFontPtr> _imGuiFonts = new ();
        private string[] _fontList = new string[] { DalamudFontKey };
        private readonly UiBuilder _uiBuilder;
        public const string DalamudFontKey = "Dalamud Font";
        public static readonly List<string> DefaultFontKeys = 
            new ()
            {
                "Expressway_24",
                "Expressway_20",
                "Expressway_16"
            };

        public static string DefaultBigFontKey => 
            DefaultFontKeys[0];
        public static string DefaultMediumFontKey => 
            DefaultFontKeys[1];
        public static string DefaultSmallFontKey => 
            DefaultFontKeys[2];

        public FontsManager(UiBuilder uiBuilder, IEnumerable<FontData> fonts)
        {
            _fontData = fonts;
            _uiBuilder = uiBuilder;
            _uiBuilder.BuildFonts += BuildFonts;
            _uiBuilder.RebuildFonts();
        }

        public void BuildFonts()
        {
            var fontDir = GetUserFontPath();
            if (string.IsNullOrEmpty(fontDir)) return;

            _imGuiFonts.Clear();
            var io = ImGui.GetIO();

            foreach (var font in _fontData)
            {
                var fontPath = $"{fontDir}{font.Name}.ttf";
                if (!File.Exists(fontPath)) continue;

                try
                {
                    var ranges = this.GetCharacterRanges(font, io);
                    var imFont = !ranges.HasValue
                        ? io.Fonts.AddFontFromFileTTF(fontPath, font.Size)
                        : io.Fonts.AddFontFromFileTTF(fontPath, font.Size, null, ranges.Value.Data);

                    _imGuiFonts.Add(GetFontKey(font), imFont);
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Failed to load font from path [{fontPath}]!");
                    PluginLog.Error(ex.ToString());
                }
            }

            var fontList = new List<string>() { DalamudFontKey };
            fontList.AddRange(_imGuiFonts.Keys);
            _fontList = fontList.ToArray();
        }

        public static bool ValidateFont(string[] fontOptions, int fontId, string fontKey) =>
            fontId < fontOptions.Length && fontOptions[fontId].Equals(fontKey);

        public FontScope PushFont(string fontKey)
        {
            if 
            (
                string.IsNullOrEmpty(fontKey) ||
                fontKey.Equals(DalamudFontKey) ||
                !_imGuiFonts.ContainsKey(fontKey)
            )
            {
                return new FontScope(false);
            }

            ImGui.PushFont(this._imGuiFonts[fontKey]);
            return new FontScope(true);
        }

        public FontScope PushFont(ImFontPtr fontPtr)
        {
            ImGui.PushFont(fontPtr);
            return new FontScope(true);
        }

        public void UpdateFonts(IEnumerable<FontData> fonts)
        {
            _fontData = fonts;
            _uiBuilder.RebuildFonts();
        }

        public string[] GetFontList() =>
            this._fontList;

        public int GetFontIndex(string fontKey)
        {
            for (var i = 0; i < _fontList.Length; i++)
            {
                if (_fontList[i].Equals(fontKey))
                {
                    return i;
                }
            }

            return 0;
        }

        private unsafe ImVector? GetCharacterRanges(FontData font, ImGuiIOPtr io)
        {
            if (!font.Chinese && !font.Korean) return null;

            var builder = new ImFontGlyphRangesBuilderPtr
            (
                ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder()
            );

            if (font.Chinese)
            {
                // GetGlyphRangesChineseFull() includes Default + Hiragana, Katakana, Half-Width, Selection of 1946 Ideographs
                // https://skia.googlesource.com/external/github.com/ocornut/imgui/+/v1.53/extra_fonts/README.txt
                builder.AddRanges(io.Fonts.GetGlyphRangesChineseFull());
            }

            if (font.Korean)
            {
                builder.AddRanges(io.Fonts.GetGlyphRangesKorean());
            }

            builder.BuildRanges(out var ranges);
            return ranges;
        }

        public static string GetFontKey(FontData font) =>
            $"{font.Name}_{font.Size}" + 
            (
                font.Chinese 
                    ? "_cnjp" 
                    : string.Empty
            ) + 
            (
                font.Korean 
                    ? "_kr" 
                    : string.Empty
            );

        public static void CopyPluginFontsToUserPath()
        {
            var pluginFontPath = GetPluginFontPath();
            var userFontPath = GetUserFontPath();

            if (string.IsNullOrEmpty(pluginFontPath) || string.IsNullOrEmpty(userFontPath)) return;

            try
            {
                Directory.CreateDirectory(userFontPath);
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"Failed to create User Font Directory {ex}");
            }

            if (!Directory.Exists(userFontPath)) return;

            string[] pluginFonts;
            try
            {
                pluginFonts = Directory.GetFiles(pluginFontPath, "*.ttf");
            }
            catch
            {
                pluginFonts = Array.Empty<string>();
            }

            foreach (var font in pluginFonts)
            {
                try
                {
                    if (!string.IsNullOrEmpty(font))
                    {
                        var fileName = font.Replace(pluginFontPath, string.Empty);
                        var copyPath = Path.Combine(userFontPath, fileName);
                        if (!File.Exists(copyPath))
                        {
                            File.Copy(font, copyPath, false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Warning($"Failed to copy font {font} to User Font Directory: {ex}");
                }
            }
        }

        public static string GetPluginFontPath()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (path is not null)
            {
                return $"{path}\\Media\\Fonts\\";
            }

            return string.Empty;
        }

        public static string GetUserFontPath() =>
            $"{Plugin.ConfigFileDir}\\Fonts\\";

        public static string[] GetFontNamesFromPath(string? path)
        {
            if (string.IsNullOrEmpty(path)) return Array.Empty<string>();

            string[] fonts;
            try
            {
                fonts = Directory.GetFiles(path, "*.ttf");
            }
            catch
            {
                fonts = Array.Empty<string>();
            }

            for (var i = 0; i < fonts.Length; i++)
            {
                fonts[i] = fonts[i]
                    .Replace(path, string.Empty)
                    .Replace(".ttf", string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return fonts;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _uiBuilder.BuildFonts -= BuildFonts;
                _imGuiFonts.Clear();
            }
        }
    }
}
