#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils; // Required for Clamp

namespace Lib.io.artnet;

[Guid("fc03dcd0-6f2f-4507-be06-1ed105607489")]
internal sealed class ArtnetInput : Instance<ArtnetInput>, IDisposable, IStatusProvider
{
    private const int ArtNetPort = 6454;
    private static readonly byte[] ArtnetId = "Art-Net\0"u8.ToArray();

    [Output(Guid = "d3c09c87-c508-4621-a54d-f14d85c3f75f", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
    public readonly Slot<List<int>> Result = new();

    public ArtnetInput()
    {
        Result.UpdateAction = Update;
    }

    private void Update(EvaluationContext context)
    {
        var active = Active.GetValue(context);

        // Explicitly check if the listening state needs to change
        if (active != _isListening)
        {
            _isListening = active;
            if (active)
            {
                StartListening();
            }
            else
            {
                StopListening();
            }
        }

        // Cleanup stale universes before assembling the output
        var timeoutInSeconds = Timeout.GetValue(context);
        CleanupStaleUniverses(timeoutInSeconds);

        // Assemble the output list from received data
        var startUniverse = StartUniverse.GetValue(context);
        var numUniverses = NumUniverses.GetValue(context).Clamp(1, 4096); // Increased max

        var combinedDmxData = new List<int>(numUniverses * 512);

        for (var i = 0; i < numUniverses; i++)
        {
            var currentUniverseId = startUniverse + i;
            if (_receivedUniverses.TryGetValue(currentUniverseId, out var universeData))
            {
                // To prevent race conditions with the background thread, we copy the data
                // under a lock or use a thread-safe mechanism. Here, we'll create a snapshot.
                int[] dmxSnapshot = new int[512];
                for (int j = 0; j < 512; ++j)
                {
                    dmxSnapshot[j] = universeData.DmxData[j];
                }
                combinedDmxData.AddRange(dmxSnapshot);
            }
            else
            {
                // If universe data isn't available, fill its slot with zeros
                combinedDmxData.AddRange(Enumerable.Repeat(0, 512));
            }
        }

        Result.Value = combinedDmxData;

        // Update status message for the UI
        UpdateStatusMessage(numUniverses, startUniverse);
    }

    /// <summary>
    /// This is the key function to make the operator "live".
    /// It is called from the background thread to signal that new data has arrived.
    /// </summary>
    private void FlagAsDirty()
    {
        Result.DirtyFlag.Invalidate();
    }

    private void StartListening()
    {
        if (_listenerThread is { IsAlive: true }) return;

        Log.Debug("Starting Art-Net listener...", this);
        _runListener = true;
        _listenerThread = new Thread(ListenLoop)
        {
            IsBackground = true,
            Name = "ArtNetInputListener",
            Priority = ThreadPriority.Normal
        };
        _listenerThread.Start();
    }

    private void StopListening()
    {
        if (_listenerThread == null) return;

        Log.Debug("Stopping Art-Net listener...", this);
        _runListener = false;

        // Closing the UdpClient will cause the blocking Receive() call to throw an exception,
        // which allows the thread to terminate gracefully.
        _udpClient?.Close();
        _listenerThread?.Join(500); // Wait a moment for the thread to exit.
        _listenerThread = null;
    }

    private void ListenLoop()
    {
        try
        {
            // Binding to IPAddress.Any allows receiving broadcasts and unicasts from all network interfaces.
            _udpClient = new UdpClient { ExclusiveAddressUse = false };
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, ArtNetPort));

            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            SetStatus(string.Empty, IStatusProvider.StatusLevel.Success);

            while (_runListener)
            {
                try
                {
                    var data = _udpClient.Receive(ref remoteEndPoint);

                    // Validate Art-DMX packet: ID "Art-Net\0", OpCode 0x5000 (OpDmx)
                    if (data.Length < 18 || !data.AsSpan(0, 8).SequenceEqual(ArtnetId) || data[8] != 0x00 || data[9] != 0x50)
                        continue;

                    var universe = data[14] | (data[15] << 8);
                    var length = (data[16] << 8) | data[17];

                    if (length == 0 || length > 512 || data.Length < 18 + length)
                        continue;

                    var universeData = _receivedUniverses.GetOrAdd(universe, _ => new UniverseData());

                    // Copy DMX data. Buffer.BlockCopy is efficient for this.
                    System.Buffer.BlockCopy(data, 18, universeData.DmxData, 0, length);

                    // Pad with zeros if the received frame is smaller than 512
                    if (length < 512)
                    {
                        Array.Clear(universeData.DmxData, length, 512 - length);
                    }

                    universeData.LastReceivedTicks = Stopwatch.GetTimestamp();

                    // CRITICAL: Tell T3 to re-evaluate this operator on the next frame.
                    FlagAsDirty();
                }
                catch (SocketException)
                {
                    // This exception is expected when StopListening() closes the socket.
                    // The loop terminates because _runListener will be false.
                    break;
                }
                catch (Exception e)
                {
                    if (_runListener)
                    {
                        Log.Error($"Error in Art-Net listen loop: {e.Message}", this);
                        Thread.Sleep(100);
                    }
                }
            }
        }
        catch (Exception e)
        {
            SetStatus($"Failed to bind to port {ArtNetPort}. Another app may be using it. Error: {e.Message}", IStatusProvider.StatusLevel.Error);
            _runListener = false; // Ensure the loop doesn't try to continue
        }
        finally
        {
            _udpClient?.Close();
            Log.Debug("Art-Net listener thread has stopped.", this);
        }
    }

