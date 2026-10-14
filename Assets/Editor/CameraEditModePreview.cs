using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Main Camera가 RenderTexture로 출력 중이어도 Play 중지 상태의 Game 창을 볼 수 있게 합니다.
/// 생성되는 Camera는 Scene과 빌드에 저장되지 않으며 Play 직전에 자동 제거됩니다.
/// </summary>
[InitializeOnLoad]
public static class CameraEditModePreview
{
    private static Camera previewCamera;

    static CameraEditModePreview()
    {
        EditorApplication.update += Refresh;
        EditorApplication.playModeStateChanged += _ => Refresh();
    }

    private static void Refresh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            RemovePreview();
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null || !mainCamera.enabled || mainCamera.targetTexture == null)
        {
            RemovePreview();
            return;
        }

        if (previewCamera == null)
        {
            GameObject previewObject = new GameObject(
                "EDITOR GAME PREVIEW CAMERA - 저장되지 않음",
                typeof(Camera), typeof(UniversalAdditionalCameraData));
            previewObject.hideFlags = HideFlags.HideAndDontSave;
            previewCamera = previewObject.GetComponent<Camera>();
        }

        previewCamera.CopyFrom(mainCamera);
        previewCamera.transform.SetPositionAndRotation(
            mainCamera.transform.position, mainCamera.transform.rotation);
        previewCamera.targetTexture = null;
        previewCamera.targetDisplay = 0;
        previewCamera.enabled = true;

        UniversalAdditionalCameraData previewData =
            previewCamera.GetUniversalAdditionalCameraData();
        previewData.renderType = CameraRenderType.Base;
    }

    private static void RemovePreview()
    {
        if (previewCamera == null)
            return;

        Object.DestroyImmediate(previewCamera.gameObject);
        previewCamera = null;
    }
}
