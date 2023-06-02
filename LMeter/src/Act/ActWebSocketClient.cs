using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using LMeter.Config;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace LMeter.Act;

public enum ConnectionStatus
{
    NotConnected,
    ConnectionFailed,
    Connecting,
    Connected,
    Subscribing,
    Subscribed,
    ShuttingDown
}

public class ActWebSocketClient : ActEventParser, IActClient
{
    private ArraySegment<byte> _buffer;
    private readonly ActConfig _config;
    private ClientWebSocket _socket;
    private CancellationTokenSource _cancellationTokenSource;
    private Task? _receiveTask;
    private readonly ChatGui _chatGui;

    private ConnectionStatus _status;
    private string? _lastErrorMessage;

    public const string SubscriptionMessage = """{"call":"subscribe","events":["CombatData"]}""";

    public ActWebSocketClient(ChatGui chatGui, ActConfig config)
    {
        _chatGui = chatGui;
        _config = config;
        _socket = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();
        _status = ConnectionStatus.NotConnected;
        PastEvents = new List<ActEvent>();
    }

    public bool ClientReady() =>
        _status == ConnectionStatus.Connected;

    public bool ConnectionIncompleteOrFailed() =>
        _status == ConnectionStatus.NotConnected || _status == ConnectionStatus.ConnectionFailed;

