# 5단계 — Curve Shader

Shader는 실제 Tilemap Transform을 구부리지 않고 RenderTexture의 UV 좌표만 변경합니다.

`_Curve` 실습값:

- 0: 평면
- 0.2: 약한 굴곡
- 0.48: 현재 원통 안쪽 굴곡
- 0.7 이상: 화면 잘림에 주의

실제 Shader: [BarrelDistortion](../Assets/Shader/BarrelDistortion.shader)

예제 코드: [Step05](../Assets/CameraTutorial/05_CurveShader/Step05_CurveShaderLessonExample.cs)
