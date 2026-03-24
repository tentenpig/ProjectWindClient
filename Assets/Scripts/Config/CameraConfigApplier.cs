using UnityEngine;

/// <summary>
/// Main Camera에 추가하여 GameConfig.json의 카메라 설정을 적용
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraConfigApplier : MonoBehaviour
{
    private void Start()
    {
        var cam = GetComponent<Camera>();
        cam.orthographicSize = GameConfigManager.Config.camera.orthographicSize;
    }
}
