using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 플레이어 엔티티를 부드럽게 추적하는 카메라 시스템
/// Main Camera에 부착 (CameraConfigApplier와 함께 사용)
/// </summary>
public class CameraFollowSystem : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 8f;

    private EntityManager _em;
    private EntityQuery _playerQuery;
    private bool _initialized;

    private void LateUpdate()
    {
        if (!_initialized)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;
            _em = world.EntityManager;
            _playerQuery = _em.CreateEntityQuery(typeof(PlayerTag), typeof(LocalTransform));
            _initialized = true;
        }

        if (_playerQuery.IsEmpty) return;

        var entity = _playerQuery.GetSingletonEntity();
        var playerPos = _em.GetComponentData<LocalTransform>(entity).Position;

        Vector3 target = new Vector3(playerPos.x, playerPos.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, target, smoothSpeed * Time.deltaTime);
    }
}
