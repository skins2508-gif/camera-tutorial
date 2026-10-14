using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 실습 씬을 원본 게임과 분리하고, 백업/복구하는 안전장치입니다.
/// </summary>
public static class CameraTutorialWorkspace
{
    private const string SourceScene = "Assets/Scenes/CurvedTilemapExample.unity";
    private const string Folder = "Assets/CameraTutorial";
    private const string ScenesFolder = Folder + "/Scenes";
    private const string BackupFolder = Folder + "/Backups";
    private const string TemplateScene = ScenesFolder + "/CameraTutorialTemplate.unity";
    private const string WorkingScene = ScenesFolder + "/CameraTutorial_Working.unity";
    private const string CheckpointScene = BackupFolder + "/CameraTutorialCheckpoint.unity";

    [MenuItem("Tools/Camera Tutorial/1. Create or Open Tutorial")]
    public static void CreateOrOpen()
    {
        EnsureFolder();
        EnsureTilemapLearningAssets();
        if (!File.Exists(TemplateScene))
            AssetDatabase.CopyAsset(SourceScene, TemplateScene);
        if (!File.Exists(WorkingScene))
            AssetDatabase.CopyAsset(TemplateScene, WorkingScene);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(WorkingScene);
        OrganizeHierarchy();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("카메라 튜토리얼 연습용 씬을 열었습니다: " + WorkingScene);
    }

    [MenuItem("Tools/Camera Tutorial/2. Save Checkpoint")]
    public static void SaveCheckpoint()
    {
        EnsureFolder();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.DeleteAsset(CheckpointScene);
        AssetDatabase.CopyAsset(WorkingScene, CheckpointScene);
        AssetDatabase.Refresh();
        Debug.Log("현재 연습 상태를 체크포인트로 저장했습니다.");
    }

    [MenuItem("Tools/Camera Tutorial/3. Restore Checkpoint")]
    public static void RestoreCheckpoint()
    {
        if (!File.Exists(CheckpointScene))
        {
            EditorUtility.DisplayDialog("체크포인트 없음", "먼저 Save Checkpoint를 실행하세요.", "확인");
            return;
        }
        if (!EditorUtility.DisplayDialog("체크포인트 복원", "현재 연습용 씬 변경 내용이 사라집니다.", "복원", "취소"))
            return;
        ReplaceWorkingScene(CheckpointScene);
    }

    [MenuItem("Tools/Camera Tutorial/4. Reset Working Scene")]
    public static void ResetWorkingScene()
    {
        EnsureFolder();
        if (!File.Exists(TemplateScene))
            AssetDatabase.CopyAsset(SourceScene, TemplateScene);
        if (!EditorUtility.DisplayDialog("튜토리얼 초기화",
            "연습용 CameraTutorial 씬을 최초 상태로 되돌립니다.\n원본 게임 씬은 변경되지 않습니다.",
            "초기화", "취소"))
            return;
        ReplaceWorkingScene(TemplateScene);
        PlayerPrefs.DeleteKey("CameraTutorial.Mission");
        PlayerPrefs.Save();
    }

