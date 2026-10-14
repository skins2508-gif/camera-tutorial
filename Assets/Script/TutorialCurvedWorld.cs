using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 원작처럼 게임 물리는 평범한 2D Tilemap으로 유지하고,
/// 카메라가 그린 최종 화면에 배럴 왜곡을 적용해 맵 전체를 둥글게 보이게 합니다.
/// </summary>
public class TutorialCurvedWorld : MonoBehaviour
{
    private const float DefaultWorldCameraSize = 10.5f;
    private const int BackgroundLayer = 9;
    private const int PlayerOverlayLayer = 10;
    private const int CurvedScreenLayer = 11;
    public static TutorialCurvedWorld Instance { get; private set; }
    public Vector2 WorldRight => Vector2.right;
    public Vector2 WorldUp => Vector2.up;
    public float CurrentVerticalCurve => distortionMaterial != null
        ? distortionMaterial.GetFloat("_Curve") : 0f;
    public float CurrentHorizontalCurve => distortionMaterial != null
        ? distortionMaterial.GetFloat("_HorizontalWarp") : 0f;

    public void SetCurveValues(float vertical, float horizontal)
    {
        if (distortionMaterial == null)
            return;
        distortionMaterial.SetFloat("_Curve", Mathf.Clamp(vertical, -0.8f, 0.8f));
        distortionMaterial.SetFloat("_HorizontalWarp", Mathf.Clamp(horizontal, 0f, 0.5f));
    }

    public bool IsMissionConfigured(int mission)
    {
        return mission switch
        {
            3 => worldCamera != null && worldTexture != null && worldCamera.targetTexture == worldTexture,
            4 => screenCamera != null && screenCamera.enabled && screenRenderer != null && screenRenderer.enabled,
            5 => distortionMaterial != null && distortionMaterial.GetFloat("_Curve") > 0.001f,
            _ => false
        };
    }

    private Camera worldCamera;
    private Camera screenCamera;
    private Camera playerCamera;
    private RenderTexture worldTexture;
    private bool ownsWorldTexture;
    private Material distortionMaterial;
    private Mesh screenMesh;
    private MeshRenderer screenRenderer;
    private Transform backgroundTransform;
    private Transform playerTransform;
    private PhysicsMaterial2D zeroFrictionMaterial;
    private Transform runtimeRoot;
    private bool manualCameraPractice;

    public static void Install(PlayerController player)
    {
        if (FindFirstObjectByType<TutorialCurvedWorld>() != null)
            return;

        TutorialCurvedWorld effect = player.gameObject.AddComponent<TutorialCurvedWorld>();
        effect.Build();
    }

