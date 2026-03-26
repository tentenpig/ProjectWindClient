/// <summary>
/// 클라이언트-서버 패킷 타입 정의 (서버 Shared.Protocol.PacketType과 동일)
/// </summary>
public enum PacketType : ushort
{
    // 연결
    C_Login = 1000,
    S_Login = 1001,

    // 이동
    C_Move = 2000,
    S_Move = 2001,
    S_Spawn = 2002,
    S_Despawn = 2003,

    // 채팅
    C_Chat = 3000,
    S_Chat = 3001,
}
