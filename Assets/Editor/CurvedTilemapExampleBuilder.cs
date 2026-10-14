using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class CurvedTilemapExampleBuilder
{
    private const string ScenePath = "Assets/Scenes/CurvedTilemapExample.unity";
    private const int BuilderVersion = 5;
    private const string BuilderVersionKey = "CurvedTilemapExampleBuilder.Version";

    [InitializeOnLoadMethod]
    private static void CreateOnceAfterImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null ||
                EditorPrefs.GetInt(BuilderVersionKey, 0) < BuilderVersion)
            {
                Generate();
                EditorPrefs.SetInt(BuilderVersionKey, BuilderVersion);
            }
        };
    }

    [MenuItem("Tools/Examples/Create Curved Tilemap Scene")]
    public static void Generate()
    {
        // 현재 작업 중인 씬을 건드리지 않도록 예제 씬을 Additive로 만든 뒤 저장하고 닫습니다.
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Scene previousActiveScene = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(scene);

        CreateCamera();
        Grid grid = new GameObject("GRID - 타일은 이 격자에 맞춰 배치").AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        Tilemap ground = CreateTilemap(grid.transform, "01_Ground - 계단식 곡선 바닥", "Ground");
        Tilemap hazards = CreateTilemap(grid.transform, "02_Hazards - 가시 전용", "Untagged", false);
        hazards.gameObject.AddComponent<HazardRespawn>();

        TileBase stone = AssetDatabase.LoadAssetAtPath<TileBase>(
            "Assets/PixelTilesForSideScroller/TileAssets/Rock/Rock1.asset");
        if (stone == null)
            throw new System.InvalidOperationException("예제에 필요한 Rock1 타일을 찾지 못했습니다.");

        // 핵심: 타일을 회전시키지 않고, 2~3칸마다 높이를 한 칸씩 바꿔 곡선처럼 보이게 합니다.
        var surface = new Dictionary<int, int>
        {
            {-18,-7},{-17,-7},{-16,-7},{-15,-7},
            {-14,-6},{-13,-6},{-12,-5},{-11,-5},
            {-10,-4},{-9,-4},{-8,-3},{-7,-3},
            {-6,-2},{-5,-2},{-4,-1},{-3,-1},{-2,-1},{-1,-1},
            {0,0},{1,0},{2,0},{3,0},{4,1},{5,1},
            {6,2},{7,2},{8,3},{9,3},{10,3},
            {11,2},{12,2},{13,1},{14,1},{15,0},{16,0},{17,0},{18,1}
        };

        foreach (KeyValuePair<int, int> cell in surface)
            FillColumn(ground, stone, cell.Key, cell.Value, 2);

        // 교육용으로 진행을 막지 않는 긴 완만한 길을 추가합니다.
        for (int x = 19; x <= 64; x++)
        {
            int section = (x - 19) / 6;
            int topY = section switch
            {
                0 => 1,
                1 => 2,
                2 => 2,
                3 => 1,
                4 => 0,
                5 => 0,
                6 => 1,
                _ => 2
            };
            FillColumn(ground, stone, x, topY, 2);
        }

        // 곡선 길을 읽기 쉽게 만드는 보조 발판과 수직 기둥.
        FillRect(ground, stone, -11, -9, -1, 0);
        FillRect(ground, stone, -3, -2, 3, 3);
        FillRect(ground, stone, 3, 4, 5, 5);
        FillRect(ground, stone, 9, 9, -3, 1);
        FillRect(ground, stone, 14, 14, 3, 7);
        FillRect(ground, stone, 17, 18, 5, 5);

        // 교육용 씬에서는 가시를 생성하지 않습니다.
        hazards.ClearAllTiles();
        hazards.name = "02_Hazards - 비어 있음 (가시 제거됨)";
        TilemapCollider2D hazardCollider = hazards.GetComponent<TilemapCollider2D>();
        if (hazardCollider != null)
            hazardCollider.enabled = false;

        ground.CompressBounds();
        hazards.CompressBounds();
        CreatePlayer();
        CreateGoal();
        new GameObject("04_Game Rules - 낙사, 리스폰, 화면 설명", typeof(CurvedStageGame));
        CreateExplanationObjects();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            SceneManager.SetActiveScene(previousActiveScene);
        EditorSceneManager.CloseScene(scene, true);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Debug.Log($"굴곡형 Tilemap 예제 씬 생성 완료: {ScenePath}");
    }

    [MenuItem("Tools/Examples/Build Curved Tutorial Game")]
    public static void BuildWindowsGame()
    {
        System.IO.Directory.CreateDirectory("Build/CurvedTutorial");
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Build/CurvedTutorial/CurvedTutorial.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Windows 게임 빌드 실패: " + report.summary.result);

        Debug.Log("실행 파일 생성 완료: Build/CurvedTutorial/CurvedTutorial.exe");
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int existing = scenes.FindIndex(item => item.path == ScenePath);
        if (existing >= 0)
            scenes[existing] = new EditorBuildSettingsScene(ScenePath, true);
        else
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Tilemap CreateTilemap(Transform parent, string name, string tag, bool collision = true)
    {
        GameObject go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        go.tag = tag;
        Tilemap map = go.GetComponent<Tilemap>();

        if (collision)
        {
            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            CompositeCollider2D composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            TilemapCollider2D tileCollider = go.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        }
        else
        {
            TilemapCollider2D trigger = go.AddComponent<TilemapCollider2D>();
            trigger.isTrigger = true;
        }
        return map;
    }

    private static void FillColumn(Tilemap map, TileBase tile, int x, int topY, int depth)
    {
        for (int y = topY; y > topY - depth; y--)
            map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static void FillRect(Tilemap map, TileBase tile, int minX, int maxX, int minY, int maxY)
    {
        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static void CreateCamera()
    {
        GameObject go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        Camera camera = go.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10.5f;
        camera.backgroundColor = Color.black;
        camera.clearFlags = CameraClearFlags.SolidColor;
        go.transform.position = new Vector3(0, 0.5f, -10);
    }

    private static void CreatePlayer()
    {
        Sprite sprite = System.Array.Find(AssetDatabase.LoadAllAssetsAtPath(
            "Assets/image/1782477308-removebg-preview.png"), asset => asset is Sprite) as Sprite;
        GameObject player = new GameObject("03_Player - 실제 캐릭터", typeof(SpriteRenderer),
            typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerController));
        player.transform.position = new Vector3(-17, -6.45f, 0);
        player.transform.localScale = Vector3.one * 0.18f;
        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 10;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        playerCollider.size = new Vector2(2.5f, 4.4f);
        playerCollider.offset = Vector2.zero;
        PlayerController controller = player.GetComponent<PlayerController>();
        controller.moveSpeed = 5f;
        controller.jumpPower = 7f;
        controller.maxJump = 2;
    }

    private static void CreateGoal()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/image/16x16 흰색 이미지.png");
        GameObject goal = new GameObject("05_GOAL - 청록색 도착점", typeof(SpriteRenderer),
            typeof(BoxCollider2D), typeof(StageGoal));
        goal.transform.position = new Vector3(17.5f, 6.5f, 0);
        goal.transform.localScale = new Vector3(0.05f, 0.12f, 1f);
        SpriteRenderer renderer = goal.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.1f, 1f, 0.8f);
        BoxCollider2D trigger = goal.GetComponent<BoxCollider2D>();
        trigger.size = new Vector2(10f, 12f);
        trigger.isTrigger = true;
    }

    private static void CreateExplanationObjects()
    {
        GameObject guide = new GameObject("00_READ_ME - Hierarchy 이름을 순서대로 확인하세요");
        new GameObject("A. 바닥은 2~3칸 진행 후 높이를 1칸 변경").transform.SetParent(guide.transform);
        new GameObject("B. 아래를 2칸 채워 벽 두께 만들기").transform.SetParent(guide.transform);
        new GameObject("C. Ground와 Hazards는 Tilemap을 분리").transform.SetParent(guide.transform);
        new GameObject("D. Tools > Examples 메뉴로 언제든 다시 생성").transform.SetParent(guide.transform);
    }
}
