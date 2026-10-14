# 4단계 — 카메라 출력 구조 자동 완성

수동으로 Camera, Quad, RenderTexture 필드를 연결하는 실습은 제거했습니다. 연결 순서가 꼬이면 맵이 사라지거나 파란 화면만 보일 수 있기 때문입니다.

4단계 코드는 다음 구조를 전부 자동으로 만듭니다.

`Main Camera → RenderTexture → Screen Quad → Distortion Camera → Display 1`

플레이어는 별도의 Player Camera를 통해 최종 화면 위에 표시됩니다.

## 사용자가 할 일

1. Play를 중지합니다.
2. `[04] SCREEN OUTPUT`에 `Step04_ScreenOutput` 컴포넌트가 있는지 확인합니다.
3. 이전 실습에서 직접 만든 `DISTORTION CAMERA`와 `SCREEN QUAD`는 삭제해도 됩니다. 남겨도 코드가 자동으로 끕니다.
4. Play를 누릅니다.
5. 숫자 `4`를 누릅니다.
6. `[05] RUNTIME OUTPUT - Play 중 자동 생성`을 펼칩니다.
7. 자동 생성된 Camera와 Screen 오브젝트를 확인합니다.

Inspector의 `Output Texture`, Material, Layer, Culling Mask는 직접 바꾸지 않아도 됩니다. 5단계부터는 완성된 카메라 구조를 유지하고 굴곡 Shader 값만 학습합니다.

4단계 최종 화면에서는 Main Camera와 CURVED SCREEN Material 양쪽에 3단계에서 만든 `TutorialRenderTexture`가 연결됩니다. 코드는 `Resources/TutorialRenderTexture`를 찾아 자동으로 양쪽에 연결합니다.

완료 조건: 배경, 맵, 플레이어가 모두 정상적으로 보이고 Hierarchy에 자동 출력 구조가 생성됩니다.
