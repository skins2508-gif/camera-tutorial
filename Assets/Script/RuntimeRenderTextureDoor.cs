using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

/// <summary>3단계에서 Render Texture를 Output Texture 슬롯에 넣는 과정을 문 충돌로 체험합니다.</summary>
public class RuntimeRenderTextureDoor : MonoBehaviour
{
    private Camera worldCamera;
    private GameObject door;
    private SpriteRenderer doorRenderer;
    private Texture2D doorTexture;
    private Sprite doorSprite;
    private GameObject temporaryRockPlatform;
    private Font uiFont;
    private bool wasMissionThree;
    private bool usingTemporaryPlatform;
    private int lastDoorOffset;

    public bool IsSucceeded { get; private set; }

    public void Initialize(Camera cameraToUse)
    {
        worldCamera = cameraToUse;
    }

    private void Start()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
        uiFont = Resources.Load<Font>("Fonts/NanumGothic-Regular");
        CreateDoor();
        door.SetActive(false);
    }

    private void CreateDoor()
    {
        doorTexture = new Texture2D(8, 10, TextureFormat.RGBA32, false)
        {
            name = "Runtime Output Texture Door",
            filterMode = FilterMode.Point
        };
        Color border = new Color(0.08f, 0.95f, 1f, 1f);
        Color inside = new Color(0.04f, 0.28f, 0.35f, 0.78f);
        for (int y = 0; y < doorTexture.height; y++)
        {
            for (int x = 0; x < doorTexture.width; x++)
            {
                bool isBorder = x == 0 || x == doorTexture.width - 1 ||
                    y == 0 || y == doorTexture.height - 1;
                doorTexture.SetPixel(x, y, isBorder ? border : inside);
            }
        }
        doorTexture.Apply();
        doorSprite = Sprite.Create(doorTexture,
            new Rect(0f, 0f, doorTexture.width, doorTexture.height),
            new Vector2(0.5f, 0.5f), 8f);
        doorSprite.name = "OUTPUT Texture Door Sprite";

        door = new GameObject("OUTPUT Texture Door", typeof(SpriteRenderer), typeof(BoxCollider2D));
        doorRenderer = door.GetComponent<SpriteRenderer>();
        doorRenderer.sprite = doorSprite;
        doorRenderer.sortingOrder = 50;
        door.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

        BoxCollider2D trigger = door.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(0.9f, 1.1f);

        RuntimeRenderTextureDoorTrigger relay = door.AddComponent<RuntimeRenderTextureDoorTrigger>();
        relay.Initialize(this);
    }

    private void Update()
    {
        if (door == null)
            return;

        bool missionThree = CameraLearningTutorial.CurrentMission == 3;
        if (missionThree && !wasMissionThree)
        {
            IsSucceeded = false;
            doorRenderer.color = Color.white;
            PlaceDoorInFrontOfPlayer();
        }
        else if (missionThree && Keyboard.current != null &&
            Keyboard.current.tKey.wasPressedThisFrame)
        {
            IsSucceeded = false;
            doorRenderer.color = Color.white;
            PlaceDoorInFrontOfPlayer();
        }
        door.SetActive(missionThree);
        if (temporaryRockPlatform != null)
            temporaryRockPlatform.SetActive(missionThree && usingTemporaryPlatform);
        wasMissionThree = missionThree;
    }

    private void PlaceDoorInFrontOfPlayer()
    {
        // 오른쪽 1~5칸 중 이전과 다른 거리를 골라 화면 안에서 무작위로 재배치합니다.
        int doorOffset;
        do
        {
            doorOffset = Random.Range(1, 6);
        }
        while (doorOffset == lastDoorOffset);
        lastDoorOffset = doorOffset;
        float doorX = transform.position.x + doorOffset;
        float doorY = transform.position.y + 0.2f;
        bool foundNearbyGround = false;
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            new Vector2(doorX, transform.position.y + 6f), Vector2.down, 14f);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || !hit.collider.CompareTag("Ground"))
                continue;
            if (Mathf.Abs(hit.point.y - transform.position.y) > 3f)
                continue;
            doorY = hit.point.y + 0.66f;
            foundNearbyGround = true;
            break;
        }

        usingTemporaryPlatform = !foundNearbyGround;
        if (usingTemporaryPlatform)
        {
            CreateTemporaryRockPlatformIfNeeded();
            temporaryRockPlatform.transform.position = new Vector3(
                doorX, transform.position.y - 0.55f, 0f);
            temporaryRockPlatform.SetActive(true);
            doorY = temporaryRockPlatform.transform.position.y + 1.16f;
        }
        else if (temporaryRockPlatform != null)
        {
            temporaryRockPlatform.SetActive(false);
        }
        Vector3 candidate = new Vector3(doorX, doorY, 0f);
        candidate = LiftDoorOutOfGround(candidate);
        door.transform.position = candidate;
    }

    private static Vector3 LiftDoorOutOfGround(Vector3 candidate)
    {
        // 문 영역에 Rock/Ground가 겹치면 한 셀씩 위로 올려 빈 공간을 찾습니다.
        Vector2 doorSpace = new Vector2(0.95f, 1.2f);
        for (int lift = 0; lift < 8; lift++)
        {
            bool overlapsGround = false;
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(candidate, doorSpace, 0f);
            foreach (Collider2D overlap in overlaps)
            {
                if (overlap != null && overlap.CompareTag("Ground"))
                {
                    overlapsGround = true;
                    break;
                }
            }
            if (!overlapsGround)
                return candidate;
            candidate.y += 1f;
        }
        return candidate;
    }

    private void CreateTemporaryRockPlatformIfNeeded()
    {
        if (temporaryRockPlatform != null)
            return;

        Tile rockTile = Resources.Load<Tile>("Rock1");
        temporaryRockPlatform = new GameObject(
            "TEMPORARY ROCK PLATFORM - Mission 3",
            typeof(SpriteRenderer), typeof(BoxCollider2D));
        temporaryRockPlatform.tag = "Ground";
        SpriteRenderer renderer = temporaryRockPlatform.GetComponent<SpriteRenderer>();
        renderer.sprite = rockTile != null ? rockTile.sprite : null;
        renderer.sortingOrder = 40;
        BoxCollider2D collider = temporaryRockPlatform.GetComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }

    public void PlayerEnteredDoor(Collider2D other)
    {
        if (IsSucceeded || CameraLearningTutorial.CurrentMission != 3)
            return;
        if (other.GetComponent<PlayerController>() == null)
            return;

        IsSucceeded = true;
        doorRenderer.color = new Color(0.25f, 1f, 0.35f, 1f);
    }

    private void OnGUI()
    {
        if (CameraLearningTutorial.CurrentMission != 3 || worldCamera == null || door == null)
            return;
        if (uiFont != null)
            GUI.skin.font = uiFont;

        DrawWorldLabel(transform.position + Vector3.up * 1.25f, "RT", 90f);
        DrawWorldLabel(door.transform.position + Vector3.up * 1.05f, "OUTPUT", 110f);
    }

    private void DrawWorldLabel(Vector3 worldPosition, string text, float width)
    {
        Vector3 viewport = worldCamera.WorldToViewportPoint(worldPosition);
        if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            return;

        float x = viewport.x * Screen.width - width * 0.5f;
        float y = (1f - viewport.y) * Screen.height - 18f;
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 16,
            font = uiFont
        };
        GUI.Box(new Rect(x, y, width, 30f), text, style);
    }

    private void OnDestroy()
    {
        if (door != null) Destroy(door);
        if (temporaryRockPlatform != null) Destroy(temporaryRockPlatform);
        if (doorSprite != null) Destroy(doorSprite);
        if (doorTexture != null) Destroy(doorTexture);
    }
}

public class RuntimeRenderTextureDoorTrigger : MonoBehaviour
{
    private RuntimeRenderTextureDoor owner;

    public void Initialize(RuntimeRenderTextureDoor challengeOwner)
    {
        owner = challengeOwner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        owner?.PlayerEnteredDoor(other);
    }
}
