# 6단계 — 플레이어 Layer 분리

플레이어가 월드 RenderTexture에 포함되면 맵과 함께 찌그러집니다.

해결:

1. Player를 Layer 30으로 이동
2. 월드 카메라 Culling Mask에서 Layer 30 제외
3. 플레이어 전용 카메라에서 Layer 30만 표시

예제 코드: [Step06](../Assets/CameraTutorial/06_LayerSeparation/Step06_LayerLessonExample.cs)
