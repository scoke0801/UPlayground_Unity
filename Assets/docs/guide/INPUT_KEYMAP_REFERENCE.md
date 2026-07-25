# PlayerInputActions 키매핑 레퍼런스

- **에셋:** `Assets/Resources/Input/PlayerInputActions.inputactions`
- **상수 정의:** `Assets/02.Scripts/Data/Input/InputDefine.cs`
- **런타임:** `Assets/02.Scripts/Manager/Input/InputManager*.cs` (`InputManager.Chord.cs`가 modifier 조합 중재)
- **기준일:** 2026-07-25

## 컨트롤 스킴

| 스킴 | 디바이스 |
| --- | --- |
| Keyboard&Mouse | `<Keyboard>`, `<Mouse>` |
| Gamepad | `<Gamepad>` |

> 대부분의 바인딩은 스킴 그룹이 비어 있어 두 디바이스 모두에서 활성이다. 그룹이 `Gamepad`로 명시된 것은 chord(조합) 바인딩뿐이다.

## 액션 맵

| 맵 | 상수 | 액션 수 | 용도 |
| --- | --- | --- | --- |
| `PlayerAction` | `InputMapNames.PlayerAction` | 29 | 인게임 이동·전투 |
| `System` | `InputMapNames.System` | 4 | 시스템 공통 (ESC/커서) |
| `UI` | `InputMapNames.UI` | 13 | UI 패널·네비게이션 |
| `Gamepad` | `InputMapNames.Gamepad` | 11 | 물리 버튼 원시 참조 |

## InputLayer 우선순위

| 값 | 이름 | 대응 |
| --- | --- | --- |
| -1 | `None` | — |
| 0 | `Level_0` | HUD |
| 1000 | `Level_1` | Scene |
| 2000 | `Level_2` | Popup |
| 3000 | `Level_3` | System |
| 10000 | `Level_Top` | 어디서든 입력 가능 |

---

## PlayerAction 맵

| 액션 | 타입 | 키보드 / 마우스 | 게임패드 | 비고 |
| --- | --- | --- | --- | --- |
| `Move` | Value (Vector2) | W / A / S / D | 좌스틱 | 2DVector 컴포짓 |
| `Look` | Value (Vector2) | 마우스 이동 (`delta`) | 우스틱 | |
| `Zoom` | Value (Vector2) | 마우스 휠 (`scroll`) | — | 게임패드 미바인딩 |
| `Jump` | Button | Space | A (`buttonSouth`) | |
| `Sprint` | Button | Z | L3 (`leftStickPress`) | |
| `Walk` | Button | — | — | **바인딩 없음** |
| `Dodge` | Button | Ctrl | LB + RB | OneModifier 컴포짓 |
| `Attack` | Button | 마우스 좌클릭 | X (`buttonWest`) | |
| `HeavyAttack` | Button | 마우스 우클릭 | Y (`buttonNorth`) | |
| `Crouching` | Button | — | — | **바인딩 없음** |
| `Interact` | Button | F | RT (`rightTrigger`) | |
| `SkillAbility` | Button | E | LT (`leftTrigger`) | |
| `SkillUltimate` | Button | R | LB + RT | `UltimateChord`, 그룹 `Gamepad` |
| `Equip` | Button | G | — | 게임패드 미바인딩 |
| `LockOn` | Button | 마우스 휠 클릭 | R3 (`rightStickPress`) | |
| `Guard` | Button | V | LB (`leftShoulder`) | |
| `Dash` | Button | Shift | B (`buttonEast`) | 빈 path 바인딩 1개 잔존 |
| `CharacterSwap_1` | Button | 1 | D-Pad ↑ | |
| `CharacterSwap_2` | Button | 2 | D-Pad → | |
| `CharacterSwap_3` | Button | 3 | D-Pad ↓ | |
| `CharacterSwap_4` | Button | 4 | D-Pad ← | |
| `LockOnSwitchRight` | Button | Tab | 우스틱 → | |
| `LockOnSwitchLeft` | Button | — | 우스틱 ← | 키보드 미바인딩 |
| `BossAssist` | Button | Q | Select (View/Share), 그룹 `Gamepad` | |
| `QuickSlot_Up` | Button | F1 | LB + D-Pad ↑ | `QuickSlotUpChord` |
| `QuickSlot_Right` | Button | F2 | LB + D-Pad → | `QuickSlotRightChord` |
| `QuickSlot_Down` | Button | F3 | LB + D-Pad ↓ | `QuickSlotDownChord` |
| `QuickSlot_Left` | Button | F4 | LB + D-Pad ← | `QuickSlotLeftChord` |
| `ElementBuff` | Button | T | RB (`rightShoulder`) | |

