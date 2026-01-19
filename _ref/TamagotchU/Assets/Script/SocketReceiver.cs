using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

[Serializable]
public class MetaData
{
    public string message;
    public float value;
}
public class TimedEntry
{
    public string message;
    public float value;
    public float timestamp;  // Time.time value when added

    public override string ToString()
    {
        return $"[{timestamp:F2}] {message}: {value}";
    }
}

public class SocketReceiver : MonoBehaviour
{
    public int port = 5005;

    // Received data accessible in Inspector or other scripts
    public string receivedString;
    public float receivedFloat;

    private Thread _listenThread;
    private TcpListener _listener;
    private volatile bool _listening = false;

    // Thread-safe queue to communicate between thread and Update()
    private ConcurrentQueue<MetaData> _dataQueue = new ConcurrentQueue<MetaData>();

    // Time-limited timed entries list
    private List<TimedEntry> playerEmoEntries = new List<TimedEntry>();
    public ReadOnlyCollection<TimedEntry> PlayerEmoEntries
    {
        get { return playerEmoEntries.AsReadOnly(); }
    }

    void Start()
    {
        _listening = true;
        _listenThread = new Thread(ListenForData);
        _listenThread.IsBackground = true;
        _listenThread.Start();
    }

    void OnDestroy()
    {
        _listening = false;
        if (_listener != null)
            _listener.Stop();

        if (_listenThread != null && _listenThread.IsAlive)
            _listenThread.Abort();
    }

    void Update()
    {
        // 1. Dequeue any received data and update fields
        while (_dataQueue.TryDequeue(out MetaData meta))
        {
            receivedString = meta.message;
            receivedFloat = meta.value;
            //Debug.Log($"Received message: {receivedString}, value: {receivedFloat}");

            // add received data
            playerEmoEntries.Add(new TimedEntry
            {
                message = meta.message,
                value = meta.value,
                timestamp = Time.time
            });
        }

        // 2. Remove entries older than 3 seconds
        float now = Time.time;
        playerEmoEntries.RemoveAll(entry => (now - entry.timestamp) > 3.0f);

        // 3. Optional: keep list sorted by timestamp ascending (oldest first)
        // This makes weighted sum easier — oldest at index 0, newest at last index
        playerEmoEntries.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Entries:\n" + string.Join("\n", playerEmoEntries));
        }
    }

    public float GetWeightedSum()
    {
        if (playerEmoEntries.Count == 0)
            return 0f;

        float sum = 0f;
        float totalWeight = 0f;
        int count = playerEmoEntries.Count;

        // Weights: oldest = 1, newest = count (simple linear scale)
        for (int i = 0; i < count; i++)
        {
            int weight = i + 1;
            sum += playerEmoEntries[i].value * weight;
            totalWeight += weight;
        }

        return sum / totalWeight;  // normalize weighted average
    }

    private void ListenForData()
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        Debug.Log($"[SocketReceiver] Listening on port {port}");

        try
        {
            while (_listening)
            {
                if (!_listener.Pending())
                {
                    Thread.Sleep(20);
                    continue;
                }

                TcpClient client = _listener.AcceptTcpClient();

                using (NetworkStream stream = client.GetStream())
                {
                    try
                    {
                        while (client.Connected && _listening)
                        {
                            // Read 4 bytes length (big endian)
                            byte[] lenBuf = ReadExactly(stream, 4);
                            if (lenBuf == null) break;  // client disconnected

                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(lenBuf);
                            int metaLength = BitConverter.ToInt32(lenBuf, 0);

                            // Read JSON metadata bytes
                            byte[] metaBuf = ReadExactly(stream, metaLength);
                            if (metaBuf == null) break;  // client disconnected

                            string metaJson = Encoding.UTF8.GetString(metaBuf);

                            // Deserialize JSON metadata
                            MetaData meta = JsonUtility.FromJson<MetaData>(metaJson);

                            // Enqueue to be processed on main thread
                            _dataQueue.Enqueue(meta);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SocketReceiver] Stream reading exception: {ex}");
                    }
                }

                client.Close();
            }
        }
        catch (ThreadAbortException)
        {
            // Expected on exit, no action needed
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketReceiver] Exception: {ex}");
        }
        finally
        {
            _listener.Stop();
        }
    }


    // Helper method to read exactly 'len' bytes or return null if disconnected
    private byte[] ReadExactly(NetworkStream stream, int len)
    {
        byte[] buffer = new byte[len];
        int read = 0;
        while (read < len)
        {
            int r = stream.Read(buffer, read, len - read);
            if (r == 0) return null; // disconnected
            read += r;
        }
        return buffer;
    }
}
