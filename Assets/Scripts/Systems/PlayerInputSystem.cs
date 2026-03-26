using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 플레이어 입력을 읽어 PlayerInput 컴포넌트에 저장 (4방향 단일 축)
/// 동시 입력 시 마지막에 눌린 축 우선
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial struct PlayerInputSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTag>();
        state.RequireForUpdate<GamePlaying>();
    }

    // Input은 메인 스레드에서만 읽을 수 있으므로 BurstCompile 불가
    public void OnUpdate(ref SystemState state)
    {
        float2 input = float2.zero;

        // 4방향만 허용: 한 축만 선택
        bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        // 세로 입력
        if (up && !down) input.y = 1f;
        else if (down && !up) input.y = -1f;

        // 가로 입력
        if (left && !right) input.x = -1f;
        else if (right && !left) input.x = 1f;

        // 동시 입력 시 한 축만 선택 (세로 우선)
        if (input.x != 0f && input.y != 0f)
            input.x = 0f;

        foreach (var playerInput in SystemAPI.Query<RefRW<PlayerInput>>().WithAll<PlayerTag>())
        {
            playerInput.ValueRW.Move = input;
        }
    }
}
