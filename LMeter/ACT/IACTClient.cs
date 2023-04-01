using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using LMeter.Config;
using LMeter.Helpers;
using Newtonsoft.Json;


namespace LMeter.ACT
{
    public enum ConnectionStatus
    {
        NotConnected,
        Connected,
        ShuttingDown,
        Connecting,
        ConnectionFailed
    }
    
    public interface IACTClient : IPluginDisposable
    {
        public static IACTClient Current => Singletons.Get<LMeterConfig>().ACTConfig.IINACTMode 
                                                ? Singletons.Get<IINACTClient>()
                                                : Singletons.Get<ACTClient>();
        public ConnectionStatus Status { get; }
        public List<ACTEvent> PastEvents { get; }

        public void Clear();
        public void EndEncounter();
        public ACTEvent? GetEvent(int index = -1);
        public void Start();
        public void RetryConnection(string address);
    }
}
