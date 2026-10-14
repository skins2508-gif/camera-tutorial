using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>게임 플레이 안에서 카메라 제작 과정을 안내하는 튜토리얼입니다.</summary>
public class CameraLearningTutorial : MonoBehaviour
{
    private const string ProgressKey = "CameraTutorial.Mission";
    private TutorialCurvedWorld curvedWorld;
    private int mission = 1;
    private bool panelVisible = true;
    private bool showReference;
    private int referencePage;
    private string[] referencePages = { "설명서를 불러오는 중입니다." };
    private Vector2 referenceScroll;
    private bool completed;
    private bool playerOverlayRestored;
    private Font readableFont;
    private readonly bool[] goalAchieved = new bool[9];
    private RuntimeTilemapEditor mapEditor;
    private RuntimeRenderTextureDoor renderTextureDoor;
    private RuntimeScreenOutputQuiz screenOutputQuiz;
    private RuntimeCurveControl curveControl;
    private bool missionSevenWPressed;

    public static bool IsRunning => FindAnyObjectByType<CameraLearningTutorial>() != null;
    public static int CurrentMission { get; private set; } = 1;

    public void Initialize(TutorialCurvedWorld world)
    {
        curvedWorld = world;
        mission = Mathf.Clamp(PlayerPrefs.GetInt(ProgressKey, 1), 1, 8);
        CurrentMission = mission;
        readableFont = CreateReadableFont();
        ApplyMissionView();
    }

    private static Font CreateReadableFont()
    {
        Font bundledKoreanFont = Resources.Load<Font>("Fonts/NanumGothic-Regular");
        if (bundledKoreanFont != null)
            return bundledKoreanFont;

        string[] preferredFonts = { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Arial Unicode MS", "Arial" };
        foreach (string fontName in preferredFonts)
        {
            Font font = Font.CreateDynamicFontFromOSFont(fontName, 16);
            if (font != null)
                return font;
        }
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.qKey.wasPressedThisFrame) panelVisible = !panelVisible;
        if (keyboard.wKey.wasPressedThisFrame)
        {
            if (mission == 7)
                missionSevenWPressed = true;
            showReference = !showReference;
            if (showReference)
            {
                referencePage = 0;
                referenceScroll = Vector2.zero;
                LoadStepPages();
            }
        }
        if (showReference && keyboard.bKey.wasPressedThisFrame)
        {
            referencePage = Mathf.Max(0, referencePage - 1);
            referenceScroll = Vector2.zero;
        }
        if (showReference && keyboard.nKey.wasPressedThisFrame)
        {
            referencePage = Mathf.Min(referencePages.Length - 1, referencePage + 1);
            referenceScroll = Vector2.zero;
        }
        if (mission == 6 && keyboard.iKey.wasPressedThisFrame)
        {
            playerOverlayRestored = !playerOverlayRestored;
            curvedWorld?.SetLesson(playerOverlayRestored ? 4 : 3);
        }
        if (mission == 8 && keyboard.enterKey.wasPressedThisFrame && !completed)
            CompleteTutorial();

        for (int number = 1; number <= 8; number++)
        {
            if (NumberKeyPressed(keyboard, number))
            {
                SetMission(number);
                break;
            }
        }

        TryCompleteCurrentGoal();
    }

    private static bool NumberKeyPressed(Keyboard keyboard, int number)
    {
        return number switch
        {
            1 => keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame,
            2 => keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame,
            3 => keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame,
            4 => keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame,
            5 => keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame,
            6 => keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame,
            7 => keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame,
            8 => keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame,
            _ => false
        };
    }

    private void SetMission(int value)
    {
        mission = Mathf.Clamp(value, 1, 8);
        CurrentMission = mission;
        playerOverlayRestored = false;
        if (mission == 7)
            missionSevenWPressed = false;
        referencePage = 0;
        referenceScroll = Vector2.zero;
        if (showReference) LoadStepPages();
        PlayerPrefs.SetInt(ProgressKey, mission);
        PlayerPrefs.Save();
        ApplyMissionView();
    }

    private void ApplyMissionView()
    {
        int renderLesson = mission <= 2 ? 1 : mission == 3 ? 2 : mission == 6 ? 3 : 4;
        curvedWorld?.SetLesson(renderLesson);
        curvedWorld?.SetManualCameraPractice(false);
        curvedWorld?.ResetCameraToPlayer();
    }

    private void OnGUI()
    {
        if (readableFont != null)
            GUI.skin.font = readableFont;
        if (!panelVisible)
            return;
        if (showReference)
        {
            DrawReferencePanel();
            return;
        }

        DrawRestartHelp();
        float panelWidth = Mathf.Min(620f, Screen.width - 32f);
        const float panelHeight = 226f;
        GUI.Box(new Rect(16, 16, panelWidth, panelHeight), $"카메라 제작 튜토리얼 {mission}/8 - {Title()}");
        GUI.Label(new Rect(34, 48, panelWidth - 36f, 44), "학습\n" + Goal());
        GUI.Label(new Rect(34, 96, panelWidth - 36f, 88), "직접 해보기\n" + Instructions());
        GUI.Label(new Rect(34, 188, panelWidth - 36f, 24), "Q: UI 숨기기   W: 유니티 엔진 설명서   숫자 1~8: 단계 이동");
        DrawCompletionBar();
    }

