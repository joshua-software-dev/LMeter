using Dalamud.Plugin;
using LMeter.Config;
using LMeter.Helpers;


namespace LMeter.Act;

public interface IActClient : IPluginDisposable
{
    public static IActClient Current =>
        Singletons.Get<LMeterConfig>().ActConfig.IinactMode 
            ? Singletons.Get<IinactClient>() 
            : Singletons.Get<ActWebSocketClient>();

    public static IActClient GetNewClient()
    {
        Singletons.DeleteActClients();

        ActConfig config = Singletons.Get<LMeterConfig>().ActConfig;
        DalamudPluginInterface dpi = Singletons.Get<DalamudPluginInterface>();

        IActClient client = config.IinactMode
            ? new IinactClient(config, dpi)
            : new ActWebSocketClient(config, dpi);
        Singletons.Register(client);
        return client;
    }

    public void Clear();
    public bool ClientReady();
    public bool ConnectionIncompleteOrFailed();
    public void DrawConnectionStatus();
    public void EndEncounter();
    public ActEvent? GetEvent(int index = -1);
    public void Start();
    public void RetryConnection();
}
