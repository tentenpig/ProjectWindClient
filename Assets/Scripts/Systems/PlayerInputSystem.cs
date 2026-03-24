using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 플레이어 입력을 읽어 PlayerInput 컴포넌트에 저장
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial struct PlayerInputSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTag>();
    }

    // Input은 메인 스레드에서만 읽을 수 있으므로 BurstCompile 불가
    public void OnUpdate(ref SystemState state)
    {
        float2 input = float2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y -= 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;

        if (math.lengthsq(input) > 0f)
            input = math.normalize(input);

        foreach (var playerInput in SystemAPI.Query<RefRW<PlayerInput>>().WithAll<PlayerTag>())
        {
            playerInput.ValueRW.Move = input;
        }
    }
}
