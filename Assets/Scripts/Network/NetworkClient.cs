using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// TCP 서버와의 네트워크 연결을 관리
/// [2byte PacketType][2byte Length][JSON Payload] 프로토콜
/// </summary>
public class NetworkClient
{
    private TcpClient _tcp;
    private NetworkStream _stream;
    private CancellationTokenSource _cts;

    public bool IsConnected => _tcp != null && _tcp.Connected;

    public event Action<ushort, string> OnPacketReceived;
    public event Action OnDisconnected;

    public async Task<bool> ConnectAsync(string host, int port)
    {
        try
        {
            _tcp = new TcpClient();
            _cts = new CancellationTokenSource();
            await _tcp.ConnectAsync(host, port);
            _stream = _tcp.GetStream();
            Debug.Log($"[NetworkClient] Connected to {host}:{port}");
            _ = ReceiveLoopAsync();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkClient] Connection failed: {ex.Message}");
            return false;
        }
    }

    public void Send<T>(PacketType type, T packet)
    {
        if (!IsConnected) return;

        try
        {
            var json = JsonUtility.ToJson(packet);
            var payload = Encoding.UTF8.GetBytes(json);
            var header = new byte[4];
            BitConverter.GetBytes((ushort)type).CopyTo(header, 0);
            BitConverter.GetBytes((ushort)payload.Length).CopyTo(header, 2);

            lock (_stream)
            {
                _stream.Write(header, 0, 4);
                _stream.Write(payload, 0, payload.Length);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkClient] Send failed: {ex.Message}");
            Disconnect();
        }
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        try { _stream?.Close(); } catch { }
        try { _tcp?.Close(); } catch { }
        _stream = null;
        _tcp = null;
        OnDisconnected?.Invoke();
    }

    private async Task ReceiveLoopAsync()
    {
        var headerBuf = new byte[4];
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                if (!await ReadExactAsync(headerBuf, 4))
                    break;

                ushort packetType = BitConverter.ToUInt16(headerBuf, 0);
                ushort length = BitConverter.ToUInt16(headerBuf, 2);

                var payload = new byte[length];
                if (length > 0 && !await ReadExactAsync(payload, length))
                    break;

                var json = length > 0 ? Encoding.UTF8.GetString(payload) : "";
                OnPacketReceived?.Invoke(packetType, json);
            }
        }
        catch (Exception ex) when (ex is System.IO.IOException or ObjectDisposedException or OperationCanceledException)
        {
            // 연결 끊김
        }
        finally
        {
            Disconnect();
        }
    }

    private async Task<bool> ReadExactAsync(byte[] buffer, int count)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = await _stream.ReadAsync(buffer, offset, count - offset, _cts.Token);
            if (read == 0) return false;
            offset += read;
        }
        return true;
    }
}
