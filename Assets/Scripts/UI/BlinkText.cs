using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 텍스트 깜빡임 효과
/// </summary>
public class BlinkText : MonoBehaviour
{
    public float speed = 2f;
    public float minAlpha = 0.2f;

    private Text _text;
    private float _timer;

    private void Awake()
    {
        _text = GetComponent<Text>();
    }

    private void Update()
    {
        if (_text == null) return;
        _timer += Time.deltaTime;
        float alpha = (Mathf.Sin(_timer * speed) + 1f) * 0.5f;
        var c = _text.color;
        c.a = Mathf.Lerp(minAlpha, 1f, alpha);
        _text.color = c;
    }
}
