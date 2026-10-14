// CameraTutorial → 07_Guide
// 목표: 완성된 카메라 구조의 각 요소가 무슨 일을 하는지 읽고 다음 확장 방향을 정리한다.
// 이 단계는 새 설정을 만드는 실습이 아니라 지금까지 만든 구조를 이해하는 마무리 설명이다.
//
// [게임 안에서 플레이어가 할 수 있는 것]
// A / D 또는 ← / → : 플레이어를 왼쪽과 오른쪽으로 이동한다.
// Z : 점프한다. 공중에서 한 번 더 눌러 2단 점프할 수 있다.
// R : 플레이어를 시작 위치로 되돌리고 카메라 위치와 굴곡값도 복구한다.
// 숫자 1~8 : 원하는 튜토리얼 단계로 바로 이동한다.
// Q : 목표, 조작 안내, 완료 확인을 포함한 게임 UI 전체를 숨기거나 다시 표시한다.
// W : 현재 단계의 유니티 엔진 설명서를 게임 중앙에 열거나 닫는다.
// 마우스 휠 / 스크롤바 : W 설명서 내용을 위아래로 이동한다.
// B : W 설명서의 이전 페이지로 이동한다.
// N : W 설명서의 다음 페이지로 이동한다.
// I : 6단계에서 굴절된 플레이어와 정상 플레이어 상태를 번갈아 비교한다.
// Enter : 8단계에서 최종 완료 문구와 폭죽을 실행한다.
//
// [플레이하면서 눈으로 확인할 수 있는 것]
// 타일 위를 이동하고 점프하며 Collider가 정상적으로 작동하는지 확인할 수 있다.
// 카메라가 플레이어를 중심으로 이동하는 모습을 확인할 수 있다.
// 맵이 원통 안쪽처럼 굴곡되지만 실제 이동과 충돌은 평면인 것을 비교할 수 있다.
// I키로 플레이어까지 굴절된 화면과 플레이어만 정상인 화면을 비교할 수 있다.
// Q로 UI를 모두 숨겨 굴곡된 맵과 배경을 방해 없이 볼 수 있다.
//
// [사용된 스크립트]
// PlayerController.cs
// TutorialCurvedWorld.cs
// CameraLearningTutorial.cs
// Step04_ScreenOutput.cs
// CameraEditModePreview.cs
//
// [사용된 Shader]
// BarrelDistortion.shader
//
// [전체 화면 흐름]
// 실제 Tilemap과 Collider → Main Camera → TutorialRenderTexture
// TutorialRenderTexture → CURVED SCREEN Material → 굴곡 Shader 적용
// CURVED SCREEN → DISTORTION CAMERA → Display 1에 굴곡된 맵 출력
// Player → PLAYER CAMERA → Distortion Camera Stack → 정상 플레이어 합성
//
// [Main Camera 기능]
// 실제 게임 월드의 Tilemap과 배경을 촬영한다.
// 촬영 결과를 Display 1이 아니라 TutorialRenderTexture에 저장한다.
// Culling Mask에서 TutorialPlayerOverlay를 제외하면 플레이어는 굴곡 화면에 들어가지 않는다.
//
// [TutorialRenderTexture 기능]
// Main Camera가 촬영한 한 프레임을 저장하는 중간 화면이다.
// Camera와 Shader 사이에서 화면을 전달하는 역할을 한다.
// 1280×720처럼 Game 화면과 같은 16:9 비율을 사용해야 찌그러짐이 적다.
// 해상도가 높으면 선명하지만 GPU와 메모리 사용량이 늘어난다.
//
// [CURVED SCREEN과 Material 기능]
// CURVED SCREEN은 RenderTexture를 보여주는 Quad 화면판이다.
// Material의 Game Screen에는 TutorialRenderTexture가 연결된다.
// Material은 BarrelDistortion Shader를 사용해 화면 픽셀의 UV 위치를 바꾼다.
// 실제 Tilemap Transform과 Collider는 휘지 않고 보이는 화면만 굽는다.
//
// [DISTORTION CAMERA 기능]
// CURVED SCREEN만 촬영하는 최종 Base Camera다.
// Output Texture = None이라 결과를 RenderTexture가 아닌 Display 1에 보낸다.
// Camera Stack에서 Player Overlay Camera의 결과를 위에 합친다.
//
// [PLAYER CAMERA 기능]
// TutorialPlayerOverlay Layer만 촬영한다.
// Render Type = Overlay라 기존 굴곡 화면을 지우지 않고 플레이어만 추가한다.
// 플레이어가 굴곡 Shader를 통과하지 않으므로 정상 비율로 보인다.
//
// [앞으로 추가하면 좋은 기능]
// 1. 부드러운 Camera Follow: SmoothDamp를 사용해 플레이어 추적의 떨림과 급정지를 줄인다.
// 2. Camera Bounds: 맵 바깥이나 검은 영역까지 카메라가 이동하지 못하도록 범위를 제한한다.
// 3. 해상도 자동 변경: Game 창 크기가 바뀌면 RenderTexture도 같은 크기로 다시 만든다.
// 4. 굴곡 Material 에셋화: Play가 끝나도 Curve 설정값이 저장되도록 Material을 Project에 만든다.
// 5. 구간별 굴곡값: 맵 위치에 따라 Curve 값을 바꾸어 방마다 다른 원통 깊이를 만든다.
// 6. 전환 애니메이션: 평면에서 굴곡 화면으로 갑자기 바뀌지 않고 값을 서서히 보간한다.
// 7. UI 전용 Overlay Camera: 설명 UI가 굴곡되지 않도록 UI만 별도 Camera로 합성한다.
// 8. 화면 흔들림: 피격이나 착지 때 최종 Camera에 짧은 Shake를 추가한다.
// 9. 체크포인트: 플레이어 위치와 Camera 상태를 함께 저장하고 R키로 복구한다.
// 10. 성능 표시: RenderTexture 해상도와 FPS를 화면에 표시해 품질과 성능을 비교한다.
//
// [최종 확인]
// 이동과 점프가 정상인지 확인한다.
// 플레이어가 바닥과 충돌하고 타일 경계에 걸리지 않는지 확인한다.
// 맵만 원통 안쪽처럼 굴곡되고 플레이어는 정상 모양인지 확인한다.
// R키로 플레이어·카메라·굴곡값이 복구되는지 확인한다.
// 모든 내용을 확인했으면 숫자 8을 눌러 최종 완료 단계로 이동한다.
//
// 완료: 각 요소의 역할과 앞으로 추가할 기능을 확인한 뒤 숫자 8로 이동한다.
// 단어 설명
// Quad: RenderTexture 영상을 보여주는 사각형 화면
// RenderTexture: 카메라 촬영 결과를 담는 실시간 텍스처
// Material: Texture와 Shader 설정을 가진 재질
// Shader: 화면 픽셀을 그리는 계산 프로그램
// UV: 이미지 안의 위치를 나타내는 0~1 좌표
// Layer: 오브젝트 분류 번호
// Culling Mask: 카메라가 표시할 Layer 선택
// Base Camera: 전체 화면을 먼저 그리는 카메라
// Overlay Camera: Base Camera 위에 추가로 그리는 카메라
// Camera Stack: Base와 Overlay 카메라의 출력 순서
// Runtime: Play 중인 상태