## System 맵

| 액션 | 타입 | 키보드 / 마우스 | 게임패드 | 비고 |
| --- | --- | --- | --- | --- |
| `Back` | Button | Esc | — | |
| `ShowCursor` | Button | Alt | — | |
| `Submit` | Button | — | — | **바인딩 없음** |
| `Cancel` | Button | — | — | **바인딩 없음** |

## UI 맵

| 액션 | 타입 | 키보드 / 마우스 | 게임패드 | 비고 |
| --- | --- | --- | --- | --- |
| `Navigate` | PassThrough (Vector2) | W / A / S / D | — | 2DVector 컴포짓 |
| `Submit` | Button | Enter | — | |
| `Cancel` | Button | Esc | — | |
| `Point` | PassThrough (Vector2) | 마우스 위치 | — | |
| `CursorClick` | Button | — | A (`buttonSouth`) | 가상 커서 클릭 |
| `CursorMove` | Value (Vector2) | — | 좌스틱 | 가상 커서 이동 |
| `Inventory` | Button | I | — | |
| `EquipInventory` | Button | O | — | |
| `DialogueNext` | Button | Space | — | |
| `Map` | Button | M | — | |
| `Party` | Button | P | — | |
| `MenuPanel` | Button | ` (backquote) | Start | |
| `CheatPanel` | Button | F11 | — | 치트 패널 |
| `DialogueSkip` | Button | Ctrl | RT (`rightTrigger`) | 대화 스킵(선택지·End까지 빨리감기) |
| `DialogueToggleAuto` | Button | F2 | Y (`buttonNorth`) | 대화 자동 재생 토글 |
| `DialogueBacklog` | Button | Tab | LB (`leftShoulder`) | 이전 대화내역 열기/닫기 |

## Gamepad 맵 (원시 버튼)

| 액션 | 경로 | 통칭 |
| --- | --- | --- |
| `L1` | `<Gamepad>/leftShoulder` | LB |
| `L2` | `<Gamepad>/leftTrigger` | LT |
| `R1` | `<Gamepad>/rightShoulder` | RB |
| `R2` | `<Gamepad>/rightTrigger` | RT |
| `Up` | `<Gamepad>/dpad/up` | D-Pad ↑ |
| `Down` | `<Gamepad>/dpad/down` | D-Pad ↓ |
| `Left` | `<Gamepad>/dpad/left` | D-Pad ← |
| `Right` | `<Gamepad>/dpad/right` | D-Pad → |
| `Select` | `<Gamepad>/select` | View / Share |
| `Start` | `<Gamepad>/start` | Menu / Options |
| `Touchpad` | `<DualShockGamepad>/touchpadButton` | DualShock 전용 |

---

## 조합(Chord) 구조

게임패드의 **LB(`leftShoulder`)** 가 modifier 겸 단독 액션으로 다중 사용된다. `InputManager.Chord.cs`의 `RebuildChordCatalog` / `SubmitToChordArbiter`가 modifier 조합과 단독 입력의 우선순위를 중재한다.

| 조합 | 액션 |
| --- | --- |
| LB 단독 | `Guard` |
| LB + RB | `Dodge` |
| LB + RT | `SkillUltimate` |
| LB + D-Pad ↑ / → / ↓ / ← | `QuickSlot_Up` / `Right` / `Down` / `Left` |

파생 충돌 지점:

- **RB**: 단독은 `ElementBuff`, LB와 함께면 `Dodge`.
- **RT**: 단독은 `Interact`, LB와 함께면 `SkillUltimate`.
- **D-Pad**: 단독은 `CharacterSwap_1~4`, LB와 함께면 퀵슬롯.

## 미해결 / 정리 대상

| 항목 | 내용 |
| --- | --- |
| 바인딩 없는 액션 | `PlayerAction/Walk`, `PlayerAction/Crouching`, `System/Submit`, `System/Cancel` |
| 한쪽 디바이스만 존재 | `Zoom`·`Equip` (게임패드 없음), `LockOnSwitchLeft` (키보드 없음) |
| 잔여 데이터 | `Dash`에 path가 빈 바인딩 1개 |
| 상수만 존재, 액션 없음 | `GamepadAction.L3/R3/North/South/East/West`, `UIAction.Click/RightClick/MiddleClick/ScrollWheel` |
| 액션만 존재, 상수 없음 | `UI/CursorClick`, `UI/CursorMove` |
