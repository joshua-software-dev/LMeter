using Dalamud.Game.Gui;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LMeter.Config;


namespace LMeter.Act;

public class ActClient
{
    private readonly ActConfig _config;
    private readonly IChatGui _chatGui;
    private readonly DalamudPluginInterface _dpi;

    public IActClient Current;

    public ActClient(IChatGui chatGui, ActConfig config, DalamudPluginInterface dpi)
    {
        _chatGui = chatGui;
        _config = config;
        _dpi = dpi;

        Current = GetNewActClient();
    }

    public IActClient GetNewActClient() 
    {
        Current?.Dispose();
        return Current = _config.IinactMode
            ? new IinactClient(_chatGui, _config, _dpi)
            : new ActWebSocketClient(_chatGui, _config);
    }
}
