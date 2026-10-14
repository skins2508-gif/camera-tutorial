using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 예제 스테이지의 시작점, 낙사 복귀, 도착 상태와 화면 안내를 담당합니다.
/// 타일맵의 모양과 플레이 규칙을 분리해서 이해할 수 있도록 만든 예제입니다.
/// </summary>
public class CurvedStageGame : MonoBehaviour
{
    public static CurvedStageGame Instance { get; private set; }

    [SerializeField] private Vector2 spawnPoint = new Vector2(-17f, -6.45f);
    [SerializeField] private float fallLimit = -12f;
    private Rigidbody2D playerBody;
    private bool cleared;

    private void Awake()
    {
        Instance = this;
        playerBody = FindFirstObjectByType<PlayerController>()?.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (playerBody != null && playerBody.position.y < fallLimit)
            Respawn(playerBody);

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && playerBody != null)
            Respawn(playerBody);
    }

    public void Respawn(Rigidbody2D body)
    {
        body.linearVelocity = Vector2.zero;
        body.position = spawnPoint;
    }

    public void ClearStage()
    {
        cleared = true;
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(16, 16, 390, 112), "굴곡형 Tilemap 플레이 예제");
        GUI.Label(new Rect(32, 45, 360, 22), "이동: A/D 또는 방향키    점프: Z (2단 점프)");
        GUI.Label(new Rect(32, 68, 360, 22), "R: 시작점 복귀    흰 가시에 닿아도 복귀");
        GUI.Label(new Rect(32, 91, 360, 22), "타일 높이를 2~3칸마다 바꿔 곡선처럼 보이게 만듦");

        if (cleared)
            GUI.Box(new Rect(Screen.width / 2f - 160, Screen.height / 2f - 45, 320, 90),
                "STAGE CLEAR!\n계단식 타일 배치로 만든 굴곡 맵입니다.\nR을 누르면 다시 플레이할 수 있습니다.");
    }
}
