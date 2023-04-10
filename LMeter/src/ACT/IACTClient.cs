using Dalamud.Plugin;
using LMeter.Config;
using LMeter.Helpers;
using System.Collections.Generic;


namespace LMeter.ACT;

public interface IACTClient : IPluginDisposable
{
    public static IACTClient Current => 
        Singletons.Get<LMeterConfig>().ACTConfig.IINACTMode 
            ? Singletons.Get<IINACTClient>() 
            : Singletons.Get<ACTClient>();

    public static IACTClient GetNewClient()
    {
        Singletons.DeleteActClients();

        ACTConfig config = Singletons.Get<LMeterConfig>().ACTConfig;
        DalamudPluginInterface dpi = Singletons.Get<DalamudPluginInterface>();

        IACTClient client = config.IINACTMode
            ? new IINACTClient(config, dpi)
            : new ACTClient(config, dpi);
        Singletons.Register(client);
        return client;
    }

    public string Status { get; }
    public List<ACTEvent> PastEvents { get; }

    public void Clear();
    public bool ClientReady();
    public bool ConnectionIncompleteOrFailed();
    public void EndEncounter();
    public ACTEvent? GetEvent(int index = -1);
    public void Start();
    public void RetryConnection();
}
