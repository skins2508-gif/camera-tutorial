// CameraTutorial → 03_RenderTexture
// 1. Play 중지
// 단계 진입 시 2단계에서 바꾼 Camera Size와 위치는 기본값으로 복구된다.
// 2. Project → Resources 폴더 열기
// 3. 제공된 TutorialRenderTexture 선택 (같은 이름으로 새로 만들지 않기)
// 4. Inspector → Size를 1280 × 720으로 변경
// 5. 1280 × 720은 Game 화면과 같은 16:9 비율이라 화면이 찌그러지지 않는다.
// 6. Hierarchy → [02] CAMERAS → Main Camera를 선택
// 7. Inspector → Camera 컴포넌트 → Output 항목을 펼치기
// 8. Output Texture 칸에 TutorialRenderTexture를 드래그해서 넣기
// 9. Project 창에서 TutorialRenderTexture 선택
// 10. Inspector 아래 Preview에서 맵 화면 확인
//
// [Play 중지 상태에서 Game 창이 보이는 이유]
// Main Camera는 TutorialRenderTexture로 출력하므로 원래는 Display 1 화면이 비게 된다.
// CameraEditModePreview.cs가 Play 중지 상태에서만 임시 Preview Camera를 하나 만든다.
// Preview Camera는 Main Camera의 위치·크기를 복사하고 Output Texture = None으로 Display 1에 출력한다.
// Preview Camera는 Hierarchy에서 숨겨지며 Scene과 빌드에 저장되지 않는다.
// Play를 누르면 Preview Camera는 자동 삭제된다.
// 완료: 카메라 화면이 RenderTexture에 저장된다.
