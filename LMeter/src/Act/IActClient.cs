using System.Collections.Generic;
using System;


namespace LMeter.Act;

public interface IActClient : IDisposable
{
    public List<ActEvent> PastEvents { get; set; }

    public void Clear();
    public bool ClientReady();
    public bool ConnectionIncompleteOrFailed();
    public void DrawConnectionStatus();
    public void EndEncounter();
    public ActEvent? GetEvent(int index = -1);
    public void Start();
    public void RetryConnection();
}
