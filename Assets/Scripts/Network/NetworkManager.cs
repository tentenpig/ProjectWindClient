using System;
using System.Collections.Concurrent;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 네트워크 연결 및 패킷 처리를 관리하는 싱글톤
/// 백그라운드 스레드에서 수신된 패킷을 메인 스레드로 디스패치
/// </summary>
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Server Settings")]
    public string host = "127.0.0.1";
    public int port = 7777;

    public NetworkClient Client { get; private set; }
    public bool IsConnected => Client != null && Client.IsConnected;
    public int MyPlayerId { get; set; }
    public int2 SpawnPosition { get; set; }

    // 서버 → 클라이언트 이벤트
    public event Action<S_Login> OnLoginResponse;
    public event Action<S_Move> OnPlayerMoved;
    public event Action<S_Spawn> OnPlayerSpawned;
    public event Action<S_Despawn> OnPlayerDespawned;
    public event Action OnDisconnected;

    private readonly ConcurrentQueue<Action> _mainThreadQueue = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Client = new NetworkClient();
        Client.OnPacketReceived += HandlePacket;
        Client.OnDisconnected += () => EnqueueMainThread(() => OnDisconnected?.Invoke());
    }

    private void Update()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            action();
        }
    }

    private void OnDestroy()
    {
        Client?.Disconnect();
    }

    public async void Connect(Action<bool> onResult = null)
    {
        var success = await Client.ConnectAsync(host, port);
        EnqueueMainThread(() => onResult?.Invoke(success));
    }

    public void SendLogin(string accountName)
    {
        Client.Send(PacketType.C_Login, new C_Login { AccountName = accountName });
    }

    public void SendMove(int x, int y, int dirX, int dirY)
    {
        Client.Send(PacketType.C_Move, new C_Move
        {
            Position = new Vec2Int(x, y),
            Direction = new Vec2Int(dirX, dirY)
        });
    }

    private void HandlePacket(ushort type, string json)
    {
        switch ((PacketType)type)
        {
            case PacketType.S_Login:
                var login = JsonUtility.FromJson<S_Login>(json);
                EnqueueMainThread(() =>
                {
                    MyPlayerId = login.PlayerId;
                    SpawnPosition = new int2(login.Position.X, login.Position.Y);
                    OnLoginResponse?.Invoke(login);
                });
                break;

            case PacketType.S_Move:
                var move = JsonUtility.FromJson<S_Move>(json);
                EnqueueMainThread(() => OnPlayerMoved?.Invoke(move));
                break;

            case PacketType.S_Spawn:
                var spawn = JsonUtility.FromJson<S_Spawn>(json);
                EnqueueMainThread(() => OnPlayerSpawned?.Invoke(spawn));
                break;

            case PacketType.S_Despawn:
                var despawn = JsonUtility.FromJson<S_Despawn>(json);
                EnqueueMainThread(() => OnPlayerDespawned?.Invoke(despawn));
                break;

            default:
                Debug.LogWarning($"[NetworkManager] Unknown packet type: {type}");
                break;
        }
    }

    private void EnqueueMainThread(Action action)
    {
        _mainThreadQueue.Enqueue(action);
    }
}
