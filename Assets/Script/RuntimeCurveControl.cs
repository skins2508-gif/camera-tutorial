using UnityEngine;

/// <summary>5단계에서 굴곡 Material 값을 실시간으로 조절하는 UI입니다.</summary>
public class RuntimeCurveControl : MonoBehaviour
{
    private TutorialCurvedWorld curvedWorld;
    private Font uiFont;
    private bool wasMissionFive;
    private bool verticalAdjusted;
    private bool horizontalAdjusted;
    private float verticalCurve = 0.48f;
    private float horizontalCurve = 0.24f;

    public bool IsSucceeded => verticalAdjusted || horizontalAdjusted;

    public void Initialize(TutorialCurvedWorld world)
    {
        curvedWorld = world;
    }

    private void Start()
    {
        uiFont = Resources.Load<Font>("Fonts/NanumGothic-Regular");
    }

    private void Update()
    {
        bool missionFive = CameraLearningTutorial.CurrentMission == 5;
        if (missionFive && !wasMissionFive)
        {
            verticalAdjusted = false;
            horizontalAdjusted = false;
            if (curvedWorld != null)
            {
                verticalCurve = curvedWorld.CurrentVerticalCurve;
                horizontalCurve = curvedWorld.CurrentHorizontalCurve;
            }
        }
        wasMissionFive = missionFive;
    }

    private void OnGUI()
    {
        if (CameraLearningTutorial.CurrentMission != 5 || curvedWorld == null)
            return;
        if (uiFont != null)
            GUI.skin.font = uiFont;

        const float width = 460f;
        const float height = 190f;
        float x = Screen.width - width - 18f;
        float y = Screen.height - height - 82f;
        GUI.Box(new Rect(x, y, width, height), "CURVE MATERIAL CONTROL");

        GUI.Label(new Rect(x + 22f, y + 36f, width - 44f, 24f),
            $"Vertical Curve   {verticalCurve:0.00}   (-0.80 ~ 0.80)");
        float nextVertical = GUI.HorizontalSlider(
            new Rect(x + 24f, y + 66f, width - 48f, 22f), verticalCurve, -0.8f, 0.8f);

        GUI.Label(new Rect(x + 22f, y + 94f, width - 44f, 24f),
            $"Horizontal Curve   {horizontalCurve:0.00}   (0.00 ~ 0.50)");
        float nextHorizontal = GUI.HorizontalSlider(
            new Rect(x + 24f, y + 124f, width - 48f, 22f), horizontalCurve, 0f, 0.5f);

        if (!Mathf.Approximately(nextVertical, verticalCurve))
        {
            verticalCurve = nextVertical;
            verticalAdjusted = true;
        }
        if (!Mathf.Approximately(nextHorizontal, horizontalCurve))
        {
            horizontalCurve = nextHorizontal;
            horizontalAdjusted = true;
        }
        curvedWorld.SetCurveValues(verticalCurve, horizontalCurve);

        string progress = IsSucceeded
            ? "O  Curve 값을 조절했습니다."
            : $"조절 진행: Vertical {(verticalAdjusted ? "O" : "-")}   Horizontal {(horizontalAdjusted ? "O" : "-")}";
        GUI.Label(new Rect(x + 22f, y + 153f, width - 44f, 25f), progress);
    }
}
