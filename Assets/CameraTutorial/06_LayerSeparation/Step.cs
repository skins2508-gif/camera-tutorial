// CameraTutorial → 06_LayerSeparation
// 6단계 목표: 굴절된 캐릭터가 생기는 원인을 확인하고 정상 모습으로 보이도록 끝까지 제작한다.
// 완성 결과: 맵과 배경은 원통 안쪽처럼 휘고 플레이어는 굴절되지 않은 정상 모습으로 보인다.
//
// [1. 굴절된 캐릭터 문제 확인]
// 1. Play 실행
// 2. 숫자 6 누르기
// 3. 플레이어를 좌우로 이동하거나 점프한다.
// 4. Game 화면에서 캐릭터가 맵과 함께 늘어나거나 휘어 보이는지 확인한다.
// 5. 이 문제는 Player Transform이나 Sprite가 실제로 변형된 것이 아니다.
// 6. Main Camera가 맵과 플레이어를 같은 RenderTexture에 저장하기 때문에 발생한다.
// 7. BarrelDistortion Shader가 RenderTexture 전체를 굽혀 캐릭터 픽셀도 함께 굴절시킨다.
//
// [2. Player 전용 Layer 확인]
// 8. Hierarchy에서 03_Player - 실제 캐릭터 선택
// 9. Inspector 맨 위에서 Tag 오른쪽의 Layer 드롭다운 클릭
// 10. TutorialPlayerOverlay 선택
// 11. Change Layer 창이 나오면 Yes, change children 선택
// 12. Inspector의 Layer 칸에 TutorialPlayerOverlay가 표시되는지 확인
// 13. Layer는 카메라가 어떤 오브젝트를 촬영할지 나누는 분류표 역할을 한다.
//
// [3. Main Camera에서 Player 제외]
// 14. Hierarchy에서 Main Camera 선택
// 15. Inspector → Camera → Rendering → Culling Mask 열기
// 16. TutorialPlayerOverlay의 체크를 해제
// 17. Main Camera의 Output Texture가 TutorialRenderTexture인지 확인
// 18. 이제 Main Camera는 맵을 RenderTexture에 저장하지만 Player는 저장하지 않는다.
// 19. 이 상태에서 별도 Player Camera가 꺼져 있으면 Game 화면에서 Player가 사라질 수 있다.
//
// [4. Player Camera 찾기]
// 20. Hierarchy → [05] RUNTIME OUTPUT - Play 중 자동 생성 펼치기
// 21. PLAYER CAMERA - 굴곡 없는 캐릭터 선택
// 22. Inspector에서 Camera 컴포넌트 왼쪽 체크를 켠다.
// 23. Projection이 Orthographic인지 확인
// 24. Player Camera의 위치와 Size는 코드가 Main Camera와 같게 유지한다.
//
// [5. Player Camera가 Player만 촬영하도록 설정]
// 25. Player Camera → Rendering → Culling Mask 열기
// 26. Nothing을 먼저 선택하여 모든 Layer 체크 해제
// 27. TutorialPlayerOverlay만 체크
// 28. Player Camera가 맵, 배경, Screen Quad를 함께 촬영하지 않는지 확인
//
// [6. Player Camera를 Overlay로 설정]
// 29. Player Camera Inspector 맨 위 Render Type 드롭다운 클릭
// 30. Overlay 선택
// 31. Overlay Camera는 새로운 배경을 그리지 않고 Base Camera 결과 위에 내용만 추가한다.
// 32. Player Camera를 Base로 두면 배경색이 최종 굴곡 화면을 덮을 수 있다.
//
// [7. Distortion Camera Stack에 연결]
// 33. Hierarchy에서 DISTORTION CAMERA - 최종 굴곡 화면 선택
// 34. Render Type이 Base인지 확인
// 35. Inspector → Camera → Stack 펼치기
// 36. Stack 오른쪽 아래 + 버튼 클릭
// 37. PLAYER CAMERA - 굴곡 없는 캐릭터 선택
// 38. 이미 들어 있다면 중복으로 추가하지 않는다.
// 39. 노란 output properties 경고가 보이면 Fix 버튼 클릭
// 40. Fix는 HDR, MSAA, Viewport 같은 출력 설정을 Base Camera와 맞춘다.
//
// [8. 정상 모습 완성 확인]
// 41. Game 화면으로 돌아간다.
// 42. 맵과 배경의 굴곡이 그대로 유지되는지 확인
// 43. 플레이어만 원래 비율과 모양으로 보이는지 확인
// 44. 이동과 점프 중에도 플레이어가 굴절되지 않는지 확인
// 45. 완성된 촬영 경로는 다음과 같다.
//     Main Camera → 맵만 TutorialRenderTexture에 저장 → 굴곡 Shader 적용
//     Player Camera → Player만 촬영 → Distortion Camera Stack 위에 정상 모습 합성
//
// [9. I키로 두 상태 비교]
// 46. I키를 누르면 위 설정을 코드가 정상 복구 상태로 전환한다.
// 47. I키를 다시 누르면 Main Camera가 Player를 다시 촬영하고 Player Camera가 꺼져 굴절 상태가 된다.
// 48. I키를 반복해서 눌러 굴절된 캐릭터와 정상 캐릭터를 비교한다.
// 49. I키는 학습용 비교 단축키이며 실제 원리는 Layer, Culling Mask, Overlay, Stack 설정이다.
//
// [10. 문제가 생겼을 때]
// Player가 안 보임: Player Layer와 Player Camera Culling Mask가 TutorialPlayerOverlay인지 확인
// Player가 계속 굴절됨: Main Camera Culling Mask에서 TutorialPlayerOverlay가 제외됐는지 확인
// 파란색·검은색 화면이 덮임: Player Camera Render Type이 Overlay인지 확인
// Player가 두 명처럼 보임: Main Camera가 Player를 함께 촬영하거나 Stack에 Player Camera가 중복됐는지 확인
// Stack에 추가할 수 없음: Player Camera가 Overlay이고 Distortion Camera가 Base인지 확인
// 출력 경고 표시: Distortion Camera Stack 아래의 Fix 버튼 클릭
// 설정을 다시 비교하고 싶음: 숫자 6을 다시 누른 뒤 I키 사용
//
// 완료: Main Camera는 맵만 굽히고 Player Camera는 정상 플레이어를 Overlay로 합성한다.
