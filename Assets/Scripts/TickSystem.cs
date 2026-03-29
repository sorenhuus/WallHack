using UnityEngine;
using UnityEngine.UI;

public class TickSystem : MonoBehaviour
{
    public static TickSystem Instance { get; private set; }

    [SerializeField] private int tickRate = 64;
    public int TickRate => tickRate;
    public float TickInterval => 1f / tickRate;

    private int _tick;
    public int Tick => _tick;

    private Text _tickText;

    private void Awake()
    {
        Instance = this;
        Time.fixedDeltaTime = 1f / tickRate;
        CreateTickUI();
    }

    private void CreateTickUI()
    {
        Canvas canvas = new GameObject("TickUI").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.gameObject.AddComponent<CanvasScaler>();
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("TickText");
        textObj.transform.SetParent(canvas.transform, false);
        _tickText = textObj.AddComponent<Text>();
        _tickText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _tickText.fontSize = 20;
        _tickText.color = Color.white;

        RectTransform rt = _tickText.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -10);
        rt.sizeDelta = new Vector2(200, 30);
    }

    private void FixedUpdate()
    {
        _tick++;
        _tickText.text = $"Tick: {_tick}";
    }
}
