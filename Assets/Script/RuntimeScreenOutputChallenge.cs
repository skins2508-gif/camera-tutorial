using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>4단계의 RenderTexture → Material → Quad → Display 연결을 문 통과로 체험합니다.</summary>
public class RuntimeScreenOutputChallenge : MonoBehaviour
{
    private static readonly string[] DoorNames = { "MATERIAL", "QUAD", "DISPLAY" };
    private static readonly string[] PlayerNames = { "RT", "MATERIAL", "QUAD", "DISPLAY" };

    private Camera worldCamera;
    private GameObject gate;
    private GameObject temporaryRockPlatform;
    private SpriteRenderer gateRenderer;
    private Texture2D gateTexture;
    private Sprite gateSprite;
    private Font uiFont;
    private int step;
    private bool wasMissionFour;
    private bool usingTemporaryPlatform;

    public bool IsSucceeded { get; private set; }

    public void Initialize(Camera cameraToUse) => worldCamera = cameraToUse;

    private void Start()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        uiFont = Resources.Load<Font>("Fonts/NanumGothic-Regular");
        CreateGate();
        gate.SetActive(false);
    }

    private void CreateGate()
    {
        gateTexture = new Texture2D(8, 10, TextureFormat.RGBA32, false)
        {
            name = "Runtime Screen Output Gate",
            filterMode = FilterMode.Point
        };
        for (int y = 0; y < gateTexture.height; y++)
        {
            for (int x = 0; x < gateTexture.width; x++)
            {
                bool border = x == 0 || x == gateTexture.width - 1 || y == 0 || y == gateTexture.height - 1;
                gateTexture.SetPixel(x, y, border
                    ? new Color(1f, 0.75f, 0.12f, 1f)
                    : new Color(0.38f, 0.18f, 0.04f, 0.8f));
            }
        }
        gateTexture.Apply();
        gateSprite = Sprite.Create(gateTexture, new Rect(0f, 0f, 8f, 10f), new Vector2(0.5f, 0.5f), 8f);

        gate = new GameObject("SCREEN OUTPUT CONNECTION GATE", typeof(SpriteRenderer), typeof(BoxCollider2D));
        gateRenderer = gate.GetComponent<SpriteRenderer>();
        gateRenderer.sprite = gateSprite;
        gateRenderer.sortingOrder = 50;
        gate.transform.localScale = Vector3.one * 1.05f;
        BoxCollider2D trigger = gate.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(0.9f, 1.1f);
        RuntimeScreenOutputGateTrigger relay = gate.AddComponent<RuntimeScreenOutputGateTrigger>();
        relay.Initialize(this);
    }

    private void Update()
    {
        if (gate == null) return;
        bool active = CameraLearningTutorial.CurrentMission == 4;
        if (active && !wasMissionFour)
        {
            step = 0;
            IsSucceeded = false;
            gateRenderer.color = Color.white;
            PlaceGateNearPlayer();
        }
        gate.SetActive(active && !IsSucceeded);
        if (temporaryRockPlatform != null)
            temporaryRockPlatform.SetActive(active && !IsSucceeded && usingTemporaryPlatform);
        wasMissionFour = active;
    }

    public void PlayerEnteredGate(Collider2D other)
    {
        if (IsSucceeded || CameraLearningTutorial.CurrentMission != 4 ||
            other.GetComponent<PlayerController>() == null)
            return;

        step++;
        if (step >= DoorNames.Length)
        {
            IsSucceeded = true;
            gate.SetActive(false);
            if (temporaryRockPlatform != null) temporaryRockPlatform.SetActive(false);
            return;
        }
        gateRenderer.color = step == 1
            ? new Color(0.35f, 1f, 0.45f, 1f)
            : new Color(0.35f, 0.75f, 1f, 1f);
        PlaceGateNearPlayer();
    }

    private void PlaceGateNearPlayer()
    {
        float gateX = transform.position.x + 3f;
        float gateY = transform.position.y + 0.2f;
        bool foundGround = false;
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(gateX, transform.position.y + 6f), Vector2.down, 14f);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || !hit.collider.CompareTag("Ground")) continue;
            if (Mathf.Abs(hit.point.y - transform.position.y) > 3f) continue;
            gateY = hit.point.y + 0.66f;
            foundGround = true;
            break;
        }

        usingTemporaryPlatform = !foundGround;
        if (usingTemporaryPlatform)
        {
            CreateTemporaryRockPlatform();
            temporaryRockPlatform.transform.position = new Vector3(gateX, transform.position.y - 0.55f, 0f);
            temporaryRockPlatform.SetActive(true);
            gateY = temporaryRockPlatform.transform.position.y + 1.16f;
        }
        else if (temporaryRockPlatform != null) temporaryRockPlatform.SetActive(false);

        Vector3 candidate = new Vector3(gateX, gateY, 0f);
        for (int lift = 0; lift < 8; lift++)
        {
            bool blocked = false;
            foreach (Collider2D overlap in Physics2D.OverlapBoxAll(candidate, new Vector2(0.95f, 1.2f), 0f))
            {
                if (overlap != null && overlap.CompareTag("Ground")) { blocked = true; break; }
            }
            if (!blocked) break;
            candidate.y += 1f;
        }
        gate.transform.position = candidate;
    }

    private void CreateTemporaryRockPlatform()
    {
        if (temporaryRockPlatform != null) return;
        Tile rock = Resources.Load<Tile>("Rock1");
        temporaryRockPlatform = new GameObject("TEMPORARY ROCK PLATFORM - Mission 4", typeof(SpriteRenderer), typeof(BoxCollider2D));
        temporaryRockPlatform.tag = "Ground";
        temporaryRockPlatform.GetComponent<SpriteRenderer>().sprite = rock != null ? rock.sprite : null;
        temporaryRockPlatform.GetComponent<SpriteRenderer>().sortingOrder = 40;
        temporaryRockPlatform.GetComponent<BoxCollider2D>().size = Vector2.one;
    }

    private void OnGUI()
    {
        if (CameraLearningTutorial.CurrentMission != 4 || worldCamera == null) return;
        if (uiFont != null) GUI.skin.font = uiFont;
        DrawLabel(transform.position + Vector3.up * 1.25f, PlayerNames[Mathf.Min(step, PlayerNames.Length - 1)], 120f);
        if (!IsSucceeded) DrawLabel(gate.transform.position + Vector3.up * 1.05f, DoorNames[step], 130f);
    }

    private void DrawLabel(Vector3 worldPosition, string text, float width)
    {
        Vector3 viewport = worldCamera.WorldToViewportPoint(worldPosition);
        if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return;
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 16,
            font = uiFont
        };
        GUI.Box(new Rect(viewport.x * Screen.width - width * 0.5f,
            (1f - viewport.y) * Screen.height - 18f, width, 30f), text, style);
    }

    private void OnDestroy()
    {
        if (gate != null) Destroy(gate);
        if (temporaryRockPlatform != null) Destroy(temporaryRockPlatform);
        if (gateSprite != null) Destroy(gateSprite);
        if (gateTexture != null) Destroy(gateTexture);
    }
}

public class RuntimeScreenOutputGateTrigger : MonoBehaviour
{
    private RuntimeScreenOutputChallenge owner;
    public void Initialize(RuntimeScreenOutputChallenge challenge) => owner = challenge;
    private void OnTriggerEnter2D(Collider2D other) => owner?.PlayerEnteredGate(other);
}
