# MotionEvent 파일 생성

사용자가 제공한 인자(`$ARGUMENTS`)를 기반으로 새 MotionEvent 파일을 생성한다.

## 인자 파싱 규칙

`$ARGUMENTS` 형식: `<EventName> [<Category>]`

- `EventName` — 필수. `Event` 접미사 없이 입력 가능 (예: `Shake`, `ShakeEvent` 둘 다 허용).
  - 클래스명: `<EventName>Event` (예: `ShakeEvent`)
  - 파일명: `MotionEvent_<EventName>.cs` (예: `MotionEvent_Shake.cs`)
- `Category` — 선택. 이벤트 성격 분류. 없으면 EventName에서 추론:
  - 이름에 `Camera`, `Cam`, `Zoom`, `Shake` 포함 → `Camera`
  - 이름에 `Sound`, `Audio`, `Footstep`, `BGM` 포함 → `Sound`
  - 이름에 `Collision`, `Hit`, `Attack` 포함 → `Collision`
  - 이름에 `Force`, `Velocity`, `Move`, `Warp` 포함 → `Movement`
  - 이름에 `Particle`, `VFX`, `Effect`, `Spawn` 포함 → `Particle`
  - 이름에 `Invincible`, `Guard`, `Poise`, `Status` 포함 → `Status`
  - 이름에 `LookAt`, `Socket`, `Target` 포함 → `LookAt`
  - 그 외 → `Misc`

## 카테고리별 색상/아이콘 매핑 (MotionEventStyle 등록용)

`COL_*` 변수들은 `MotionEventStyle` 클래스 내부의 `private static readonly` 필드이므로,
`GetByType` 메서드 안에서만 참조 가능하다. 스킬은 해당 메서드 내부에 추가하므로 정상 동작한다.

| Category   | Color 변수    | Icon |
|------------|--------------|------|
| Collision  | COL_COLLISION | ⚔   |
| Particle   | COL_PARTICLE  | ✦   |
| Camera     | COL_CAMERA    | 📷  |
| Status     | COL_INVINCIBLE| 🛡  |
| Sound      | COL_SOUND     | ♪   |
| Movement   | COL_MOVEMENT  | ↗   |
| LookAt     | COL_LOOKAT    | 🎯  |
| Misc       | COL_MISC      | ▸   |

## 실행 단계

### 1단계: 이벤트 스크립트 파일 생성

`Assets/02.Scripts/Data/Event/Animation/MotionEvent_<EventName>.cs` 를 **Write 툴**로 생성한다.

템플릿:

```csharp
using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// TODO: 이벤트 설명 작성
    /// startTime에 Execute, endTime에 OnCompleteEvent 자동 호출됨.
    /// </summary>
    [Serializable]
    public class <EventName>Event : MotionEventBase
    {
        // TODO: 필요한 직렬화 필드 추가
        // public float value;

        public override string GetDisplayName() => "<EventName>";

        public override string GetShortLabel() => "<EventName>";

        public override void Execute(GameObject target)
        {
            // TODO: startTime에 실행할 로직
        }

        public override void OnCompleteEvent(GameObject target)
        {
            // TODO: endTime에 실행할 로직
        }
    }
}
```

### 2단계: MotionEventStyle.cs 수정

`Assets/02.Scripts/Data/Actor/Animation/Editor/MotionEventStyle.cs` 를 **Read 툴로 읽은 뒤**
`GetByType` 메서드의 기존 if 블록 마지막 줄 (`return Make(COL_MISC, "▸");` 바로 위) 에 다음 한 줄을 **Edit 툴**로 추가한다:

```csharp
            if (type == typeof(<EventName>Event))       return Make(<COL_변수>, "<아이콘>");
```

카테고리에 맞는 COL 변수와 아이콘은 위 매핑 표에서 선택한다.

### 3단계: 완료 메시지 출력

다음 형식으로 출력한다:

---
**생성된 파일:**
- `Assets/02.Scripts/Data/Event/Animation/MotionEvent_<EventName>.cs`

**수정된 파일:**
- `Assets/02.Scripts/Data/Actor/Animation/Editor/MotionEventStyle.cs` — `<EventName>Event` 등록

**다음 작업:**
- `Execute` / `OnCompleteEvent` 내부 로직을 구현한다.
- 타겟이 Player/Monster 모두 필요하면 `target.GetComponent<GameActor>()`로 분기한다.
- **활성화/비활성화 쌍이 필요한 경우** (예: 충돌 ON/OFF, 무적 ON/OFF):
  `Execute`에 ON 로직, `OnCompleteEvent`에 OFF 로직을 작성한다.
  필요하다면 `BeginXxxEvent` / `DisableXxxEvent` 처럼 두 클래스를 같은 파일에 분리할 수도 있다 (`MotionEvent_Collision.cs` 참고).
- MotionSet SO의 이벤트 리스트에 추가하면 타임라인에서 바로 사용 가능하다.
---

생성한 스크립트 파일 전체 내용을 보여준다.
