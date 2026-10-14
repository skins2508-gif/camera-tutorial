# 6단계 — 굴절된 캐릭터 정상 모습까지 제작

굴절된 캐릭터가 발생하는 원인을 확인한 뒤 Player 전용 Layer, Main Camera Culling Mask, Player Overlay Camera, Distortion Camera Stack을 순서대로 설정합니다.

완성 구조는 `Main Camera → 굴곡된 맵`과 `Player Camera → 정상 플레이어`의 두 촬영 경로를 Distortion Camera에서 합성하는 방식입니다.

`I`키는 굴절 상태와 정상 상태를 번갈아 비교하는 학습용 단축키입니다. 실제 원리는 Layer 분리, Culling Mask 제외, Overlay Camera, Camera Stack 연결입니다.

완료 조건: 맵과 배경은 굴곡되며 플레이어만 정상 비율로 움직입니다.
