using UnityEngine;

/// <summary>
/// 4단계 학습용 실행 코드입니다.
/// 3단계에서 준비한 TutorialRenderTexture를 사용해
/// Screen Quad와 Distortion Camera의 최종 출력 구조를 완성합니다.
/// </summary>
[ExecuteAlways]
public class Step04_ScreenOutput : MonoBehaviour
{
    private const string EditCameraName = "DISTORTION CAMERA - Display 1 최종 출력";
    private const string EditQuadName = "SCREEN QUAD - RenderTexture 화면판";

    private void OnEnable()
    {
        if (!Application.isPlaying)
            RemovePremadeEditObjects();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            RemovePremadeEditObjects();
            return;
        }

        if (CameraLearningTutorial.CurrentMission != 4)
            return;

        // TutorialCurvedWorld가 다음을 자동 연결합니다.
        // Main Camera → TutorialRenderTexture → Curved Screen
        // → Distortion Camera → Display 1
        TutorialCurvedWorld.Instance?.SetLesson(4);
        enabled = false;
    }

    private void RemovePremadeEditObjects()
    {
        RemoveChild(EditCameraName);
        RemoveChild(EditQuadName);
    }

    private void RemoveChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
            DestroyImmediate(child.gameObject);
    }
}