    private void DrawReferencePanel()
    {
        float width = Mathf.Max(320f, Mathf.Min(920f, Screen.width - 48f));
        float height = Mathf.Max(280f, Mathf.Min(700f, Screen.height - 48f));
        float x = (Screen.width - width) * 0.5f;
        float y = (Screen.height - height) * 0.5f;
        GUI.Box(new Rect(x, y, width, height), $"W: 유니티 엔진 설명서   |   {referencePage + 1}/{referencePages.Length} 페이지");
        GUI.Label(new Rect(x + 24f, y + 35f, width - 48f, 24f), $"{mission}/8 단계 · {Title()}");
        GUIStyle pageStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            fontSize = 16,
            padding = new RectOffset(18, 18, 16, 16),
            font = readableFont
        };
        Rect scrollArea = new Rect(x + 20f, y + 66f, width - 40f, height - 122f);
        GUI.Box(scrollArea, GUIContent.none);
        float contentWidth = scrollArea.width - 38f;
        float contentHeight = Mathf.Max(scrollArea.height - 8f, pageStyle.CalcHeight(new GUIContent(referencePages[referencePage]), contentWidth) + 24f);
        referenceScroll = GUI.BeginScrollView(scrollArea, referenceScroll, new Rect(0f, 0f, contentWidth, contentHeight));
        GUI.Label(new Rect(4f, 4f, contentWidth - 8f, contentHeight - 8f), referencePages[referencePage], pageStyle);
        GUI.EndScrollView();
        GUI.Label(new Rect(x + 24f, y + height - 45f, width - 48f, 25f), "마우스 휠: 스크롤   B: 이전 페이지   N: 다음 페이지   W: 닫기");
    }

    private void LoadStepPages()
    {
        string[] lines = BuiltInReferenceText().Replace("\r", "").Split('\n');
        const int linesPerPage = 11;
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(lines.Length / (float)linesPerPage));
        referencePages = new string[pageCount];
        for (int page = 0; page < pageCount; page++)
        {
            int start = page * linesPerPage;
            int count = Mathf.Min(linesPerPage, lines.Length - start);
            referencePages[page] = string.Join("\n", new List<string>(lines).GetRange(start, count));
        }
        referencePage = Mathf.Clamp(referencePage, 0, referencePages.Length - 1);
    }

    private string BuiltInReferenceText()
    {
        if (mission == 2)
            return DetailedMainCameraReferenceText();
        if (mission == 3)
            return DetailedRenderTextureReferenceText();
        if (mission == 4)
            return DetailedScreenOutputReferenceText() + DetailedMaterialShaderReferenceText();
        if (mission == 5)
            return RemoveReferenceSection(
                DetailedCurveShaderReferenceText(),
                "[코드에서 값 바꾸기]",
                "[공유 Material 주의]");
        if (mission == 6)
            return DetailedLayerSeparationReferenceText();
        if (mission == 7)
            return DetailedWorkflowReferenceText();
        if (mission == 8)
            return "열심히 공부하세요~";

        return mission switch
        {
        1 => "[Tile Palette 열기]\nWindow → 2D → Tile Palette\n\n[사용 방법]\nPalette에서 Tile을 선택합니다.\nHierarchy에서 작업할 Tilemap을 선택합니다.\nBrush로 그리고 Eraser로 지웁니다.\nSprite를 Palette로 드래그하면 새 Tile을 등록할 수 있습니다.",
        2 => "[Main Camera 열기]\nHierarchy 창에서 Main Camera를 선택합니다.\n선택하면 Inspector에 Transform과 Camera 컴포넌트가 표시됩니다.\nScene에 Main Camera가 없다면 Hierarchy 우클릭 → Camera로 만듭니다.\n게임의 대표 카메라는 Tag를 MainCamera로 설정합니다.\n\n[Transform]\nPosition X/Y는 카메라가 바라보는 월드 위치입니다.\n2D 카메라는 보통 Z를 -10으로 두고 Z=0의 맵을 촬영합니다.\nRotation을 바꾸면 게임 화면도 같은 방향으로 회전합니다.\nScale은 Camera의 화면 크기를 조절하는 값이 아닙니다.\n\n[Projection]\nInspector → Camera → Projection에서 투영 방식을 선택합니다.\nPerspective는 거리에 따라 물체 크기가 달라지는 3D 방식입니다.\nOrthographic은 거리에 관계없이 크기가 유지되는 2D 방식입니다.\n이 프로젝트에서는 Orthographic을 사용합니다.\n\n[Orthographic Size]\nSize는 화면 세로 방향으로 보이는 월드 범위의 절반입니다.\nSize를 줄이면 화면이 확대되고, 늘리면 더 넓게 보입니다.\nSize는 캐릭터나 Tilemap의 실제 크기를 변경하지 않습니다.\nGame 창의 가로 범위는 Size와 화면 비율에 따라 결정됩니다.\n\n[Culling Mask]\nInspector → Camera → Rendering → Culling Mask를 엽니다.\n체크된 Layer만 해당 Camera에 보입니다.\nNothing은 아무것도 촬영하지 않고 Everything은 모든 Layer를 촬영합니다.\n맵과 플레이어를 다른 Camera로 나눌 때 Layer와 Culling Mask를 사용합니다.\n\n[Clipping Planes]\nNear는 카메라와 너무 가까워 보이지 않는 경계입니다.\nFar는 너무 멀어 보이지 않는 경계입니다.\n2D 오브젝트의 Z 위치가 Near와 Far 사이에 있어야 화면에 표시됩니다.\n\n[Environment와 Background]\nBackground Type 또는 Clear Flags에서 화면을 지우는 방식을 정합니다.\nSolid Color를 선택하면 Background 색상으로 빈 화면을 채웁니다.\nSkybox는 주로 3D 게임의 하늘 배경에 사용합니다.\n\n[Output]\nOutput Texture가 None이면 Game View 또는 Display에 직접 출력합니다.\nRender Texture를 넣으면 촬영 결과가 해당 텍스처에 저장됩니다.\nTarget Display는 여러 모니터 중 출력할 Display를 선택합니다.\nViewport Rect는 화면 안에서 Camera가 차지할 위치와 크기를 정합니다.\nX/Y는 시작 위치, W/H는 너비와 높이이며 0~1 값을 사용합니다.\n\n[카메라 우선순위와 Stack]\nURP에서는 Base Camera가 기본 화면을 먼저 그립니다.\nOverlay Camera는 Base Camera의 Stack에 추가해 결과를 위에 합칩니다.\n여러 Base Camera가 있으면 Priority가 높은 Camera가 나중에 표시됩니다.\n\n[플레이어 추적]\nCamera Transform을 플레이어 위치에 맞추면 이동을 따라갑니다.\n보통 LateUpdate에서 처리해 플레이어 이동 계산 이후 카메라를 움직입니다.\n카메라 Z는 유지하고 플레이어의 X/Y만 따라가도록 설정합니다.\n부드러운 추적에는 SmoothDamp 또는 Cinemachine을 사용할 수 있습니다.\n\n[확인 방법]\nGame 창을 보면서 Position과 Size를 조금씩 변경해 차이를 확인합니다.\n오브젝트가 사라지면 Culling Mask, Z 위치, Near/Far를 확인합니다.\n화면이 나오지 않으면 Camera 활성화, Tag, Output Texture를 확인합니다.",
        3 => "[Render Texture 만들기]\nProject 창 우클릭 → Create → Rendering → Render Texture\n\n[사용 방법]\nMain Camera를 선택합니다.\nInspector → Output → Output Texture에 Render Texture를 넣습니다.\nRender Texture를 선택하면 Inspector에서 미리 볼 수 있습니다.",
        4 => "[Quad와 Material 만들기]\nHierarchy 우클릭 → 3D Object → Quad\nProject 창 우클릭 → Create → Material\n\n[사용 방법]\nMaterial에 Render Texture를 연결합니다.\nQuad의 Mesh Renderer에 Material을 넣습니다.\n별도 Camera로 Quad를 촬영해 Display 1에 출력합니다.",
        5 => "[Shader와 Material 열기]\nProject 창에서 Shader 또는 Material을 선택합니다.\n\n[사용 방법]\nMaterial의 Shader 항목에서 사용할 Shader를 선택합니다.\nCurve 값이 0이면 평면입니다.\nCurve 값을 높이면 화면 굴곡이 커집니다.\nHorizontal Warp로 좌우 왜곡을 조절합니다.",
        6 => "[Layer 설정 열기]\nInspector 상단 → Layer → Add Layer\n\n[사용 방법]\nPlayer에 전용 Layer를 지정합니다.\nPlayer Camera의 Culling Mask를 Player Layer로 설정합니다.\nRender Type을 Overlay로 바꿉니다.\nBase Camera의 Stack에 Player Camera를 추가합니다.",
        7 => "[카메라 구조 확인]\nHierarchy에서 Main Camera, Distortion Camera, Player Camera를 선택합니다.\n\n[확인 방법]\nInspector에서 Culling Mask를 확인합니다.\nOutput Texture 연결을 확인합니다.\nBase/Overlay 설정과 Camera Stack 순서를 확인합니다.\nMaterial의 Texture와 Shader 연결을 확인합니다.",
        _ => "[Play와 Console 열기]\n에디터 상단의 Play 버튼으로 실행합니다.\nWindow → General → Console로 Console을 엽니다.\n\n[사용 방법]\nGame 창에서 최종 화면을 확인합니다.\nConsole에서 빨간 Error가 있는지 확인합니다.\nFile → Build Profiles에서 실행 파일을 만들 수 있습니다."
        };
    }

    private static string DetailedMainCameraReferenceText()
    {
        return "[Main Camera 열기]\nHierarchy에서 Main Camera를 선택합니다.\nInspector에서 Transform과 Camera 컴포넌트를 확인합니다.\n\n[Transform]\nPosition X/Y는 카메라가 바라보는 위치입니다.\n2D 카메라는 보통 Z를 -10으로 설정합니다.\nScale은 화면 크기를 변경하지 않습니다.\n\n[Projection]\nCamera → Projection을 Orthographic으로 설정합니다.\nSize가 작으면 확대되고 크면 넓게 보입니다.\n\n[Culling Mask]\nCamera → Rendering → Culling Mask를 엽니다.\n체크한 Layer만 카메라에 표시됩니다.\n\n[Near/Far Clipping Planes 열기]\nMain Camera → Inspector → Camera → Clipping Planes\nNear와 Far 입력칸을 확인합니다.\n\n[Near Clipping Plane]\n카메라가 촬영을 시작하는 가장 가까운 거리입니다.\nNear보다 카메라에 가까운 오브젝트는 잘려서 보이지 않습니다.\nNear가 0.3이면 카메라 앞 0.3보다 가까운 영역은 촬영하지 않습니다.\n\n[Far Clipping Plane]\n카메라가 촬영할 수 있는 가장 먼 거리입니다.\nFar보다 멀리 있는 오브젝트는 잘려서 보이지 않습니다.\nFar가 1000이면 카메라로부터 1000 거리까지만 촬영합니다.\n\n[2D 예시]\n카메라 Z가 -10이고 Tilemap Z가 0이면 둘의 거리는 약 10입니다.\nNear는 10보다 작고 Far는 10보다 커야 Tilemap이 보입니다.\n오브젝트가 사라지면 오브젝트 Z가 Near/Far 범위 안인지 확인합니다.\n\n[Background]\nCamera의 Background Type과 색상으로 빈 화면 표시를 정합니다.\n\n[Output]\nOutput Texture가 None이면 Display에 직접 출력합니다.\nRender Texture를 넣으면 촬영 결과가 텍스처에 저장됩니다.\nViewport Rect는 화면 안에서 Camera가 차지할 영역입니다.\n\n[플레이어 추적]\nLateUpdate에서 Camera의 X/Y를 플레이어 위치에 맞춥니다.\n카메라 Z는 유지해야 2D 오브젝트와의 촬영 거리가 보존됩니다.\n\n[문제 확인]\n화면이 안 보이면 Camera 활성화, Culling Mask, Z 위치를 확인합니다.\n그다음 Near, Far, Output Texture 설정을 확인합니다.";
    }

    private static string DetailedRenderTextureReferenceText()
    {
        return "[Render Texture란?]\nRender Texture는 Camera가 촬영한 결과를 저장하는 화면용 텍스처입니다.\n일반 Camera는 Game View나 Display에 바로 출력합니다.\nOutput Texture를 지정한 Camera는 촬영 결과를 Render Texture로 보냅니다.\n저장된 화면은 Material, UI, 미니맵, 감시 카메라 등에 사용할 수 있습니다.\n\n[Render Texture 만들기]\nProject 창의 빈 공간에서 마우스 오른쪽 버튼을 누릅니다.\nCreate → Rendering → Render Texture를 선택합니다.\n생성된 에셋의 이름을 알아보기 쉽게 변경합니다.\n이 튜토리얼에서는 TutorialRenderTexture라는 이름을 사용합니다.\n\n[Inspector 열기]\nProject 창에서 만든 Render Texture를 선택합니다.\nInspector에 Size, Format, Depth Buffer 등의 설정이 표시됩니다.\nInspector 아래 Preview에서 현재 저장된 화면을 확인할 수 있습니다.\n\n[Size]\nSize의 Width와 Height는 저장할 화면의 픽셀 해상도입니다.\n1280 × 720은 16:9 비율의 HD 화면입니다.\n1920 × 1080은 더 선명하지만 더 많은 GPU 메모리를 사용합니다.\nGame View와 화면 비율이 다르면 결과가 늘어나거나 눌릴 수 있습니다.\n처음에는 Game View와 같은 비율을 사용하는 것이 좋습니다.\n\n[Color Format]\nColor Format은 한 픽셀에 색상을 어떤 방식으로 저장할지 정합니다.\nDefault는 현재 플랫폼에 적합한 일반 색상 형식을 선택합니다.\nHDR 형식은 밝은 색 범위를 더 많이 저장하지만 메모리 사용량이 증가합니다.\n특별한 목적이 없다면 일반 화면에는 Default를 사용할 수 있습니다.\n\n[Depth Buffer]\nDepth Buffer는 어떤 오브젝트가 앞에 있는지 판단하는 깊이 정보입니다.\n2D 화면만 단순 출력할 때는 깊이 정밀도가 많이 필요하지 않을 수 있습니다.\n3D 오브젝트의 앞뒤 관계를 저장하려면 Depth Buffer가 필요합니다.\n깊이 값이 클수록 정밀도가 높아지지만 메모리를 더 사용합니다.\n\n[Anti-Aliasing]\nAnti-Aliasing은 대각선과 가장자리의 계단 현상을 줄입니다.\n샘플 수가 높을수록 부드럽지만 렌더링 비용이 증가합니다.\nURP와 Camera의 MSAA 설정도 함께 확인해야 합니다.\n\n[Filter Mode]\nPoint는 픽셀을 선명하게 유지해 픽셀 아트에 적합합니다.\nBilinear는 주변 픽셀을 섞어 확대 시 부드럽게 표시합니다.\nTrilinear는 Mipmap 단계까지 섞지만 일반 화면 출력에는 보통 필요하지 않습니다.\n\n[Wrap Mode]\nClamp는 UV가 화면 밖으로 나갈 때 가장자리 색상을 유지합니다.\nRepeat는 화면을 반복해 표시합니다.\n최종 화면 출력에는 보통 Clamp가 안전합니다.\n\n[Main Camera에 연결하기]\nHierarchy에서 Main Camera를 선택합니다.\nInspector → Camera → Output을 펼칩니다.\nOutput Texture 슬롯에 TutorialRenderTexture를 드래그합니다.\n이제 Main Camera는 Display 1 대신 Render Texture에 화면을 저장합니다.\nGame View가 비어 보여도 Render Texture Preview에 화면이 보이면 정상입니다.\n\n[Material에 사용하기]\nProject 창에서 Material을 선택합니다.\nMaterial의 Texture 슬롯에 Render Texture를 드래그합니다.\n그 Material을 Quad 또는 UI Raw Image에 연결하면 Camera 화면이 표시됩니다.\n이 프로젝트에서는 Screen Quad의 Material 입력으로 사용합니다.\n\n[연결 흐름]\nMain Camera → TutorialRenderTexture\nTutorialRenderTexture → 화면용 Material\n화면용 Material → Screen Quad\n최종 Camera → Screen Quad 촬영 → Display 1\n\n[실행 중 확인]\nPlay Mode에서 Render Texture를 선택하면 Inspector Preview가 계속 갱신됩니다.\nCamera를 움직이면 Preview 화면도 함께 변해야 합니다.\nOutput Texture를 None으로 되돌리면 Camera가 다시 Display에 직접 출력합니다.\n\n[화면이 안 보일 때]\nMain Camera가 활성화되어 있는지 확인합니다.\nCamera의 Culling Mask가 맵 Layer를 포함하는지 확인합니다.\nOutput Texture에 올바른 Render Texture가 연결됐는지 확인합니다.\nRender Texture를 사용하는 Material과 Quad가 활성화됐는지 확인합니다.\n최종 Camera의 Culling Mask가 Screen Quad Layer를 포함하는지 확인합니다.\n\n[성능 확인]\nRender Texture 해상도가 크면 GPU 메모리와 렌더링 비용이 증가합니다.\n여러 Camera가 각각 Render Texture를 촬영하면 비용도 함께 증가합니다.\n필요한 해상도와 Camera 수만 사용하고 사용하지 않는 Camera는 비활성화합니다.";
    }

    private static string DetailedScreenOutputReferenceText()
    {
        return "[4단계의 목적]\nRender Texture에 저장된 화면을 Material과 Quad를 거쳐 Display에 출력합니다.\n연결 순서는 Render Texture → Material → Quad → Camera → Display입니다.\n\n[Quad 만들기]\nHierarchy 우클릭 → 3D Object → Quad를 선택합니다.\nQuad는 네 개의 정점으로 만든 평면 Mesh입니다.\nTransform Position과 Scale로 최종 Camera 화면을 채우도록 배치합니다.\n화면 출력용 Quad에는 물리 충돌이 필요 없으므로 Collider를 제거합니다.\n\n[Mesh Filter]\nMesh Filter는 Quad의 정점, 삼각형, UV 정보를 가진 Mesh를 보관합니다.\nUV는 Render Texture의 어느 위치를 Quad에 표시할지 정하는 0~1 좌표입니다.\nMesh Filter의 Mesh가 없으면 Mesh Renderer가 그릴 모양도 없습니다.\n\n[Material 만들기]\nProject 창 우클릭 → Create → Material을 선택합니다.\nMaterial 이름을 ScreenOutputMaterial처럼 알아보기 쉽게 정합니다.\nMaterial의 Shader를 화면 출력용 Shader로 선택합니다.\nTexture 슬롯에 TutorialRenderTexture를 드래그합니다.\n\n[Mesh Renderer]\nHierarchy에서 Quad를 선택합니다.\nInspector → Mesh Renderer → Materials를 펼칩니다.\nElement 0에 화면 출력용 Material을 드래그합니다.\n이제 Quad 표면에 Main Camera가 촬영한 화면이 표시됩니다.\n\n[전용 Layer]\nInspector 상단 Layer → Add Layer에서 Screen 전용 Layer를 만듭니다.\nQuad의 Layer를 Screen 전용 Layer로 지정합니다.\nMain Camera의 Culling Mask에서는 Screen Layer를 제외해 재귀 촬영을 막습니다.\n최종 Camera의 Culling Mask는 Screen Layer만 선택합니다.\n\n[최종 Camera 만들기]\nHierarchy 우클릭 → Camera를 선택합니다.\n이름을 Distortion Camera 또는 Output Camera로 변경합니다.\nProjection은 Orthographic으로 설정합니다.\nCamera가 Quad 정면을 바라보도록 Position을 지정합니다.\nOrthographic Size와 Quad Scale을 맞춰 화면을 가득 채웁니다.\n\n[최종 Camera Output]\nOutput Texture는 None으로 둡니다.\nTarget Display는 Display 1로 설정합니다.\nOutput Texture가 None이어야 최종 결과가 Game View에 직접 표시됩니다.\nCamera가 Screen Layer만 촬영하는지 Culling Mask를 확인합니다.\n\n[화면 비율]\nQuad Scale X는 Game View의 가로세로 비율에 맞춰야 합니다.\n16:9 화면은 가로가 세로보다 약 1.777배 넓습니다.\n비율이 맞지 않으면 화면이 늘어나거나 좌우에 빈 공간이 생깁니다.\n\n[URP Camera]\n최종 화면을 먼저 그리는 Camera는 Base로 사용합니다.\n추가로 합성할 Camera가 있다면 Overlay로 설정해 Base Camera Stack에 넣습니다.\nBase와 Overlay Camera의 HDR, MSAA, Viewport 설정을 맞춥니다.\n\n[전체 연결 확인]\nMain Camera의 Output Texture → TutorialRenderTexture\nMaterial의 Texture → TutorialRenderTexture\nQuad의 Mesh Renderer → 화면 Material\n최종 Camera Culling Mask → Screen Layer\n최종 Camera Output Texture → None\n최종 Camera Target Display → Display 1\n\n[검은 화면일 때]\nRender Texture Preview에 Main Camera 화면이 있는지 확인합니다.\nMaterial의 Texture 슬롯이 비어 있지 않은지 확인합니다.\nQuad의 Mesh Renderer와 GameObject가 활성화됐는지 확인합니다.\n최종 Camera가 Quad를 바라보는지 확인합니다.\n최종 Camera의 Culling Mask와 Quad Layer가 같은지 확인합니다.\n\n[화면이 반복되거나 겹칠 때]\nMain Camera가 Screen Quad를 다시 촬영하지 않는지 확인합니다.\nScreen Quad Layer를 Main Camera Culling Mask에서 제외합니다.\n같은 Display를 출력하는 Base Camera가 중복으로 켜져 있지 않은지 확인합니다.";
    }

    private static string DetailedMaterialShaderReferenceText()
    {
        return "\n\n[Element 0와 Texture 슬롯의 차이]\nQuad 선택 → Mesh Renderer → Materials → Element 0은 Material을 넣는 슬롯입니다.\n이곳에 Render Texture를 직접 넣는 것이 아닙니다.\n먼저 Project 창에서 Material을 만들고 Element 0에 그 Material을 넣습니다.\n그다음 Project 창에서 Material 자체를 선택해야 Texture 설정이 Inspector에 나타납니다.\n이미 Element 0에 Material이 있다면 이름을 클릭해 해당 Material을 선택할 수도 있습니다.\n\n[Material Inspector 열기]\nProject 창에서 사용할 Material 에셋을 클릭합니다.\n또는 Quad → Mesh Renderer → Materials → Element 0의 Material 이름을 클릭합니다.\nInspector를 아래로 스크롤하면 Material 전용 Inspector가 표시됩니다.\nMaterial의 Shader 선택에 따라 Texture 슬롯의 이름과 위치가 달라집니다.\n\n[화면 출력용 Shader란?]\n화면 출력용 Shader는 Render Texture의 색상을 조명 계산 없이 Quad에 그대로 보여주는 Shader입니다.\n최종 화면은 빛을 받아 어두워지면 안 되므로 보통 Unlit 계열을 사용합니다.\nUnlit은 Scene Light의 영향을 받지 않고 Texture 색상을 그대로 출력합니다.\n\n[대표 Shader 1: Universal Render Pipeline/Unlit]\nURP의 일반적인 화면 Quad에 가장 먼저 사용할 수 있는 Shader입니다.\nMaterial → Shader → Universal Render Pipeline → Unlit을 선택합니다.\n이 Shader에서는 Texture 슬롯 이름이 Base Map입니다.\nBase Map 왼쪽의 작은 사각형 칸에 Render Texture를 드래그합니다.\n화면을 조명 영향 없이 그대로 표시하므로 화면 출력용으로 적합합니다.\n\n[대표 Shader 2: Unlit/Texture]\nBuilt-in Render Pipeline에서 자주 사용하는 단순 Texture 표시 Shader입니다.\nTexture 슬롯은 Main Texture 또는 MainTex로 표시될 수 있습니다.\nURP 프로젝트에서는 분홍색으로 보이거나 호환되지 않을 수 있으므로 URP/Unlit을 우선 사용합니다.\n\n[대표 Shader 3: Universal Render Pipeline/Lit]\n3D Mesh가 빛과 그림자의 영향을 받게 만드는 Shader입니다.\nTexture 슬롯은 Base Map입니다.\nQuad가 Light 영향을 받아 어두워질 수 있어 최종 화면 출력에는 보통 사용하지 않습니다.\n표면 재질을 표현하는 3D 오브젝트에 적합합니다.\n\n[대표 Shader 4: Sprite-Lit-Default]\nURP 2D Renderer에서 Sprite가 2D Light 영향을 받게 하는 Shader입니다.\nSprite Renderer용 캐릭터나 배경에 적합합니다.\n일반 3D Quad의 최종 화면 출력용으로는 URP/Unlit이 더 단순합니다.\n\n[대표 Shader 5: Sprite-Unlit-Default]\nSprite를 2D Light 영향 없이 표시하는 Shader입니다.\nSprite Renderer에서 원래 색을 유지할 때 사용합니다.\nMesh Renderer를 사용하는 Quad와 Sprite Renderer의 용도를 구분해야 합니다.\n\n[이 프로젝트의 BarrelDistortion Shader]\n이 프로젝트의 굴곡 화면 전용 Shader입니다.\nMaterial Inspector의 Texture 슬롯 이름은 Game Screen입니다.\nGame Screen 슬롯에 TutorialRenderTexture가 연결됩니다.\nOriginal Background 슬롯에는 굴곡 뒤에 보일 배경 Texture가 연결됩니다.\nVertical Curve는 세로 굴곡, Horizontal Warp는 좌우 왜곡을 조절합니다.\nCurve가 0이면 일반 평면 출력처럼 보입니다.\n이 Shader는 코드에서 Resources로 불러와 런타임 Material에 연결합니다.\n\n[URP/Unlit에서 Texture 넣기]\n1. Project 창에서 Material을 선택합니다.\n2. Shader를 Universal Render Pipeline/Unlit으로 선택합니다.\n3. Surface Inputs 또는 Base Map 항목을 찾습니다.\n4. Base Map 왼쪽의 작은 Texture 칸에 TutorialRenderTexture를 드래그합니다.\n5. Quad의 Mesh Renderer → Element 0에 이 Material을 넣습니다.\n\n[BarrelDistortion에서 Texture 넣기]\n1. BarrelDistortion Shader를 사용하는 Material을 선택합니다.\n2. Game Screen 슬롯을 찾습니다.\n3. TutorialRenderTexture를 Game Screen에 드래그합니다.\n4. Curve 0으로 먼저 평면 출력이 되는지 확인합니다.\n5. 연결 확인 후 Curve 값을 올려 굴곡을 적용합니다.\n\n[Texture 슬롯이 안 보일 때]\nElement 0의 Material이 아니라 Material 에셋 자체를 선택했는지 확인합니다.\nMaterial Inspector의 항목이 접혀 있다면 Surface Inputs 등을 펼칩니다.\nInspector가 아래쪽에 있다면 스크롤을 끝까지 내립니다.\n선택한 Shader에 Texture Property가 없으면 Texture 슬롯도 표시되지 않습니다.\nURP Shader가 분홍색이면 현재 Render Pipeline과 Shader 호환성을 확인합니다.";
    }

    private static string DetailedCurveShaderReferenceText()
    {
        return "[5단계의 목적]\nBarrelDistortion Shader로 최종 화면의 UV를 바꿔 원통형 굴곡을 만듭니다.\n실제 Tilemap Transform과 Collider는 움직이지 않고 보이는 화면만 변합니다.\n5단계부터 기본 Curve 0.48과 Horizontal Warp 0.24를 사용합니다.\n\n[Shader란?]\nShader는 GPU에서 정점과 픽셀의 최종 표시 방법을 계산하는 프로그램입니다.\nVertex 단계는 Mesh 정점의 화면 위치를 계산합니다.\nFragment 단계는 화면의 각 픽셀 색상을 계산합니다.\n이 프로젝트의 Shader는 픽셀을 읽을 Texture 좌표를 바꿉니다.\n\n[Material과 Shader]\nShader는 계산 규칙이고 Material은 Shader와 입력값을 저장하는 에셋입니다.\nMaterial의 Shader 항목에서 사용할 Shader를 선택합니다.\nShader의 Properties에 선언된 값은 Material Inspector에 표시됩니다.\n같은 Shader를 사용해도 Material마다 Curve 값을 다르게 저장할 수 있습니다.\n\n[BarrelDistortion Shader 위치]\nProject → CameraTutorial → 05_CurveShader → Resources → BarrelDistortion.shader\nShader 이름은 Hidden/Tutorial/BarrelDistortion입니다.\nResources 폴더에 있어 Development Build에도 포함됩니다.\n\n[UV 좌표]\nUV는 Texture의 위치를 나타내는 0~1 좌표입니다.\n왼쪽 아래는 보통 (0,0), 오른쪽 위는 (1,1)입니다.\n중앙은 (0.5,0.5)입니다.\nTexture 자체를 바꾸지 않고 UV를 바꾸면 다른 위치의 픽셀을 읽어 왜곡할 수 있습니다.\n\n[중앙 기준 좌표]\nUV에서 0.5를 빼면 화면 중앙이 0이 됩니다.\n왼쪽은 음수, 오른쪽은 양수 좌표가 됩니다.\n굴곡 계산은 중앙에서의 거리를 사용해 가장자리 변형을 크게 만듭니다.\n\n[Vertical Curve]\nMaterial Inspector에서 Vertical Curve 항목을 찾습니다.\n실제 Shader Property 이름은 _Curve입니다.\n0이면 원래 UV를 사용해 평평한 화면으로 표시됩니다.\n양수 값을 높이면 화면이 원통 안쪽처럼 굽어 보입니다.\n이 Shader의 Inspector 범위는 -0.8에서 0.8입니다.\n처음에는 0, 0.2, 0.48 순서로 비교하는 것이 좋습니다.\n\n[Horizontal Warp]\nMaterial Inspector에서 Horizontal Warp 항목을 찾습니다.\n실제 Shader Property 이름은 _HorizontalWarp입니다.\n좌우 가장자리의 수평 이동과 원근감을 보조합니다.\n0이면 추가 좌우 왜곡이 없습니다.\n이 Shader의 범위는 0에서 0.5입니다.\nCurve가 0이면 Horizontal Warp 효과도 함께 약해집니다.\n\n[Game Screen Texture]\nGame Screen은 Main Camera 결과가 저장된 Render Texture 슬롯입니다.\n실제 Shader Property 이름은 _MainTex입니다.\nTutorialRenderTexture가 연결되어야 게임 화면을 굴곡시킬 수 있습니다.\n비어 있으면 검은색 또는 기본 Texture가 표시될 수 있습니다.\n\n[Original Background]\nOriginal Background는 굴곡 바깥 영역에 표시할 배경 Texture입니다.\n실제 Shader Property 이름은 _BackgroundTex입니다.\n굴곡으로 Game Screen UV가 0~1 범위를 벗어나면 배경을 반환합니다.\n\n[Material Inspector에서 조절하기]\n1. Project 창에서 BarrelDistortion Material을 선택합니다.\n2. Shader가 Hidden/Tutorial/BarrelDistortion인지 확인합니다.\n3. Game Screen에 TutorialRenderTexture를 연결합니다.\n4. Vertical Curve를 0으로 두고 평면 화면을 확인합니다.\n5. Vertical Curve를 천천히 올려 굴곡을 비교합니다.\n6. Horizontal Warp로 좌우 왜곡을 조절합니다.\n\n[코드에서 값 바꾸기]\nMaterial.SetFloat(\"_Curve\", 0.48f)로 Curve를 변경할 수 있습니다.\nMaterial.SetFloat(\"_HorizontalWarp\", 0.24f)로 좌우 왜곡을 변경합니다.\nMaterial.GetFloat(\"_Curve\")로 현재 값을 읽을 수 있습니다.\n런타임 UI Slider 값을 SetFloat에 전달하면 게임 안에서 실시간 조절할 수 있습니다.\n\n[공유 Material 주의]\nRenderer.material은 해당 Renderer용 Material 인스턴스를 만들 수 있습니다.\nRenderer.sharedMaterial은 에셋 Material을 공유합니다.\nPlay Mode에서 에셋 값을 직접 바꾸지 않으려면 런타임 Material 인스턴스를 사용합니다.\n이 프로젝트는 실행 중 새 Material을 만들어 값을 적용합니다.\n\n[화면과 물리의 차이]\nShader 굴곡은 Render Texture 픽셀만 변형합니다.\nTilemap Cell, Sprite Transform, Rigidbody2D, Collider2D는 원래 위치에 있습니다.\n굴곡이 강하면 보이는 발판과 물리 위치가 다르게 느껴질 수 있습니다.\n따라서 1단계 편집은 Curve 0의 평면 화면에서 진행합니다.\n\n[Curve가 적용되지 않을 때]\nMaterial의 Shader가 BarrelDistortion인지 확인합니다.\nGame Screen에 TutorialRenderTexture가 연결됐는지 확인합니다.\nVertical Curve 값이 0으로 남아 있지 않은지 확인합니다.\nScreen Quad의 Mesh Renderer에 올바른 Material이 연결됐는지 확인합니다.\n최종 Camera가 Screen Quad Layer를 촬영하는지 확인합니다.\n\n[분홍색 화면일 때]\nShader 컴파일 오류 또는 Render Pipeline 호환 문제를 확인합니다.\nConsole의 Shader error를 확인합니다.\n빌드에서만 찾지 못하면 Resources 또는 Material 참조로 Shader 포함을 보장합니다.\n\n[권장 테스트 순서]\nCurve 0으로 Render Texture 연결부터 확인합니다.\nCurve 0.2로 약한 굴곡을 확인합니다.\nCurve 0.48로 기본 굴곡을 확인합니다.\nHorizontal Warp를 0과 0.24로 비교합니다.\n플레이어 이동과 Tilemap 충돌이 그대로 유지되는지 확인합니다.";
    }

    private static string RemoveReferenceSection(string text, string sectionStart, string nextSection)
    {
        int start = text.IndexOf(sectionStart, System.StringComparison.Ordinal);
        int next = text.IndexOf(nextSection, System.StringComparison.Ordinal);
        if (start < 0 || next <= start)
            return text;
        return text.Remove(start, next - start);
    }

    private static string DetailedLayerSeparationReferenceText()
    {
        return "[6단계의 목적]\n굴곡 Shader가 적용된 맵과 정상 모양의 플레이어를 서로 다른 Camera로 분리합니다.\nI를 눌러 플레이어가 굴곡 화면에 포함된 상태와 Overlay로 분리된 상태를 비교합니다.\n\n[플레이어가 함께 굴곡되는 이유]\nMain Camera가 맵과 플레이어를 함께 Render Texture에 저장하면 두 이미지가 하나로 합쳐집니다.\nBarrelDistortion Shader는 Render Texture 전체의 UV를 바꿉니다.\n따라서 같은 Render Texture 안에 있는 플레이어 픽셀도 맵과 함께 굴곡됩니다.\n플레이어만 정상으로 표시하려면 굴곡 처리 전에 별도로 촬영해야 합니다.\n\n[Unity Layer 열기]\nPlayer GameObject를 선택합니다.\nInspector 상단의 Layer 메뉴를 엽니다.\n새 Layer가 필요하면 Add Layer를 선택합니다.\n빈 User Layer에 TutorialPlayerOverlay 같은 이름을 입력합니다.\n다시 Player를 선택해 해당 Layer를 지정합니다.\n자식 오브젝트에도 적용할지 묻는 창에서는 플레이어 구조에 맞게 선택합니다.\n\n[Layer와 Sorting Layer 차이]\nLayer는 Camera Culling Mask와 물리 충돌 분리에 사용합니다.\nSorting Layer는 Sprite Renderer가 앞뒤로 그려지는 순서를 정합니다.\nCamera에서 플레이어만 촬영하려면 Sorting Layer가 아니라 일반 Layer를 사용해야 합니다.\n두 설정의 이름이 비슷하지만 역할은 다릅니다.\n\n[Main Camera에서 Player 제외]\nHierarchy에서 Main Camera를 선택합니다.\nInspector → Camera → Rendering → Culling Mask를 엽니다.\nTutorialPlayerOverlay Layer의 체크를 해제합니다.\n이제 Main Camera는 맵을 Render Texture에 저장하지만 플레이어는 저장하지 않습니다.\nPlayer Camera가 아직 없다면 플레이어가 잠시 화면에서 사라질 수 있습니다.\n\n[Player Camera 만들기]\nHierarchy 우클릭 → Camera로 새 Camera를 만듭니다.\n이름을 Player Camera로 변경합니다.\nProjection을 Orthographic으로 설정합니다.\nMain Camera와 같은 Position, Rotation, Orthographic Size를 사용합니다.\n카메라 범위가 다르면 플레이어와 맵 위치가 어긋납니다.\n\n[Player Camera Culling Mask]\nPlayer Camera → Rendering → Culling Mask를 엽니다.\nNothing을 선택한 뒤 TutorialPlayerOverlay Layer만 체크합니다.\nPlayer Camera에는 맵, 배경, Screen Quad가 보이면 안 됩니다.\n플레이어 Sprite만 보이는지 확인합니다.\n\n[Base Camera]\nURP에서 Base Camera는 화면의 기본 배경과 첫 번째 결과를 그립니다.\n이 프로젝트에서는 Distortion Camera가 굴곡된 Screen Quad를 최종 화면에 그리는 Base Camera입니다.\nBase Camera는 Overlay Camera를 Stack으로 받을 수 있습니다.\n\n[Overlay Camera]\nPlayer Camera를 선택합니다.\nInspector의 Render Type을 Overlay로 변경합니다.\nOverlay Camera는 새 배경을 지우지 않고 Base Camera 결과 위에 내용만 추가합니다.\n그래서 굴곡된 맵 위에 정상 플레이어 Sprite를 합성할 수 있습니다.\n\n[Camera Stack 연결]\nHierarchy에서 Distortion Camera를 선택합니다.\nInspector의 Stack 또는 Camera Stack 목록을 찾습니다.\n목록의 추가 버튼으로 Player Camera를 넣습니다.\n순서는 위에서 아래로 합성되므로 필요한 Overlay 순서를 확인합니다.\nPlayer Camera가 Overlay가 아니면 Stack에 추가되지 않을 수 있습니다.\n\n[Camera 설정 맞추기]\nBase와 Overlay Camera의 Viewport Rect를 같게 맞춥니다.\nHDR와 MSAA 설정도 호환되도록 맞춥니다.\nOrthographic Size와 Transform이 다르면 플레이어 위치가 어긋납니다.\nPlayer Camera는 Base Camera보다 독립적으로 Display에 직접 출력하지 않습니다.\n\n[I 비교 체험]\nI를 누르기 전에는 플레이어가 Main Camera의 Render Texture 안에 포함됩니다.\n맵과 플레이어가 함께 굴곡되어 Layer 분리 전 상태를 볼 수 있습니다.\nI를 누르면 Main Camera에서 Player Layer를 제외하고 Player Camera를 활성화합니다.\nPlayer Camera 결과가 Camera Stack 위에 합성되어 플레이어가 정상 모양으로 보입니다.\n두 상태를 비교하면 Layer와 Overlay Camera의 역할을 확인할 수 있습니다.\n\n[플레이어가 안 보일 때]\nPlayer의 Layer가 TutorialPlayerOverlay인지 확인합니다.\nPlayer Camera Culling Mask가 같은 Layer를 포함하는지 확인합니다.\nPlayer Camera GameObject와 Camera 컴포넌트가 활성화됐는지 확인합니다.\nPlayer Camera가 Distortion Camera Stack에 들어 있는지 확인합니다.\n\n[플레이어가 계속 굴곡될 때]\nMain Camera Culling Mask에서 Player Layer가 제외됐는지 확인합니다.\n플레이어 자식 Sprite가 다른 Layer에 남아 있지 않은지 확인합니다.\nMain Camera와 Player Camera가 플레이어를 동시에 촬영하지 않는지 확인합니다.\n\n[배경이 플레이어 위를 덮을 때]\nPlayer Camera Render Type이 Overlay인지 확인합니다.\nBase Camera의 Stack에 Player Camera가 들어 있는지 확인합니다.\nPlayer Camera의 Clear Flags나 Background 설정으로 화면을 다시 지우지 않는지 확인합니다.\n\n[플레이어가 두 명처럼 보일 때]\nMain Camera와 Player Camera가 모두 Player Layer를 촬영하면 두 번 표시됩니다.\nMain Camera에서는 Player Layer를 제외하고 Player Camera에서만 포함합니다.\nCamera Stack에 같은 Player Camera가 중복 등록되지 않았는지도 확인합니다.\n\n[최종 연결]\nMain Camera → 맵만 Render Texture에 저장\nRender Texture → BarrelDistortion → Screen Quad\nDistortion Camera → 굴곡된 맵을 Base 화면으로 출력\nPlayer Camera → 플레이어만 촬영\nCamera Stack → 굴곡된 맵 위에 정상 플레이어 합성";
    }

    private static string DetailedWorkflowReferenceText()
    {
        return "[1~6단계 전체 실행 순서]\n아래 순서는 Unity Editor에서 평면 Tilemap을 만들고 굴곡 화면과 정상 플레이어를 출력하는 작업 흐름입니다.\n각 단계가 끝난 뒤 다음 단계로 이동하면 연결 문제를 찾기 쉽습니다.\n\n[1단계: Tile과 Tile Palette 준비]\n1. Window → 2D → Tile Palette를 엽니다.\n2. Create New Palette로 Palette를 만듭니다.\n3. Sprite를 Palette로 드래그해 Tile 에셋을 만듭니다.\n4. Hierarchy에서 Grid 아래 Ground Tilemap을 선택합니다.\n5. Tile Palette의 Active Tilemap이 Ground인지 확인합니다.\n6. Brush로 발판을 그리고 Eraser로 잘못 그린 타일을 지웁니다.\n7. Scene에서 Tilemap 모양을 확인합니다.\n\n[1단계 확인]\nGround Tilemap에 원하는 발판이 보이는지 확인합니다.\nTilemap Collider가 있다면 플레이어가 발판 위에 서는지 확인합니다.\n맵 제작 중에는 화면 굴곡을 0으로 두는 것이 편합니다.\n\n[2단계: Main Camera 설정]\n1. Hierarchy에서 Main Camera를 선택합니다.\n2. Tag가 MainCamera인지 확인합니다.\n3. Projection을 Orthographic으로 설정합니다.\n4. Orthographic Size로 화면 범위를 정합니다.\n5. Transform Position을 맵과 플레이어 위치에 맞춥니다.\n6. Culling Mask에 맵 Layer가 포함됐는지 확인합니다.\n7. Near와 Far 사이에 Tilemap Z 위치가 들어가는지 확인합니다.\n\n[2단계 확인]\nGame 창에서 맵과 플레이어가 함께 보이는지 확인합니다.\n플레이어 이동 시 Camera가 X/Y 위치를 따라가는지 확인합니다.\n화면이 비면 Camera 활성화, Culling Mask, Z, Near/Far를 확인합니다.\n\n[3단계: Render Texture 만들기]\n1. Project 창 우클릭 → Create → Rendering → Render Texture를 선택합니다.\n2. 이름을 TutorialRenderTexture로 변경합니다.\n3. Size를 Game View와 같은 화면 비율로 설정합니다.\n4. Hierarchy에서 Main Camera를 선택합니다.\n5. Camera → Output → Output Texture를 찾습니다.\n6. TutorialRenderTexture를 Output Texture 슬롯에 넣습니다.\n7. Render Texture Inspector Preview를 확인합니다.\n\n[3단계 확인]\nPreview에 Main Camera 화면이 보이면 연결된 것입니다.\nMain Camera는 이제 Display가 아니라 Render Texture에 결과를 저장합니다.\nGame 창이 비어도 Preview가 보인다면 다음 출력 단계가 아직 없는 상태입니다.\n\n[4단계: Material과 Quad 만들기]\n1. Project 창 우클릭 → Create → Material을 선택합니다.\n2. 화면 출력에는 URP/Unlit 같은 Unlit Shader를 선택합니다.\n3. Material의 Base Map 또는 Texture 슬롯에 TutorialRenderTexture를 넣습니다.\n4. Hierarchy 우클릭 → 3D Object → Quad를 만듭니다.\n5. Quad의 Mesh Renderer → Materials → Element 0에 Material을 넣습니다.\n6. 화면용 Quad에서 불필요한 Collider를 제거합니다.\n7. Quad에 Screen 전용 Layer를 지정합니다.\n\n[4단계: 최종 Camera 만들기]\n1. Hierarchy 우클릭 → Camera로 Output Camera를 만듭니다.\n2. Projection을 Orthographic으로 설정합니다.\n3. Culling Mask는 Screen Layer만 선택합니다.\n4. Camera가 Quad를 정면으로 촬영하도록 배치합니다.\n5. Output Texture는 None으로 둡니다.\n6. Target Display를 Display 1로 설정합니다.\n7. Quad Scale과 Camera Size를 화면 비율에 맞춥니다.\n\n[4단계 확인]\n연결은 Main Camera → Render Texture → Material → Quad → Output Camera → Display 순서입니다.\n검은 화면이면 Render Texture Preview, Material Texture, Quad Renderer, Camera Culling Mask 순서로 확인합니다.\nMain Camera는 Screen Layer를 촬영하지 않도록 설정합니다.\n\n[5단계: 굴곡 Shader 연결]\n1. BarrelDistortion Shader를 사용하는 Material을 준비합니다.\n2. Game Screen 슬롯에 TutorialRenderTexture를 연결합니다.\n3. Original Background 슬롯에 배경 Texture를 연결합니다.\n4. Quad의 Element 0에 굴곡 Material을 넣습니다.\n5. Vertical Curve를 0으로 두고 평면 출력부터 확인합니다.\n6. Vertical Curve를 올려 화면 굴곡을 확인합니다.\n7. Horizontal Curve로 좌우 왜곡을 조절합니다.\n\n[5단계 확인]\n화면만 굴곡되고 Tilemap과 Collider는 원래 위치에 있어야 합니다.\nCurve가 적용되지 않으면 Shader, Game Screen Texture, Quad Material을 확인합니다.\n분홍색 화면이면 Shader 컴파일과 Render Pipeline 호환성을 확인합니다.\n\n[6단계: Player Layer 분리]\n1. Add Layer에서 TutorialPlayerOverlay Layer를 만듭니다.\n2. Player와 필요한 자식 Sprite에 해당 Layer를 지정합니다.\n3. Main Camera Culling Mask에서 Player Layer를 제외합니다.\n4. 새 Player Camera를 만듭니다.\n5. Player Camera Culling Mask는 Player Layer만 선택합니다.\n6. Main Camera와 같은 Transform과 Orthographic Size를 사용합니다.\n7. Player Camera Render Type을 Overlay로 설정합니다.\n\n[6단계: Camera Stack 연결]\n1. 굴곡 Quad를 출력하는 Base Camera를 선택합니다.\n2. Camera Stack에 Player Camera를 추가합니다.\n3. Base와 Overlay Camera의 Viewport, HDR, MSAA 설정을 맞춥니다.\n4. Player Camera가 Display에 단독 출력하지 않는지 확인합니다.\n5. 굴곡된 맵 위에 정상 플레이어가 합성되는지 확인합니다.\n\n[6단계 확인]\n플레이어가 안 보이면 Layer, Player Camera Culling Mask, Camera Stack을 확인합니다.\n플레이어가 두 명이면 Main Camera에서도 Player Layer를 촬영하는지 확인합니다.\n플레이어까지 굴곡되면 Main Camera Culling Mask에서 Player Layer를 제외합니다.\n\n[최종 연결 구조]\nGround Tilemap → Main Camera\nMain Camera → TutorialRenderTexture\nTutorialRenderTexture → BarrelDistortion Material\nMaterial → Screen Quad\nScreen Quad → Output Base Camera → Display 1\nPlayer → Player Camera → Base Camera Stack\n\n[최종 점검 순서]\n1. Tilemap과 Collider\n2. Main Camera 범위와 추적\n3. Render Texture Preview\n4. Material과 Quad 연결\n5. Output Camera Display 출력\n6. Curve 값과 화면 굴곡\n7. Player Layer와 Overlay Camera\n8. Console Error 확인";
    }

    private void DrawRestartHelp() => GUI.Box(new Rect(16f, Screen.height - 108f, 360f, 30f), "R: 시작 지점으로 복귀 + 카메라 중앙 정렬");

    private string ReferenceFileName() => mission switch
    {
        1 => "Tilemap", 2 => "MainCamera", 3 => "RenderTexture", 4 => "ScreenQuad",
        5 => "CurveShader", 6 => "LayerSeparation", 7 => "Guide", _ => "Complete"
    };

    private void DrawCompletionBar()
    {
        float y = Screen.height - 72f;
        GUI.Box(new Rect(16, y, Screen.width - 32f, 56f), "목표");
        GUI.Label(new Rect(32, y + 24f, Screen.width - 64f, 24f),
            goalAchieved[mission] ? "O  목표 달성" : Check());
        if (!completed) return;
        GUIStyle style = new GUIStyle(GUI.skin.box) { fontSize = 44, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, font = readableFont };
        GUI.Box(new Rect(Screen.width / 2f - 210f, Screen.height / 2f - 60f, 420f, 120f), "COMPLETE!", style);
    }

    private void CompleteTutorial()
    {
        completed = true;
        PlayerPrefs.SetInt("CameraTutorial.Completed", 1);
        PlayerPrefs.Save();
    }

    private void TryCompleteCurrentGoal()
    {
        if (goalAchieved[mission] || !IsCurrentGoalComplete())
            return;

        goalAchieved[mission] = true;
        CreateFireworks(transform.position);
    }

    private bool IsCurrentGoalComplete()
    {
        switch (mission)
        {
            case 1:
                if (mapEditor == null)
                    mapEditor = FindAnyObjectByType<RuntimeTilemapEditor>();
                return mapEditor != null && mapEditor.PaintedTileCount >= 5;
            case 2:
                return showReference && referencePages.Length > 0 &&
                    referencePage == referencePages.Length - 1;
            case 3:
                if (renderTextureDoor == null)
                    renderTextureDoor = FindAnyObjectByType<RuntimeRenderTextureDoor>();
                return renderTextureDoor != null && renderTextureDoor.IsSucceeded;
            case 4:
                if (screenOutputQuiz == null)
                    screenOutputQuiz = FindAnyObjectByType<RuntimeScreenOutputQuiz>();
                return screenOutputQuiz != null && screenOutputQuiz.IsSucceeded;
            case 5:
                if (curveControl == null)
                    curveControl = FindAnyObjectByType<RuntimeCurveControl>();
                return curveControl != null && curveControl.IsSucceeded;
            case 6:
                return playerOverlayRestored;
            case 7:
                return missionSevenWPressed;
            case 8:
                return completed;
            default:
                return false;
        }
    }

    private static void CreateFireworks(Vector3 center)
    {
        for (int burst = 0; burst < 3; burst++)
        {
            GameObject firework = new GameObject($"COMPLETE FIREWORK {burst + 1}");
            firework.transform.position = center + new Vector3((burst - 1) * 1.5f, 1.5f, 0f);
            ParticleSystem particles = firework.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 1.5f; main.loop = false; main.startLifetime = 1.2f; main.startSpeed = 4.5f; main.startSize = 0.12f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = burst == 0 ? Color.yellow : burst == 1 ? Color.cyan : Color.magenta;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });
            ParticleSystem.ShapeModule shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.15f;
            particles.Play(); Destroy(firework, 2.5f);
        }
    }

    private string Title() => mission switch
    {
        1 => "Tilemap 직접 꾸미기", 2 => "Main Camera 이해하기", 3 => "RenderTexture 연결하기",
        4 => "최종 출력 카메라 완성", 5 => "굴곡 Shader 이해하기", 6 => "플레이어 화면 복구",
        7 => "카메라 구조 정리", _ => "최종 플레이 테스트"
    };

    private string Goal() => mission switch
    {
        1 => "Rock 타일 5개 제작",
        2 => "Main Camera 유니티 엔진 설명서 읽어보기",
        3 => "플레이어(RenderTexture)를 문(OUTPUT Texture)에 넣으세요",
        4 => "퀴즈를 맞추세요",
        5 => "Curve 값을 조절하세요",
        6 => "I를 눌러 비교해보기",
        7 => "W 누르기",
        _ => "이동, 점프, 충돌, 카메라 추적을 실제 게임처럼 테스트합니다."
    };

    private string Instructions() => mission switch
    {
        1 => "마우스 왼쪽 클릭 또는 드래그로 빈 셀에 Rock 타일 5개를 제작하세요.",
        2 => "W로 설명서를 열고 N 또는 스크롤을 사용해 마지막 페이지까지 읽어보세요.",
        3 => "RT 플레이어를 OUTPUT 문에 넣으세요.   T: 문 랜덤 위치 변경 (1~5칸)",
        4 => "T로 OX 퀴즈를 시작하세요. O는 맞다, X는 틀리다이며 5문제 중 3문제를 맞히세요.",
        5 => "Vertical Curve와 Horizontal Curve 슬라이더를 각각 움직여 화면 변화를 비교하세요.",
        6 => "I를 눌러 굴곡된 플레이어와 Overlay로 분리된 정상 플레이어를 비교하세요.",
        7 => "W를 눌러 1~6단계 전체 실행 순서 설명서를 여세요.",
        _ => "A/D 이동, Z 점프, R 복귀를 시험한 뒤 문제가 없으면 Enter를 누르세요."
    };

    private string Check() => mission switch
    {
        1 => "빈 셀에 Rock 타일 5개를 제작하면 목표가 자동으로 달성됩니다.",
        2 => "Main Camera 설명서의 마지막 페이지까지 이동하면 목표가 자동으로 달성됩니다.",
        3 => "플레이어가 OUTPUT Texture 문과 충돌하면 목표가 자동으로 달성됩니다.",
        4 => "5문제 중 3문제 이상 정답이면 목표가 자동으로 달성됩니다.",
        5 => "Curve 슬라이더 중 하나의 값만 조절해도 목표가 자동으로 달성됩니다.",
        6 => playerOverlayRestored ? "O  I 비교를 완료했습니다." : "I를 누르면 목표가 자동으로 달성됩니다.",
        7 => "W를 누르면 목표가 자동으로 달성됩니다.",
        _ => "모든 조작이 정상이라면 Enter를 눌러 완료하세요."
    };

}
