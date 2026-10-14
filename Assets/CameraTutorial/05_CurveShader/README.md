# 5단계 — 원통 굴곡 Shader

사용할 파일 위치: `CameraTutorial > 05_CurveShader > Resources > BarrelDistortion.shader`

Shader는 오브젝트 Transform이 아니라 화면의 UV 좌표를 변경합니다. 실제 Tilemap과 Collider는 직선이며, 보이는 픽셀만 원통 안쪽처럼 재배치됩니다.

완료 조건: `_Curve`가 0이면 평면이고 값이 커질수록 굴곡이 강해집니다.
