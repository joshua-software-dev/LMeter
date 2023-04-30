using Dalamud.Game.Gui;
using Dalamud.Plugin;
using LMeter.Config;


namespace LMeter.Act;

public class ActClient
{
    private readonly ActConfig _config;
    private readonly ChatGui _chatGui;
    private readonly DalamudPluginInterface _dpi;

    public IActClient Current;

    public ActClient(ChatGui chatGui, ActConfig config, DalamudPluginInterface dpi)
    {
        _chatGui = chatGui;
        _config = config;
        _dpi = dpi;

        Current = null!;
        GetNewActClient();
    }

    public void GetNewActClient() 
    {
        Current.Dispose();
        Current = _config.IinactMode
            ? new IinactClient(_chatGui, _config, _dpi)
            : new ActWebSocketClient(_chatGui, _config, _dpi);
    }
}
