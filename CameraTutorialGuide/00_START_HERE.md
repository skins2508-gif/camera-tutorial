# 굴곡 카메라 튜토리얼 — 여기서 시작

`R키`는 플레이어 재시작과 카메라 시점 복귀입니다.

이 폴더는 Unity 프로젝트의 `Assets` 밖에 있는 학습용 목차입니다. 실제 실행 파일은 Unity가 인식할 수 있도록 `Assets` 안에 유지합니다.

## 시작 순서

1. Unity에서 `Tools > Camera Tutorial > 1. Create or Open Tutorial`
2. `CameraTutorial_Working` 씬이 열렸는지 확인
3. Play
4. 화면 안내에 따라 미션 1부터 진행
5. 큰 변경 전 `2. Save Checkpoint`
6. 문제가 생기면 `3. Restore Checkpoint` 또는 `4. Reset Working Scene`

Play 중 `F2`를 누르면 이 시작 문서와 현재 단계 Markdown 문서의 전체 경로가 표시됩니다.

## 단계별 문서

1. [Tilemap](01_Tilemap.md)
2. [Main Camera](02_MainCamera.md)
3. [RenderTexture](03_RenderTexture.md)
4. [Screen Quad](04_ScreenQuad.md)
5. [Curve Shader](05_CurveShader.md)
6. [Layer Separation](06_LayerSeparation.md)
7. [Overlay Camera](07_OverlayCamera.md)
8. [Complete](08_Complete.md)
9. 기타 단어 설명: `Assets/CameraTutorial/09_단어설명/README.md`

## 실제 핵심 파일

- [미션 UI](../Assets/Script/CameraLearningTutorial.cs)
- [카메라 생성과 전환](../Assets/Script/TutorialCurvedWorld.cs)
- [플레이어 조작](../Assets/Script/PlayerController.cs)
- [원통 굴곡 Shader](../Assets/Shader/BarrelDistortion.shader)
- [튜토리얼 생성·백업·초기화](../Assets/Editor/CameraTutorialWorkspace.cs)
- [연습 씬 폴더](../Assets/CameraTutorial/Scenes)
- [배경 이미지](../Assets/Resources/NatBed.png)

## 왜 실제 파일은 Assets 안에 있어야 하나요?

Unity는 `Assets` 폴더 안에 있는 씬, C#, Shader, 이미지와 Material만 자동 임포트합니다. 실제 파일을 이 가이드 폴더로 옮기면 Unity 게임에서 사용할 수 없습니다.