    private void Build()
    {
        Instance = this;
        playerTransform = transform;
        GameObject runtimeRootObject = GameObject.Find("[05] RUNTIME OUTPUT - Play 중 자동 생성");
        if (runtimeRootObject == null)
            runtimeRootObject = new GameObject("[05] RUNTIME OUTPUT - Play 중 자동 생성");
        runtimeRoot = runtimeRootObject.transform;
        worldCamera = Camera.main;
        if (worldCamera == null)
            return;

        FixGroundCollision();
        CreateHierarchyBackground();

        // Resources 참조로 Shader가 빌드에서 제거되지 않도록 보장합니다.
        Shader shader = Resources.Load<Shader>("BarrelDistortion");
        if (shader == null)
            shader = Shader.Find("Hidden/Tutorial/BarrelDistortion");
        if (shader == null)
        {
            Debug.LogError("BarrelDistortion 셰이더를 찾을 수 없습니다.");
            return;
        }

        // 3단계에서 만든 학습용 에셋을 4단계 최종 출력에도 그대로 사용합니다.
        worldTexture = Resources.Load<RenderTexture>("TutorialRenderTexture");
        if (worldTexture == null)
        {
            worldTexture = new RenderTexture(Screen.width, Screen.height, 24,
                RenderTextureFormat.ARGB32)
            {
                name = "Temporary Tutorial Render Texture",
                filterMode = FilterMode.Bilinear
            };
            worldTexture.Create();
            ownsWorldTexture = true;
        }

        // 기존 카메라는 실제 Tilemap과 플레이어를 RenderTexture에 그립니다.
        worldCamera.targetTexture = worldTexture;
        worldCamera.clearFlags = CameraClearFlags.SolidColor;
        worldCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        worldCamera.cullingMask &= ~((1 << PlayerOverlayLayer) | (1 << CurvedScreenLayer));

        // 플레이어는 굴곡 RenderTexture에서 제외합니다.
        gameObject.layer = PlayerOverlayLayer;

        // 두 번째 카메라는 왜곡된 결과 한 장만 화면에 출력합니다.
        GameObject cameraObject = new GameObject("DISTORTION CAMERA - 최종 굴곡 화면", typeof(Camera));
        cameraObject.transform.SetParent(runtimeRoot, false);
        screenCamera = cameraObject.GetComponent<Camera>();
        screenCamera.orthographic = true;
        screenCamera.orthographicSize = 1f;
        screenCamera.clearFlags = CameraClearFlags.SolidColor;
        screenCamera.backgroundColor = Color.black;
        screenCamera.cullingMask = 1 << CurvedScreenLayer;
        screenCamera.depth = worldCamera.depth + 10f;
        screenCamera.allowHDR = false;
        screenCamera.allowMSAA = false;
        screenCamera.targetTexture = null;
        screenCamera.targetDisplay = 0;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        GameObject screen = new GameObject(
            "Runtime Screen Quad", typeof(MeshFilter), typeof(MeshRenderer));
        screen.name = "CURVED SCREEN - 배럴 왜곡 셰이더";
        screen.transform.SetParent(runtimeRoot, false);
        screen.layer = CurvedScreenLayer;
        screenMesh = CreateScreenQuadMesh();
        screen.GetComponent<MeshFilter>().sharedMesh = screenMesh;
        float aspect = (float)Screen.width / Screen.height;
        screen.transform.localScale = new Vector3(aspect * 2f, 2f, 1f);

        distortionMaterial = new Material(shader);
        distortionMaterial.SetTexture("_MainTex", worldTexture);
        Texture2D background = Resources.Load<Texture2D>("NatBed");
        distortionMaterial.SetTexture("_BackgroundTex", background);
        distortionMaterial.SetFloat("_Curve", 0f);
        distortionMaterial.SetFloat("_HorizontalWarp", 0f);
        screenRenderer = screen.GetComponent<MeshRenderer>();
        screenRenderer.material = distortionMaterial;

        CreateUndistortedPlayerCamera();
        CameraLearningTutorial tutorial = gameObject.AddComponent<CameraLearningTutorial>();
        tutorial.Initialize(this);
        RuntimeTilemapEditor mapEditor = gameObject.AddComponent<RuntimeTilemapEditor>();
        mapEditor.Initialize(worldCamera);
        RuntimeRenderTextureDoor renderTextureDoor =
            gameObject.AddComponent<RuntimeRenderTextureDoor>();
        renderTextureDoor.Initialize(worldCamera);
        gameObject.AddComponent<RuntimeScreenOutputQuiz>();
        RuntimeCurveControl curveControl = gameObject.AddComponent<RuntimeCurveControl>();
        curveControl.Initialize(this);
    }

