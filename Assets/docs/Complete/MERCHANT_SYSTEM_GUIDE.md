# 상인 시스템 가이드

## 목표

상인 NPC와 대화한 뒤 구매·판매 화면으로 자연스럽게 이어지고, 골드·인벤토리·한정 재고가 하나의 거래처럼 확정되거나 모두 롤백되는 수직 슬라이스다. 상점 화면이 닫힐 때까지 NPC 상호작용과 게임 시뮬레이션 정지를 유지한다.

## 런타임 흐름

```text
NpcActorSO.merchantCatalog
→ NpcActor 대화/상호작용
→ IMerchantService(MerchantManager)
→ UI_Scene_Merchant
→ IInventoryService
→ 골드·아이템·한정 재고 확정
```

- `MerchantCatalogSO`는 상인 ID, 표시 이름, 구매가·매입가, 무제한·한정 재고를 소유한다.
- `MerchantManager`는 활성 상점 세션과 거래 트랜잭션, 저장되는 한정 재고를 소유한다.
- `InventoryManager`는 골드와 실제 아이템 인스턴스의 추가·제거를 담당한다.
- `UI_Scene_Merchant`는 목록, 수량, 포커스, 거래 결과 피드백만 담당한다.
- UI와 NPC는 구체 매니저가 아니라 `IMerchantService` 계약을 사용한다.

구매는 골드를 먼저 차감하고 아이템 지급이 실패하면 골드를 돌려준다. 판매는 선택 슬롯의 아이템 인스턴스를 보관한 채 제거하고, 골드 지급이 실패하면 같은 인스턴스를 복구한다. 장착 중인 장비는 판매할 수 없다.

## 데이터 저작

1. `Assets/10.Datas/Merchant/`에서 `UPlayGround/상인/카탈로그`로 카탈로그를 만든다.
2. `Merchant ID`에는 저장 이후에도 바꾸지 않을 고유 ID를 입력한다. 표시 이름이나 에셋 이름을 저장 키로 사용하지 않는다.
3. 품목마다 구매가와 매입가를 입력한다. `0`인 방향은 거래 목록에 노출되지 않는다.
4. 한정 재고라면 시작 수량을 지정한다. 남은 재고는 저장 데이터에 기록되고 새 게임에서만 초기화된다.
5. 대상 `NpcActorSO.merchantCatalog`에 카탈로그를 연결한다. 대화 데이터가 있으면 대화 종료 후 상점이 열리고, 없으면 즉시 열린다.

동일 아이템을 한 카탈로그에 중복 등록하거나 양쪽 가격을 모두 0으로 두면 카탈로그 검증과 상점 열기가 실패한다. 인스펙터의 검증 메시지를 먼저 해결한다.

기본 샘플은 `Merchant_Penny.asset`이며 회복 물약 구매와 필드 재료 판매로 초반 탐색 보상을 골드 순환에 연결한다. 희귀 물약은 한정 재고라서 상위 회복 수단을 무제한 비축하는 문제를 막는다.

## UI 저작과 재생성

- 런타임 프리팹: `Assets/03.Prefabs/UI/Scene/Merchant/UI_Scene_Merchant.prefab`
- 목록 슬롯: `Assets/03.Prefabs/UI/Scene/Merchant/UIMerchantItemSlot.prefab`
- UI 키: `Merchant`, 레이어: `Scene`
- 통합 툴 런처: `생성 도구/상점 UI 프리팹 빌드`

화면은 구매·판매 탭, 세로 순환 포커스, 초기 포커스, `Cancel` 닫기, 탭 숄더 전환을 지원한다. 선택·골드·거래 결과 트윈은 시간 정지 중에도 재생되도록 모두 `SetUpdate(true)`를 사용한다.

## 저장 계약

`GameSaveData.merchant.limitedStocks`에는 `merchantId + itemId + remainingStock`만 저장한다. 무제한 재고와 가격은 카탈로그가 단일 소스다. 카탈로그의 상인 ID나 아이템 ID를 변경하면 기존 저장의 재고와 연결이 끊기므로 라이브 데이터에서는 ID를 보존한다.

## 검증 체크리스트

- 카탈로그의 상인 ID와 품목 ID가 고유한가
- 구매가가 매입가보다 커서 무한 골드 루프가 생기지 않는가
- 구매 실패 시 골드와 재고가 그대로인가
- 판매 실패 시 원래 아이템 인스턴스가 복구되는가
- 장착 장비가 판매 목록과 거래에서 차단되는가
- 한정 재고가 저장·로드와 새 게임 초기화에서 올바른가
- 키보드·마우스 없이 탭, 품목, 수량, 거래, 닫기를 모두 조작할 수 있는가
- 상점 종료 뒤 NPC 상호작용과 시뮬레이션 리스가 해제되는가

자동 검증은 `MerchantTradeCalculatorTests`, `MerchantCatalogTests`, `MerchantUIPrefabTests`가 담당한다. 실제 NPC 대화 연결, 상점 종료, 저장·로드는 Unity Play Mode에서 최종 스모크 검증한다.
