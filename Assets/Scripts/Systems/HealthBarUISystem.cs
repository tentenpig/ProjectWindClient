using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 엔티티 위에 체력바(Slider)를 표시하는 매니지드 시스템
/// - 플레이어: 항상 표시
/// - 적: 체력이 닳은 대상만 표시
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class HealthBarUISystem : SystemBase
{
    private Canvas _canvas;
    private Camera _mainCamera;
    private Dictionary<Entity, RectTransform> _healthBars = new();
    private HashSet<Entity> _activeEntities = new();

    private static readonly Vector2 BarSize = new(80, 10);
    private static readonly float BarOffsetY = 0.8f;

    // 색상
    private static readonly Color PlayerBarColor = new(0.2f, 0.8f, 0.2f);
    private static readonly Color PlayerBarBgColor = new(0.1f, 0.3f, 0.1f);
    private static readonly Color EnemyBarColor = new(0.9f, 0.2f, 0.2f);
    private static readonly Color EnemyBarBgColor = new(0.3f, 0.1f, 0.1f);

    protected override void OnCreate()
    {
        RequireForUpdate<GamePlaying>();
    }

    protected override void OnStartRunning()
    {
        _mainCamera = Camera.main;

        // Screen Space Overlay 캔버스 생성
        var canvasGO = new GameObject("HealthBarCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
    }

    protected override void OnUpdate()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        _activeEntities.Clear();

        // 플레이어 체력바: 항상 표시
        foreach (var (health, transform, entity) in
            SystemAPI.Query<RefRO<Health>, RefRO<LocalTransform>>()
                .WithAll<PlayerTag>()
                .WithEntityAccess())
        {
            _activeEntities.Add(entity);
            UpdateHealthBar(entity, health.ValueRO, transform.ValueRO.Position, true);
        }

        // 적 체력바: 데미지를 입은 적만 표시
        foreach (var (health, transform, entity) in
            SystemAPI.Query<RefRO<Health>, RefRO<LocalTransform>>()
                .WithAll<EnemyTag>()
                .WithEntityAccess())
        {
            if (health.ValueRO.Current < health.ValueRO.Max && health.ValueRO.Current > 0f)
            {
                _activeEntities.Add(entity);
                UpdateHealthBar(entity, health.ValueRO, transform.ValueRO.Position, false);
            }
            else if (_healthBars.ContainsKey(entity))
            {
                RemoveHealthBar(entity);
            }
        }

        // 파괴된 엔티티의 체력바 정리
        var toRemove = new List<Entity>();
        foreach (var kvp in _healthBars)
        {
            if (!_activeEntities.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }
        foreach (var entity in toRemove)
        {
            RemoveHealthBar(entity);
        }
    }

    private void UpdateHealthBar(Entity entity, Health health, float3 worldPos, bool isPlayer)
    {
        if (!_healthBars.TryGetValue(entity, out var barRect))
        {
            barRect = CreateHealthBar(entity, isPlayer);
        }

        // 체력 비율 업데이트
        var fill = barRect.GetChild(1) as RectTransform;
        float ratio = math.clamp(health.Current / health.Max, 0f, 1f);
        fill.anchorMax = new Vector2(ratio, 1f);

        // 월드 좌표를 스크린 좌표로 변환
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(
            new Vector3(worldPos.x, worldPos.y + BarOffsetY, worldPos.z));

        if (screenPos.z > 0)
        {
            barRect.gameObject.SetActive(true);
            barRect.position = screenPos;
        }
        else
        {
            barRect.gameObject.SetActive(false);
        }
    }

    private RectTransform CreateHealthBar(Entity entity, bool isPlayer)
    {
        // 바 컨테이너
        var barGO = new GameObject(isPlayer ? "PlayerHealthBar" : "EnemyHealthBar");
        barGO.transform.SetParent(_canvas.transform, false);
        var barRect = barGO.AddComponent<RectTransform>();
        barRect.sizeDelta = BarSize;
        barRect.pivot = new Vector2(0.5f, 0f);

        // 배경
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(barGO.transform, false);
        var bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = isPlayer ? PlayerBarBgColor : EnemyBarBgColor;

        // 체력 필 (fill)
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barGO.transform, false);
        var fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fillGO.AddComponent<Image>();
        fillImage.color = isPlayer ? PlayerBarColor : EnemyBarColor;

        _healthBars[entity] = barRect;
        return barRect;
    }

    private void RemoveHealthBar(Entity entity)
    {
        if (_healthBars.TryGetValue(entity, out var barRect))
        {
            if (barRect != null)
                Object.Destroy(barRect.gameObject);
            _healthBars.Remove(entity);
        }
    }

    protected override void OnStopRunning()
    {
        foreach (var kvp in _healthBars)
        {
            if (kvp.Value != null)
                Object.Destroy(kvp.Value.gameObject);
        }
        _healthBars.Clear();

        if (_canvas != null)
            Object.Destroy(_canvas.gameObject);
    }
}