    private static Mesh CreateScreenQuadMesh()
    {
        Mesh mesh = new Mesh { name = "Runtime Screen Quad Mesh" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateHierarchyBackground()
    {
        Texture2D texture = Resources.Load<Texture2D>("NatBed");
        if (texture == null)
            return;

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 100f);
        GameObject background = new GameObject("BACKGROUND - 낫베드 원본", typeof(SpriteRenderer));
        background.transform.SetParent(runtimeRoot, false);
        background.layer = BackgroundLayer;
        backgroundTransform = background.transform;
        SpriteRenderer renderer = background.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -100;

        float visibleHeight = worldCamera.orthographicSize * 2f;
        float scale = visibleHeight / sprite.bounds.size.y;
        backgroundTransform.localScale = Vector3.one * scale;
        backgroundTransform.position = new Vector3(
            worldCamera.transform.position.x, worldCamera.transform.position.y, 5f);
    }

    public void SetLesson(int lesson)
    {
        if (worldCamera == null || screenCamera == null || playerCamera == null)
            return;

        if (lesson == 1)
        {
            worldCamera.targetTexture = null;
            worldCamera.backgroundColor = Color.black;
            worldCamera.cullingMask |= (1 << BackgroundLayer) | (1 << PlayerOverlayLayer);
            screenCamera.enabled = false;
            playerCamera.enabled = false;
            return;
        }

        worldCamera.targetTexture = worldTexture;
        worldCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        worldCamera.cullingMask &= ~(1 << BackgroundLayer);
        screenCamera.targetTexture = null;
        screenCamera.targetDisplay = 0;
        screenCamera.enabled = true;

        if (lesson == 2)
        {
            worldCamera.cullingMask &= ~(1 << PlayerOverlayLayer);
            playerCamera.enabled = true;
            distortionMaterial.SetFloat("_Curve", 0f);
            distortionMaterial.SetFloat("_HorizontalWarp", 0f);
        }
        else if (lesson == 3)
        {
            // 플레이어까지 월드 카메라에 포함해 함께 구부러지는 문제를 보여줍니다.
            worldCamera.cullingMask |= 1 << PlayerOverlayLayer;
            playerCamera.enabled = false;
            distortionMaterial.SetFloat("_Curve", 0f);
            distortionMaterial.SetFloat("_HorizontalWarp", 0f);
        }
        else
        {
            worldCamera.cullingMask &= ~(1 << PlayerOverlayLayer);
            playerCamera.enabled = true;
            distortionMaterial.SetFloat("_Curve", 0f);
            distortionMaterial.SetFloat("_HorizontalWarp", 0f);
        }

        ResetCurveForCurrentMission();
    }

    public void SetManualCameraPractice(bool enabled)
    {
        if (enabled && !manualCameraPractice && worldCamera != null && playerTransform != null)
            worldCamera.transform.position = new Vector3(
                playerTransform.position.x, playerTransform.position.y, -10f);

        manualCameraPractice = enabled;
        if (!enabled && worldCamera != null && playerTransform != null)
            worldCamera.transform.position = new Vector3(
                playerTransform.position.x, playerTransform.position.y, -10f);
    }

    public void ResetCameraToPlayer()
    {
        if (worldCamera == null || playerTransform == null)
            return;

        if (CameraLearningTutorial.CurrentMission >= 3 && screenCamera != null)
        {
            // 다른 단계나 Inspector 변경이 최종 출력을 꺼도 Display 1 연결을 유지합니다.
            screenCamera.targetTexture = null;
            screenCamera.targetDisplay = 0;
            screenCamera.cullingMask = 1 << CurvedScreenLayer;
            screenCamera.enabled = true;
            if (screenRenderer != null)
                screenRenderer.enabled = true;
        }

        Vector3 center = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
        worldCamera.transform.position = center;
        worldCamera.transform.rotation = Quaternion.identity;
        worldCamera.orthographicSize = DefaultWorldCameraSize;

        if (playerCamera != null)
        {
            playerCamera.transform.position = center;
            playerCamera.transform.rotation = Quaternion.identity;
            playerCamera.orthographicSize = worldCamera.orthographicSize;
        }

        ResetCurveForCurrentMission();
    }

    private void ResetCurveForCurrentMission()
    {
        if (distortionMaterial == null)
            return;

        // 3단계는 평면 연결 확인, 4단계부터는 기본 원통 굴곡입니다.
        bool useCurve = CameraLearningTutorial.CurrentMission >= 5;
        distortionMaterial.SetFloat("_Curve", useCurve ? 0.48f : 0f);
        distortionMaterial.SetFloat("_HorizontalWarp", useCurve ? 0.24f : 0f);
    }

    private void CreateUndistortedPlayerCamera()
    {
        GameObject cameraObject = new GameObject("PLAYER CAMERA - 굴곡 없는 캐릭터", typeof(Camera));
        cameraObject.transform.SetParent(runtimeRoot, false);
        playerCamera = cameraObject.GetComponent<Camera>();
        playerCamera.orthographic = true;
        playerCamera.clearFlags = CameraClearFlags.Depth;
        playerCamera.cullingMask = 1 << PlayerOverlayLayer;
        playerCamera.depth = screenCamera.depth + 10f;
        playerCamera.allowHDR = false;
        playerCamera.allowMSAA = false;

        // URP에서는 별도 Base 카메라가 색상 화면을 다시 지우므로,
        // 플레이어 카메라를 굴곡 화면 위에 합성되는 Overlay로 등록합니다.
        UniversalAdditionalCameraData screenData = screenCamera.GetUniversalAdditionalCameraData();
        UniversalAdditionalCameraData playerData = playerCamera.GetUniversalAdditionalCameraData();
        playerData.renderType = CameraRenderType.Overlay;
        if (!screenData.cameraStack.Contains(playerCamera))
            screenData.cameraStack.Add(playerCamera);
    }

    private void FixGroundCollision()
    {
        zeroFrictionMaterial = new PhysicsMaterial2D("Zero Friction")
        {
            friction = 0f,
            bounciness = 0f
        };

        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            playerCollider.sharedMaterial = zeroFrictionMaterial;
            if (playerCollider is BoxCollider2D playerBox)
                playerBox.offset = Vector2.zero;
        }

        TilemapCollider2D[] tileColliders = FindObjectsByType<TilemapCollider2D>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (TilemapCollider2D tileCollider in tileColliders)
        {
            if (!tileCollider.CompareTag("Ground"))
                continue;

            // 타일마다 생기는 내부 경계선에 플레이어가 걸리지 않도록 하나의 면으로 합칩니다.
            Rigidbody2D tileBody = tileCollider.GetComponent<Rigidbody2D>();
            if (tileBody == null)
                tileBody = tileCollider.gameObject.AddComponent<Rigidbody2D>();
            tileBody.bodyType = RigidbodyType2D.Static;

            CompositeCollider2D composite = tileCollider.GetComponent<CompositeCollider2D>();
            if (composite == null)
                composite = tileCollider.gameObject.AddComponent<CompositeCollider2D>();
            composite.enabled = true;
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.sharedMaterial = zeroFrictionMaterial;

            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            tileCollider.enabled = true;
            tileCollider.sharedMaterial = zeroFrictionMaterial;
            Tilemap tilemap = tileCollider.GetComponent<Tilemap>();
            if (tilemap != null)
                tilemap.RefreshAllTiles();
            tileCollider.ProcessTilemapChanges();

        }
        Physics2D.SyncTransforms();
    }

