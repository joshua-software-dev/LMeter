using LMeter.Helpers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;


namespace LMeter.Config;

[JsonObject]
public class LMeterConfig : IConfigurable, IDisposable
{
    public string Name
    {
        get => $"LMeter v{this.Version}";
        set {}
    }

    public string? Version
    {
        get => Plugin.Version;
        set {}
    }

    public bool FirstLoad = true;

    public MeterListConfig MeterList { get; init; }

    private ActConfig _actConfig = null!;
    public ActConfig ActConfig
    {
        get => _actConfig;
        init
        {
            if (value is ACTConfig oldTypeName)
            {
                // I HATE THIS, but I cannot find any way in C# to actually convert an object to a parent type, in any
                // other way than creating a brand new object that happens to share every value with a different type.
                // So if I don't want to manually maintain mappings from now until the end of time whenever the parent
                // class changes for any reason, I am FORCED to ensure the class is serializable. This is because, C#
                // does not offer any generic way to simply deep copy an object, without FUCKING SERIALIZING IT! In
                // this case, being serializable is a required feature anyway for these config objects, so whatever,
                // eat all my performance and memory why dontcha?
                var tempConf = JsonConvert.DeserializeObject<ActConfig>(JsonConvert.SerializeObject(oldTypeName));
                if (tempConf != null)
                {
                    _actConfig = tempConf;
                    return;
                }
            }

            _actConfig = value;
        }
    }

    public CactbotConfig CactbotConfig { get; init; }

    public FontConfig FontConfig { get; init; }

    [JsonIgnore]
    private AboutPage AboutPage { get; } = new ();

    public LMeterConfig()
    {
        this.MeterList = new MeterListConfig();
        this.ActConfig = new ActConfig();
        this.FontConfig = new FontConfig();
        this.CactbotConfig = new CactbotConfig();
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
            ConfigHelpers.SaveConfig(this);
        }
    }

    public IEnumerable<IConfigPage> GetConfigPages()
    {
        yield return this.MeterList;
        yield return this.ActConfig;
        yield return this.FontConfig;
        yield return this.CactbotConfig;
        yield return this.AboutPage;
    }

    public void ApplyConfig()
    {
        this.CactbotConfig.SetNewCactbotUrl(forceStart: false);
    }

    public void ImportPage(IConfigPage page) { }
}
