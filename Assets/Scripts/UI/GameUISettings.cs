using UnityEngine;

/// <summary>
/// UI 프리팹 참조를 보관하는 ScriptableObject
/// Assets/Resources/GameUISettings.asset 에 위치해야 함
/// </summary>
[CreateAssetMenu(fileName = "GameUISettings", menuName = "Game/UI Settings")]
public class GameUISettings : ScriptableObject
{
    public GameObject titlePanelPrefab;
    public GameObject gameOverPanelPrefab;
}
