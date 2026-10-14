using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpPower = 9f;
    public int maxJump = 2;
    public LayerMask groudLayer; // 스크립트 상에 선언된 변수 유지

    private Rigidbody2D rb;
    private int jumpCount;
    private float moveInputX;
    private bool isJumpRequested;
    private SpriteRenderer spriteRenderer;
    private Vector2 spawnPoint = new Vector2(-17f, -6.45f);
    private bool stageCleared;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        TutorialCurvedWorld.Install(this);
        StartCoroutine(PlaceOnGroundAfterPhysicsStarts());
    }

    private System.Collections.IEnumerator PlaceOnGroundAfterPhysicsStarts()
    {
        // TilemapCollider2D/CompositeCollider2D가 첫 물리 프레임에 경계를 만든 뒤 배치합니다.
        rb.simulated = false;
        yield return new WaitForFixedUpdate();
        if (!SnapToGround(spawnPoint.x))
        {
            CreateEmergencyStartFloor();
            Physics2D.SyncTransforms();
            SnapToGround(spawnPoint.x);
        }
        spawnPoint = rb.position;
        rb.simulated = true;
    }

    void Update()
    {
        if (rb.position.y < -12f || Keyboard.current.rKey.wasPressedThisFrame)
            Respawn();

        if (rb.position.x > 16.5f && rb.position.y > 4.5f)
            stageCleared = true;

        // 1. 키 입력 감지 (Update에서 매 프레임 정확하게 캡처)
        moveInputX = 0f;

        // A, ← 입력
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            moveInputX = -1f;

        // D, → 입력
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            moveInputX = 1f;

        if (spriteRenderer != null && moveInputX != 0f)
            spriteRenderer.flipX = moveInputX < 0f;

        // Z 점프 입력 감지
        if (Keyboard.current.zKey.wasPressedThisFrame && jumpCount < maxJump)
        {
            isJumpRequested = true;
        }
    }

    public void SetSpawnPoint(Vector2 point)
    {
        spawnPoint = point;
    }

    private void Respawn()
    {
        rb.linearVelocity = Vector2.zero;
        if (!SnapToGround(spawnPoint.x))
            rb.position = spawnPoint;
        TutorialCurvedWorld.Instance?.ResetCameraToPlayer();
        stageCleared = false;
    }

    private bool SnapToGround(float x)
    {
        BoxCollider2D playerCollider = GetComponent<BoxCollider2D>();
        Physics2D.SyncTransforms();

        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, 10f), Vector2.down, 30f);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || !hit.collider.CompareTag("Ground"))
                continue;

            // 현재 피벗/콜라이더 오프셋을 고려해 발바닥을 지면보다 0.03 위에 둡니다.
            float bottomFromPivot = playerCollider.bounds.min.y - rb.position.y;
            rb.position = new Vector2(x, hit.point.y - bottomFromPivot + 0.03f);
            Physics2D.SyncTransforms();
            return true;
        }

        return false;
    }

    private void CreateEmergencyStartFloor()
    {
        if (GameObject.Find("START FLOOR - 추락 방지 발판") != null)
            return;

        GameObject floor = new GameObject("START FLOOR - 추락 방지 발판", typeof(BoxCollider2D));
        floor.tag = "Ground";
        floor.transform.position = new Vector3(spawnPoint.x, spawnPoint.y - 0.55f, 0f);
        BoxCollider2D floorCollider = floor.GetComponent<BoxCollider2D>();
        floorCollider.size = new Vector2(4f, 0.5f);
    }

    private void OnGUI()
    {
        if (CameraLearningTutorial.IsRunning)
            return;

        GUI.Box(new Rect(16, 16, 390, 112), "굴곡형 Tilemap 플레이 예제");
        GUI.Label(new Rect(32, 45, 350, 22), "이동: A/D 또는 방향키    점프: Z (2단 점프)");
        GUI.Label(new Rect(32, 68, 350, 22), "R: 처음부터    흰 가시/낙하: 시작점 복귀");
        GUI.Label(new Rect(32, 91, 350, 22), "목표: 맵 오른쪽 위까지 이동하세요");

        if (stageCleared)
            GUI.Box(new Rect(Screen.width / 2f - 150, Screen.height / 2f - 40, 300, 80),
                "STAGE CLEAR!\n2~3칸마다 높이를 바꾼 계단식 굴곡입니다.\nR을 눌러 다시 시작");
    }

    void FixedUpdate()
    {
        // 2. 물리 연산 적용 (FixedUpdate에서 처리하여 곡선 이동 시 떨림 방지)
        Vector2 right = TutorialCurvedWorld.Instance != null
            ? TutorialCurvedWorld.Instance.WorldRight : Vector2.right;
        Vector2 up = TutorialCurvedWorld.Instance != null
            ? TutorialCurvedWorld.Instance.WorldUp : Vector2.up;
        float verticalSpeed = Vector2.Dot(rb.linearVelocity, up);
        rb.linearVelocity = right * (moveInputX * moveSpeed) + up * verticalSpeed;

        if (isJumpRequested)
        {
            float horizontalSpeed = Vector2.Dot(rb.linearVelocity, right);
            rb.linearVelocity = right * horizontalSpeed + up * jumpPower;
            jumpCount++;
            isJumpRequested = false; // 요청 처리 완료
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 3. 정교한 곡선 지면 검사 (OnCollisionEnter 대신 Stay를 써야 경사면에서 안전합니다)
        if (collision.gameObject.CompareTag("Ground"))
        {
            // 충돌한 모든 지점(Contacts)을 조사하여 발밑 충돌인지 확인합니다
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // 법선 벡터(Normal)의 Y값이 0.5 이상이면 위를 바라보는 바닥으로 판정합니다
                Vector2 up = TutorialCurvedWorld.Instance != null
                    ? TutorialCurvedWorld.Instance.WorldUp : Vector2.up;
                if (Vector2.Dot(contact.normal, up) > 0.5f)
                {
                    jumpCount = 0;
                    break;
                }
            }
        }
    }
}
