// CameraTutorial → 05_CurveShader
// Shader 파일에는 굴곡 계산 코드가 있고, 굴곡값은 그 Shader를 사용하는 Material에서 바꾼다.
// Shader 파일을 GameObject에 넣거나 Add Component에 추가하지 않는다.
//
// 1. Play 실행
// 2. 숫자 5 누르기
// 3. Hierarchy → [05] RUNTIME OUTPUT - Play 중 자동 생성 펼치기
// 4. CURVED SCREEN - 배럴 왜곡 셰이더 선택
// 5. Inspector → Mesh Renderer → Materials 펼치기
// 6. Materials → Element 0에 들어 있는 Material을 더블 클릭
// 7. Material Inspector에서 다음 항목 찾기
//    - Game Screen
//    - Original Background
//    - Vertical Curve
//    - Horizontal Warp
// 8. Vertical Curve를 0으로 변경 → 평면 화면 확인
// 9. Vertical Curve를 0.2로 변경 → 약한 굴곡 확인
// 10. Vertical Curve를 0.48로 변경 → 기본 굴곡 확인
// 11. Horizontal Warp를 0과 0.24로 바꾸어 좌우 굴곡 비교
// 12. Project → CameraTutorial → 05_CurveShader → Resources → BarrelDistortion.shader는 계산 코드를 볼 때만 연다.
// R을 누르면 Vertical Curve 0.48 / Horizontal Warp 0.24로 복구된다.
// Play 중 만든 Material이므로 Play를 중지하면 직접 바꾼 값은 저장되지 않는다.
// 완료: Material의 Vertical Curve와 Horizontal Warp에서 화면 굴곡을 조절한다.