    private void CleanupStaleUniverses(float timeoutInSeconds)
    {
        if (timeoutInSeconds <= 0) return;

        var timeoutTicks = (long)(timeoutInSeconds * Stopwatch.Frequency);
        var nowTicks = Stopwatch.GetTimestamp();

        var staleUniverses = _receivedUniverses
                             .Where(pair => (nowTicks - pair.Value.LastReceivedTicks) > timeoutTicks)
                             .Select(pair => pair.Key)
                             .ToList();

        foreach (var universeId in staleUniverses)
        {
            if (_receivedUniverses.TryRemove(universeId, out _))
            {
                Log.Debug($"Removed stale Art-Net universe {universeId} due to timeout.", this);
                FlagAsDirty(); // Update the view after removing a universe
            }
        }
    }

    private void UpdateStatusMessage(int numUniverses, int startUniverse)
    {
        if (!_isListening)
        {
            SetStatus("Inactive. Turn on 'Active' to start listening.", IStatusProvider.StatusLevel.Notice);
            return;
        }

        if (_lastStatusLevel == IStatusProvider.StatusLevel.Error)
            return; // Don't overwrite a critical error message

        var receivedCount = _receivedUniverses.Count;
        if (receivedCount == 0)
        {
            SetStatus($"Listening on port {ArtNetPort} for {numUniverses} universes (from {startUniverse})... No packets received yet.", IStatusProvider.StatusLevel.Warning);
        }
        else
        {
            SetStatus($"Listening for {numUniverses} universes. Actively receiving data for {receivedCount} universes.", IStatusProvider.StatusLevel.Success);
        }
    }

    // This is called when the operator instance is deleted.
    public void Dispose()
    {
        StopListening();
    }

    private sealed class UniverseData
    {
        public readonly byte[] DmxData = new byte[512];
        public long LastReceivedTicks;
    }

    private Thread? _listenerThread;
    private volatile bool _runListener; // Used to signal the thread to stop
    private bool _isListening; // Reflects the state of the 'Active' parameter
    private UdpClient? _udpClient;
    private readonly ConcurrentDictionary<int, UniverseData> _receivedUniverses = new();

    #region IStatusProvider implementation
    private string _lastStatusMessage = string.Empty;
    private IStatusProvider.StatusLevel _lastStatusLevel;

    private void SetStatus(string message, IStatusProvider.StatusLevel level)
    {
        _lastStatusMessage = message;
        _lastStatusLevel = level;
    }

    public IStatusProvider.StatusLevel GetStatusLevel() => _lastStatusLevel;
    public string GetStatusMessage() => _lastStatusMessage;
    #endregion

    [Input(Guid = "3d085f6f-6f4a-4876-805f-22f25497a731")]
    public readonly InputSlot<bool> Active = new();

    [Input(Guid = "19bde769-3992-4cf0-a0b4-e3ae25c03c79")]
    public readonly InputSlot<int> StartUniverse = new();

    [Input(Guid = "c18a9359-3ef8-4e0d-85d8-51f725357388")]
    public readonly InputSlot<int> NumUniverses = new();

    [Input(Guid = "a38c29b6-057d-4883-9366-139366113b63")]
    public readonly InputSlot<float> Timeout = new();
}