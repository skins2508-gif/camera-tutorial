# 3단계 — RenderTexture

RenderTexture는 카메라가 모니터 대신 그림을 저장하는 GPU 화면입니다. 이 튜토리얼에서는 `Assets/Resources/TutorialRenderTexture`로 제공된 에셋을 사용합니다. 같은 이름으로 새 에셋을 만들지 않습니다.

연결 위치: `Hierarchy > [02] CAMERAS > Main Camera > Inspector > Camera > Output > Output Texture`

만든 RenderTexture 에셋을 `Output Texture` 칸으로 드래그합니다. 연결 뒤 Game 창에 `No cameras rendering`이 뜨는 것은 정상입니다. Project 창에서 RenderTexture를 선택한 뒤 Inspector 아래쪽 Preview에서 카메라 화면을 확인합니다.

`TutorialRenderTexture`의 Inspector에서 Size를 `1280 × 720`으로 설정합니다. 256×256은 정사각형이라 16:9 Game 화면에서 가로로 늘어나고 흐릿하게 보입니다.

완료 조건: RenderTexture 에셋을 만들고 Main Camera의 `Output Texture`에 넣을 수 있습니다.

이름 구분: Unity 6 Inspector의 항목 이름은 `Output Texture`이고, C#에서 같은 항목을 조작하는 속성 이름은 `camera.targetTexture`입니다.