    private static void ReplaceWorkingScene(string source)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(WorkingScene);
        AssetDatabase.CopyAsset(source, WorkingScene);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(WorkingScene);
        OrganizeHierarchy();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("연습용 씬 복원 완료: " + WorkingScene);
    }

    private static void EnsureFolder()
    {
        Directory.CreateDirectory(ScenesFolder);
        Directory.CreateDirectory(BackupFolder);
        Directory.CreateDirectory(Folder + "/Docs");
        AssetDatabase.Refresh();
    }

    private static void EnsureTilemapLearningAssets()
    {
        const string targetFolder = Folder + "/01_Tilemap/RequiredAssets";
        Directory.CreateDirectory(targetFolder);
        AssetDatabase.Refresh();

        const string sourcePng = "Assets/PixelTilesForSideScroller/Sprites/Rock/Rock1.png";
        const string targetPng = targetFolder + "/Rock1.png";
        const string targetTile = targetFolder + "/Rock1_Tutorial.asset";

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(targetPng) == null)
        {
            AssetDatabase.CopyAsset(sourcePng, targetPng);
            AssetDatabase.ImportAsset(targetPng, ImportAssetOptions.ForceSynchronousImport);
        }

        if (AssetDatabase.LoadAssetAtPath<TileBase>(targetTile) == null)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(targetPng);
            if (sprite == null)
            {
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(targetPng))
                    if (asset is Sprite found) { sprite = found; break; }
            }

            if (sprite != null)
            {
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Sprite;
                AssetDatabase.CreateAsset(tile, targetTile);
            }
        }
        AssetDatabase.SaveAssets();
    }

    private static void OrganizeHierarchy()
    {
        GameObject guide = FindOrCreateRoot("[01] GUIDE - 미션 설명");
        GameObject cameras = FindOrCreateRoot("[02] CAMERAS - 카메라 실습");
        GameObject level = FindOrCreateRoot("[03] LEVEL - Grid와 Tilemap");
        GameObject player = FindOrCreateRoot("[04] PLAYER - 캐릭터와 물리");
        FindOrCreateRoot("[05] RUNTIME OUTPUT - Play 중 자동 생성");

        Camera mainCamera = Object.FindFirstObjectByType<Camera>();
        if (mainCamera != null && mainCamera.transform.parent == null)
            mainCamera.transform.SetParent(cameras.transform, true);

        Grid grid = Object.FindFirstObjectByType<Grid>();
        if (grid != null && grid.transform.parent == null)
            grid.transform.SetParent(level.transform, true);

        PlayerController controller = Object.FindFirstObjectByType<PlayerController>();
        if (controller != null && controller.transform.parent == null)
            controller.transform.SetParent(player.transform, true);

        GameObject oldGuide = GameObject.Find("00_READ_ME - Hierarchy 이름을 순서대로 확인하세요");
        if (oldGuide != null && oldGuide.transform.parent == null)
            oldGuide.transform.SetParent(guide.transform, true);

        ConfigureLearningLevel();
    }

    private static void ConfigureLearningLevel()
    {
        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Tilemap ground = null;

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap.CompareTag("Ground"))
            {
                ground = tilemap;
                continue;
            }

            if (tilemap.name.Contains("Hazard") || tilemap.name.Contains("가시"))
            {
                tilemap.ClearAllTiles();
                tilemap.name = "Hazards Tilemap - 비어 있음 (교육용)";
                TilemapCollider2D hazardCollider = tilemap.GetComponent<TilemapCollider2D>();
                if (hazardCollider != null)
                    hazardCollider.enabled = false;
            }
        }

        if (ground == null)
            return;

        TileBase stone = AssetDatabase.LoadAssetAtPath<TileBase>(
            "Assets/PixelTilesForSideScroller/TileAssets/Rock/Rock1.asset");
        if (stone == null)
            return;

        // 오른쪽으로 이어지는 완만한 교육용 길입니다. 한 번에 한 칸만 높이가 변합니다.
        int[] heights =
        {
            1,1,1,1,1, 2,2,2,2,2, 2,2,2,2,2,
            1,1,1,1,1, 0,0,0,0,0, 0,0,0,0,0,
            1,1,1,1,1, 2,2,2,2,2, 1,1,1,1,1
        };

        for (int i = 0; i < heights.Length; i++)
        {
            int x = 19 + i;
            int top = heights[i];
            ground.SetTile(new Vector3Int(x, top, 0), stone);
            ground.SetTile(new Vector3Int(x, top - 1, 0), stone);
        }

        // 넓은 공간에 짧은 보조 발판만 추가합니다. 진행 길을 완전히 막지 않습니다.
        for (int x = 25; x <= 27; x++) ground.SetTile(new Vector3Int(x, 5, 0), stone);
        for (int x = 34; x <= 37; x++) ground.SetTile(new Vector3Int(x, 4, 0), stone);
        for (int x = 46; x <= 49; x++) ground.SetTile(new Vector3Int(x, 5, 0), stone);

        ground.CompressBounds();
        ground.RefreshAllTiles();
        TilemapCollider2D groundCollider = ground.GetComponent<TilemapCollider2D>();
        if (groundCollider != null)
            groundCollider.ProcessTilemapChanges();
        EditorUtility.SetDirty(ground);
    }

    private static GameObject FindOrCreateRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        return existing != null ? existing : new GameObject(name);
    }
}
