# 09 — 단어 설명

## Quad

평평한 사각형 3D 오브젝트입니다. 이 튜토리얼에서는 RenderTexture 영상을 표시하는 모니터 화면으로 사용합니다.

## RenderTexture

카메라가 촬영한 결과를 매 프레임 저장하는 GPU 텍스처입니다. 일반 PNG와 달리 게임 실행 중 계속 내용이 바뀝니다.

## Screen Quad

RenderTexture가 연결된 Material을 붙인 Quad입니다. 현재 이름은 `CURVED SCREEN - 배럴 왜곡 셰이더`입니다.

## Material

오브젝트가 화면에 어떻게 보이는지를 정하는 재질입니다. Texture와 Shader 값을 가지고 있습니다.

## Shader

픽셀과 꼭짓점을 화면에 그리는 방법을 계산하는 프로그램입니다. 현재 Shader는 맵 화면의 UV를 원통 안쪽 형태로 변경합니다.

## UV

이미지 안의 위치를 나타내는 0~1 좌표입니다. `(0,0)`은 한쪽 모서리, `(1,1)`은 반대쪽 모서리입니다.

## Orthographic Camera

거리에 따른 원근 크기 변화가 없는 카메라입니다. 2D 게임에서 주로 사용합니다.

## Layer

GameObject를 분류하는 번호입니다. 플레이어와 맵을 서로 다른 카메라로 그릴 때 사용합니다.

## Culling Mask

카메라가 어떤 Layer를 그릴지 선택하는 설정입니다.

## Base Camera

URP Camera Stack에서 가장 먼저 전체 화면을 그리는 기본 카메라입니다.

## Overlay Camera

Base Camera의 결과를 지우지 않고 특정 오브젝트를 위에 추가로 그리는 카메라입니다.

## Camera Stack

Base Camera와 Overlay Camera의 출력 순서를 묶은 목록입니다.

## Tilemap

격자 셀에 Tile을 칠해서 2D 맵을 만드는 Unity 오브젝트입니다.

## Tile Palette

맵에 칠할 Tile을 고르는 팔레트 창입니다. `Window > 2D > Tile Palette`에서 엽니다.

## TilemapCollider2D

Tilemap에 칠한 타일 모양을 따라 충돌 영역을 만드는 컴포넌트입니다.

## Hierarchy

현재 씬에 존재하는 GameObject 목록입니다.

## Project 창

프로젝트에 저장된 씬, 이미지, C#, Material과 Shader 파일 목록입니다.

## Runtime

Play를 누른 뒤 게임이 실행되고 있는 상태입니다. Runtime에 자동 생성된 오브젝트는 Play를 중지하면 사라집니다.
