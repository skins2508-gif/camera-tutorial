# 3단계 — RenderTexture

RenderTexture는 카메라 화면을 매 프레임 저장하는 GPU 텍스처입니다.

`Project 우클릭 > Create > Rendering > Render Texture`로 직접 생성합니다.

`Hierarchy > [02] CAMERAS > Main Camera`를 선택한 다음, Inspector의 `Camera > Output > Output Texture` 칸에 만든 RenderTexture를 드래그합니다.

`Output Texture`에 연결하면 카메라는 모니터 대신 RenderTexture에 그립니다. 따라서 Game 창에 `No cameras rendering`이 나오는 것은 정상이며, Project 창에서 RenderTexture를 선택한 뒤 Inspector Preview로 결과를 확인합니다.

Unity 6 Inspector에서는 `Output Texture`라고 표시합니다. C# 코드에서 같은 설정을 다룰 때만 `camera.targetTexture`라는 이름을 사용합니다.

예제 코드: [Step03](../Assets/CameraTutorial/03_RenderTexture/Step03_RenderTextureLessonExample.cs)
