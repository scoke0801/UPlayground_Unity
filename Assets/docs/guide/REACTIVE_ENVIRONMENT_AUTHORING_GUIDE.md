# 반응형 환경 저작

[설계 계약](../design/REACTIVE_ENVIRONMENT_INTEGRATION.md) · [현재 상태](../../../docs/work-status/reactive-environment.md) · [사용자 테스트](../../../docs/work-status/reactive-environment-tests.md)

## 프로필과 명중

ReactiveEnvironmentDefinitionSO에 안정적인 definitionId, 반응 계수, 발화/파괴 허용, 종류별 구조 배율, 충격/화재 보상, 전파 범위/rate/차폐 레이어/surfaceLayerId를 저작한다. 이름이나 렌더링 재질에서 재질 성질을 추측하지 않는다.

GAS는 Ability Editor의 Motion Payload → baseInfo → hitPhases → environment에 kind, heatImpulse, waterImpulse를 저작한다. impulse에 deltaTime을 곱하지 않는다. 기존 공격의 원소 필드가 0이면 환경 원소 입력도 0이다. 실제 전투 속성이나 이펙트 색을 자동 열량으로 바꾸지 않는다. 부착형/명시적 근접, 잔상, 몬스터와 투사체가 이 필드를 전달한다. 지속 공급은 EnvironmentElementEmitter의 radius/heatPerSecond/waterPerSecond/lifetime으로 만든다. lifetime 0은 무기한이다.

같은 외부 공격의 다중 Collider는 TryApplyImpact의 실행 ID와 sequence를 공유한다. 다음 연타는 sequence를 증가시킨다. 소화·건조는 그을림/구조를 복구하지 않는다.

## 배치와 파괴

1. root에 SceneEntityId와 ReactiveEnvironmentObject를 둔다. GatheringActor를 함께 붙이지 않는다. 프리팹 원본의 GUID는 비워 두며 씬 배치마다 기존 SceneEntityId 경로로 서로 다른 ID를 부여한다. RegionOwnership/SceneContext가 있는 소유 맵에 둔다.
2. 원형과 잔해는 root의 독립 자식으로 지정한다. 원형 충돌은 원형 자식에 둔다. root를 원형/잔해/불꽃으로 지정하지 않는다. 불꽃은 두 형상과 별개인 root 직계 자식이다.
3. rainSamples는 표면 밖에 두고 원형·잔해별로 각각 배치한다. heatSamples는 화염 전달을 대표하는 중심에 둔다. 비활성 형상의 샘플은 사용하지 않는다. 날씨는 명시 연결 또는 기존 공용 날씨 자동 연결을 사용한다.
4. 지붕 Collider는 기존 EnvironmentEffects의 roofLayers에 포함되는 레이어를 쓴다. 보호 볼륨을 쓰면 그 원본 Collider도 파괴 컴포넌트의 shelterColliders에 지정한다. 새 구조물은 카메라 가림 가이드와 이름 규칙을 함께 확인한다.
5. EnvironmentDestructionRecipeSO를 연결할 때 root에 ReactiveEnvironmentDestruction을 둔다. Swap은 저작된 사전 파손 형상, DirectionalFall은 CutPivot 기준의 실제 줄기 길이/반경/각도/시간/검사 레이어와 막힘 fallback을 요구한다. 낙하 검사 레이어에는 지형·플레이어·NPC·중요 구조물을 포함한다.
6. standingObstacles/remnantObstacles에 소유 NavMeshObstacle을 연결한다. 파괴 시 지붕/길찾기 변화를 레시피에 명시한다. 잔해에 TTL을 붙여 게임플레이 통로를 뒤늦게 바꾸지 않는다. 낙하 중 저장은 포즈 진행을 복원하며 새 게임은 초기 포즈로 돌아간다.

## 툰 재질과 LOD

surfaceBackend를 ReactiveToon으로 지정하고 UPlayGround/Environment/Reactive Toon을 사용하는 모든 Renderer를 surfaces에 등록한다. 비활성 잔해와 LODGroup의 모든 LOD/slot도 포함한다. 원본 재질은 공유하고 런타임 lease는 객체가 소유한다. Renderer.material을 매 프레임 호출하거나 원본 sharedMaterial 자산에 상태를 쓰지 않는다.

