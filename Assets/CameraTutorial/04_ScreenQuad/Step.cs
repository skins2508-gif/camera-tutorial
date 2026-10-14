// CameraTutorial → 04_ScreenQuad
// 목표: 3단계의 RenderTexture를 최종 Game 화면에 다시 표시한다.
// 최종 흐름: Main Camera → RenderTexture → CURVED SCREEN → DISTORTION CAMERA → Display 1
//
// [실행하기]
// 1. 3단계에서 만든 TutorialRenderTexture가 Project 창에 있는지 확인
// 2. Play 실행
//    Play 직전에는 편집용 Preview Camera가 자동 삭제된다.
//    이후 실제 게임용 DISTORTION CAMERA가 Display 1 최종 출력을 담당한다.
// 3. 숫자 4 누르기
// 4. Game 창에 배경·맵·플레이어가 모두 표시되는지 확인
//
// [자동 생성된 구조 확인]
// 5. Hierarchy → [05] RUNTIME OUTPUT - Play 중 자동 생성 펼치기
// 6. 아래 항목이 모두 있는지 확인
//    - BACKGROUND - 낫베드 원본
//    - DISTORTION CAMERA - 최종 굴곡 화면
//    - CURVED SCREEN - 배럴 왜곡 셰이더
//    - PLAYER CAMERA - 굴곡 없는 캐릭터
//
// [Main Camera 확인]
// 7. Main Camera 선택
// 8. Inspector → Camera → Output → Output Texture 확인
// 9. Output Texture = TutorialRenderTexture이면 정상
//    Main Camera는 Display 1에 직접 출력하지 않고 TutorialRenderTexture에 맵 화면을 저장한다.
//
// [CURVED SCREEN 확인]
// 10. CURVED SCREEN - 배럴 왜곡 셰이더 선택
// 11. Inspector → Mesh Renderer → Materials → Element 0 확인
// 12. Element 0의 Material을 클릭하고 Game Screen 칸 확인
// 13. Game Screen = TutorialRenderTexture이면 정상
//     3단계에서 만든 같은 TutorialRenderTexture를 4단계 코드가 자동으로 연결한다.
//     CURVED SCREEN은 TutorialRenderTexture를 최종 카메라 앞에 보여주는 화면판이다.
//
// [DISTORTION CAMERA 확인]
// 14. DISTORTION CAMERA - 최종 굴곡 화면 선택
// 15. Inspector에서 Camera 컴포넌트가 켜져 있는지 확인
// 16. Camera → Output → Output Texture가 None인지 확인
//     None이어야 RenderTexture가 아닌 Display 1로 최종 화면을 출력한다.
// 17. Camera Stack에 PLAYER CAMERA가 들어 있는지 확인
// 18. 노란 경고가 보이면 경고 아래 Fix 버튼을 눌러 출력 설정을 맞춘다.
//
// [PLAYER CAMERA 확인]
// 19. PLAYER CAMERA - 굴곡 없는 캐릭터 선택
// 20. Inspector → Camera → Render Type이 Overlay인지 확인
//     PLAYER CAMERA는 굴곡되지 않은 플레이어를 최종 화면 위에 추가한다.
//
// [화면이 안 보일 때]
// 21. No cameras rendering이면 DISTORTION CAMERA의 Output Texture를 확인
//     DISTORTION CAMERA의 Output Texture는 반드시 None이어야 한다.
// 22. 파란 화면만 보이면 DISTORTION CAMERA는 켜졌지만 CURVED SCREEN을 못 보고 있는 상태
//     CURVED SCREEN의 Mesh Renderer와 Material이 켜져 있는지 확인한다.
// 23. 검은 화면이면 Main Camera가 RenderTexture에 맵을 저장하는지 확인한다.
// 24. 플레이어만 없으면 PLAYER CAMERA가 Stack에 들어 있는지 확인한다.
// 25. 설정이 꼬이면 R을 눌러 카메라 위치와 기본 굴곡값을 함께 복구한다.
//
// [직접 변경하지 않을 것]
// 26. Layer, Culling Mask, Material 텍스처 연결은 4단계 코드가 자동 처리한다.
// 27. Play 중 자동 생성 오브젝트의 변경은 Play를 중지하면 사라진다.
//
// 완료: Game 창에 배경·맵·플레이어가 보이고 최종 화면이 Display 1에 출력된다.