    private void Update()
    {
        // 굴곡은 고정합니다. 배경, 중력, 충돌 오브젝트는 회전하지 않습니다.
    }

    private void LateUpdate()
    {
        if (worldCamera == null || playerTransform == null)
            return;

        Vector3 playerPosition = playerTransform.position;
        Vector3 target = new Vector3(playerPosition.x, playerPosition.y, -10f);
        // 실제 플레이에서는 튜토리얼 단계와 관계없이 플레이어를 계속 추적합니다.
        bool followPlayer = true;
        worldCamera.transform.position = target;

        if (playerCamera != null)
        {
            playerCamera.transform.position = followPlayer
                ? target : worldCamera.transform.position;
            playerCamera.transform.rotation = worldCamera.transform.rotation;
            playerCamera.orthographicSize = worldCamera.orthographicSize;
            playerCamera.rect = worldCamera.rect;
        }

        if (backgroundTransform != null)
        {
            Vector3 backgroundCenter = followPlayer
                ? target : worldCamera.transform.position;
            backgroundTransform.position = new Vector3(
                backgroundCenter.x, backgroundCenter.y, 5f);
        }

    }

    private void OnDestroy()
    {
        if (worldCamera != null)
            worldCamera.targetTexture = null;
        if (worldTexture != null && ownsWorldTexture)
        {
            worldTexture.Release();
            Destroy(worldTexture);
        }
        if (distortionMaterial != null)
            Destroy(distortionMaterial);
        if (screenMesh != null)
            Destroy(screenMesh);
        if (zeroFrictionMaterial != null)
            Destroy(zeroFrictionMaterial);
        if (Instance == this)
            Instance = null;
    }
}