    public void DrawConnectionStatus()
    {
        ImGui.Text($"ACT Status: {_status}");

        if
        (
            _status != ConnectionStatus.ConnectionFailed &&
            _status != ConnectionStatus.Connecting &&
            _status != ConnectionStatus.Connected &&
            _status != ConnectionStatus.Subscribing &&
            _status != ConnectionStatus.Subscribed
        )
        {
            return;
        }

        ImGui.SameLine();
        ImGui.Text
        (
            _status switch
            {
                ConnectionStatus.ConnectionFailed => "0/4",
                ConnectionStatus.Connecting => "1/4",
                ConnectionStatus.Connected => "2/4",
                ConnectionStatus.Subscribing => "3/4",
                ConnectionStatus.Subscribed => "4/4",
                _ => throw new ArgumentOutOfRangeException()
            }
        );

        if (_status == ConnectionStatus.ConnectionFailed)
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(_lastErrorMessage);
            }
        }

        var fontScale = ImGui.GetIO().FontGlobalScale;
        var failColor = ImGui.GetColorU32(ImGuiColors.DalamudRed);
        var loadingColor = ImGui.GetColorU32(ImGuiColors.DalamudGrey);
        var successColor = ImGui.GetColorU32(ImGuiColors.DalamudGrey3);

        ImGui.BeginTable("ACT connection status", 4, ImGuiTableFlags.Borders);
        ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.None, 25 * fontScale);
        ImGui.TableSetupColumn("2", ImGuiTableColumnFlags.None, 25 * fontScale);
        ImGui.TableSetupColumn("3", ImGuiTableColumnFlags.None, 25 * fontScale);
        ImGui.TableSetupColumn("4", ImGuiTableColumnFlags.None, 25 * fontScale);
        ImGui.TableNextRow();

        for (var i = 2; i < 6; i++)
        {
            var iterStatus = (ConnectionStatus) i;
            ImGui.TableNextColumn();
            if (_status == ConnectionStatus.ConnectionFailed)
            {
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, failColor);
                ImGui.Text(iterStatus.ToString());
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text("");
                ImGui.PopFont();
            }
            else if ((int) _status > i || _status == ConnectionStatus.Subscribed)
            {
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, successColor);
                ImGui.Text(iterStatus.ToString());
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text("");
                ImGui.PopFont();
            }
            else if ((int) _status == i)
            {
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, loadingColor);
                ImGui.Text(iterStatus.ToString());
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text("");
                ImGui.PopFont();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip
                (
                    iterStatus switch
                    {
                        ConnectionStatus.Connecting =>
                            """
                            LMeter attempts to connect using the ACT WebSocket protocol,
                            if it fails the connection attempt ends.
                            """,
                        ConnectionStatus.Connected =>
                            """
                            LMeter successfully connected to a conforming ACT client.
                            While connection was established, in this state no data
                            will be sent until LMeter explicitly requests for that data.
                            """,
                        ConnectionStatus.Subscribing =>
                            """
                            LMeter sends a second message to the ACT client requesting
                            that all "CombatEvent" data be sent whenever the ACT client
                            generates such an event.
                            """,
                        ConnectionStatus.Subscribed =>
                            """
                            LMeter successfully sent a message to the ACT Client
                            requesting "CombatEvent" data. The connection did not
                            terminate on this request, however ACT clients do not reply
                            on successful subscription state change.
                            """,
                        _ => throw new ArgumentOutOfRangeException()
                    }
                );
            }
        }

        ImGui.EndTable();
    }

    public ActEvent? GetEvent(int index = -1)
    {
        if (index >= 0 && index < PastEvents.Count)
        {
            return PastEvents[index];
        }

        return LastEvent;
    }

    public void EndEncounter()
    {
        var message = new XivChatEntry
        {
            Message = "end",
            Type = XivChatType.Echo
        };

        _chatGui.PrintChat(message);
    }

    public void Clear()
    {
        LastEvent = null;
        PastEvents = new List<ActEvent>();
        if (_config.ClearAct)
        {
            var message = new XivChatEntry
            {
                Message = "clear",
                Type = XivChatType.Echo
            };

            _chatGui.PrintChat(message);
        }
    }

    public void RetryConnection()
    {
        Reset();
        Start();
    }

    public void Start()
    {
        if (_status != ConnectionStatus.NotConnected)
        {
            PluginLog.Error("Cannot start, ActWebSocketClient needs to be reset!");
            return;
        }
        else if (_config.WaitForCharacterLogin)
        {
            if (!PluginManager.Instance?.ClientState.IsLoggedIn ?? true)
            {
                PluginLog.Error("Cannot start, player is not logged in.");
                return;
            }
        }

        try
        {
            _receiveTask = Task.Run(() => this.Connect(_config.ActSocketAddress));
        }
        catch (Exception ex)
        {
            _status = ConnectionStatus.ConnectionFailed;
            _lastErrorMessage = ex.ToString();
            this.LogConnectionFailure(_lastErrorMessage);
        }
    }

    private async Task Connect(string host)
    {
        try
        {
            _status = ConnectionStatus.Connecting;
            await _socket.ConnectAsync(new Uri(host), _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _status = ConnectionStatus.ConnectionFailed;
            _lastErrorMessage = ex.ToString();
            this.LogConnectionFailure(_lastErrorMessage);
            return;
        }

        _buffer = new ArraySegment<byte>(new byte[4096]);
        if (_buffer.Array is null)
        {
            _status = ConnectionStatus.ConnectionFailed;
            this.LogConnectionFailure("Failed to allocate receive buffer!");
            return;
        }

        _status = ConnectionStatus.Connected;
        PluginLog.Information("Successfully Established ACT Connection");

        try
        {
            _status = ConnectionStatus.Subscribing;
            await _socket.SendAsync
            (
                Encoding.UTF8.GetBytes(SubscriptionMessage),
                WebSocketMessageType.Text,
                endOfMessage: true,
                _cancellationTokenSource.Token
            );
        }
        catch (Exception ex)
        {
            _status = ConnectionStatus.ConnectionFailed;
            _lastErrorMessage = ex.ToString();
            this.LogConnectionFailure(_lastErrorMessage);
            return;
        }

        _status = ConnectionStatus.Subscribed;
        PluginLog.Information("Successfully subscribed to ACT");

        await ReceiveMessages();
    }

    private async Task ReceiveMessages()
    {
        if (_buffer.Array is null)
        {
            throw new NullReferenceException();
        }

        try
        {
            while (_status == ConnectionStatus.Subscribed)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(_buffer, _cancellationTokenSource.Token);
                    ms.Write(_buffer.Array, _buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms, Encoding.UTF8);
                var data = await reader.ReadToEndAsync();
                PluginLog.Verbose(data);
                if (string.IsNullOrEmpty(data)) continue;

                try
                {
                    var newEvent = JsonConvert.DeserializeObject<ActEvent?>(data);
                    this.ParseNewEvent(newEvent, _config.EncounterHistorySize);
                }
                catch (Exception ex)
                {
                    this.LogConnectionFailure(ex.ToString());
                }
            }
        }
        catch
        {
            // Swallow exception in case something weird happens during shutdown
        }
        finally
        {
            if (_status != ConnectionStatus.ShuttingDown)
            {
                this.Shutdown();
            }
        }
    }

    public void Shutdown()
    {
        _status = ConnectionStatus.ShuttingDown;
        LastEvent = null;
        if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.Connecting)
        {
            try
            {
                // Close the websocket
                _socket
                    .CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            catch
            {
                // If closing the socket failed, force it with the cancellation token.
                _cancellationTokenSource.Cancel();
            }

            // TODO: Replace this whole thing with a ThreadPool version
            _receiveTask?.GetAwaiter().GetResult();
            PluginLog.Information($"Closed ACT Connection");
        }

        _socket.Dispose();
        _status = ConnectionStatus.NotConnected;
    }

    public void Reset()
    {
        this.Shutdown();
        _socket = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();
        _status = ConnectionStatus.NotConnected;
    }

    private void LogConnectionFailure(string error)
    {
        PluginLog.Debug($"Failed to connect to ACT!");
        PluginLog.Verbose(error);
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
            this.Shutdown();
        }
    }
}
