using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 플레이어 엔티티 Authoring 컴포넌트
/// 값은 GameConfig.json에서 로드
/// </summary>
public class PlayerAuthoring : MonoBehaviour
{
    public class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var cfg = GameConfigManager.Config.player;
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // SubScene 내 배치 위치를 초기 타일 좌표로 사용
            var pos = authoring.transform.position;
            int2 startTile = new int2(
                (int)math.floor(pos.x),
                (int)math.floor(pos.y));

            AddComponent<PlayerTag>(entity);
            AddComponent(entity, new PlayerInput { Move = default });
            AddComponent(entity, new Health
            {
                Current = cfg.maxHealth,
                Max = cfg.maxHealth
            });
            AddComponent(entity, new MoveSpeed { Value = cfg.moveSpeed });
            AddComponent(entity, new MoveDirection { Value = default });
            AddComponent(entity, new TilePosition
            {
                Current = startTile,
                Target = startTile,
                Progress = 1f,
                IsMoving = false
            });
            AddComponent(entity, new PlayerLevel
            {
                Level = 1,
                CurrentExp = 0,
                ExpToNextLevel = 15
            });
        }
    }
}