- Body: 기본 불투명, 일반 Collider. 균열/그을림은 색 표현이며 형상 파손은 레시피가 담당한다.
- Foliage: Cull Off, 필요한 AlphaClip/Cutoff와 WindAmplitude를 저작한다. AllowFuelCoverage를 켜면 연료가 소진될 때 잎만 소거한다. 해당 Renderer에 게임플레이 Collider를 함께 두지 않는다.
- Interior: 절단면 전용 기본 색·그을림·노멀을 저작하고 원래 외피 UV를 무리하게 재사용하지 않는다.
- ReactionMask를 켜면 R 반응 허용/G 균열/B 그을림 패턴을 사용한다. 끄면 물체 로컬 패턴을 사용한다. WetTint/CharTint/CrackColor/FireColor/명암 경계/최저 간접광/외곽선은 재질에서 조절한다.

일시정지 중에는 논리와 shader time이 멈추는지, 소화 직후 발광만 사라지는지, 여러 LOD·그림자·깊이·노멀·외곽선에서 같은 coverage/바람인지 실제 화면으로 확인한다. shader 8패스 컴파일과 전방 GPU 표면 자동 검사는 최종 월드 아트 인수가 아니다.

## 연결된 샘플

Assets/03.Prefabs/Environment의 ReactiveEnvironmentCrateSample, TreeSample, RockSample, RoofSample, GrassSample은 기능 검증용이다. Unity primitive/직접 생성 leaf mesh와 커스텀 툰을 썼으며 최종 구매/MCP 아트가 아니다. 상자/나무의 직접 파괴는 기존 장작, 바위는 기존 돌을 사용한다. 화재 샘플은 자원을 소진한다. 숯 아이템/소비처를 새로 만들지 않았다.

사람의 반복 확인은 툴 런처 → 월드/맵 → 환경 반응 모니터를 사용한다. 샘플 생성기/캡처 도구는 카탈로그 전용이며 정규 씬이나 구매 에셋을 일괄 변환하지 않는다.


## 환경 에셋 테스트 치트

Editor에서 툴 런처 → 유틸 → 치트 콘솔 → 환경 에셋 테스트를 열거나, 기존 월드/맵 → 환경 반응 모니터를 연다. Unity 에디터의 마우스/키보드 도구이며 게임 내 F12 패널이나 Development Player에는 추가하지 않는다.

1. 기존 Boot 경로로 테스트 슬롯에서 Play한다. 활성 플레이어·WorldStateManager·날씨·SceneContext/RegionOwnership이 준비된 맵에서 사용한다.
2. 샘플 버튼(상자/나무/바위/지붕/풀) 또는 테스트 프리팹에 직접 지정한 ReactiveEnvironmentObject 루트 프리팹을 소환한다. 전방 거리에서 Ground 지면을 찾고 플레이어와 같은 소유 씬에 배치한다. 원본과 별개의 GUID를 활성화 전에 발급한다.
3. 자동 선택된 대상의 등록=True를 확인한다. 열/물/구조 피해를 조절해 각각 적용한다. 화학 반응은 게임 시간 tick을 기다린다. 남은 내구도만큼 파괴는 구조 피해 경로이며 화재 소진과 보상 분기가 다를 수 있다.
4. 창에 남은 소환 목록이나 Hierarchy에서 대상을 바꾼다. 새 개체로 교체하면 새 ID와 초기 상태로 다시 생성한다. 교체/제거는 이 창에서 소환한 개체만 허용한다. 창을 닫아도 테스트 개체는 유지되며, 모두 제거하거나 Play를 종료한다.
5. 이 치트는 실제 보상과 저장 원장을 사용한다. 제거/초기화가 이미 나온 아이템·저장된 원장까지 되돌리지는 않는다. 런타임 소환 개체는 씬 저작에 저장되지 않으며 세션 재실행에서 자동 재소환하지 않는다. 저장/로드 인수는 정식 씬 배치 대상을 사용한다.

소환 실패 문구·등록=False·Console 오류는 성공으로 간주하지 않는다. 물 렌더러 MSAA 오류가 재현 중이면 렌더링 인수는 그 오류와 별도로 실패/대기로 기록한다.
