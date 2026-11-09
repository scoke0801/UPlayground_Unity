# 환경 동물 자연스러움 개선 설계

상태: 설계 (미구현, 미검증). 대상: `WildlifeCreature`(+Bird/Combat partial), `WildlifeProfileSO`, `Assets/10.Datas/World/Wildlife/*.asset`.

## 1. 목표와 완료 기준

"동물이 굼뜨고 기계적이다"를 "종마다 한눈에 구별되는 움직임과 반응을 가진 살아있는 동물"로 바꾼다.

완료 기준(플레이어 경험 기준):
1. 5초만 보고도 사슴·토끼·멧돼지·닭의 종을 행동만으로 구별할 수 있다.
2. 접근하면 **즉시 멈칫 → 반응 → 도주**가 이어지고, 도주는 "빠르다"고 느껴진다.
3. 가만히 있을 때도 고개 들기·귀/꼬리 움직임·자세 변화가 일어나 정지 화면처럼 보이지 않는다.
4. 발 미끄러짐 없이 속도와 보행 클립이 맞는다.

## 2. 현재 구현 진단 (코드·데이터 근거)

### 2.1 느림의 직접 원인
| 원인 | 근거 | 영향 |
|---|---|---|
| 도주 속도가 지나치게 낮음 | 쥐 `FleeSpeed 0.65`, 돼지 `0.75`(걷기 0.45), 닭 `1.5`, 토끼 `2.8`, 사슴 `4.2`, 늑대 `3.5` | 쥐·돼지는 도주가 걷기와 거의 같다. 토끼는 실제로 사슴급 순간 가속 동물인데 사슴과 비슷하다 |
| 가속이 느림 | 사슴 `Acceleration 3.5`로 도주 최고속 4.2에 약 1.2초, 곰 1.6, 소 1.2, 돼지 1.0 | "놀라서 튀어나가는" 느낌이 없고 슬금슬금 빨라진다 |
| 도주 시작 시 회전 제동 | `UpdateGroundSteering`: `agent.speed = moveSpeed * max(TurnMoveSpeedRatio 0.25, alignment)` | 도주 방향이 반대로 잡히면 첫 구간이 25% 속도 + 낮은 `TurnSpeed`(곰 65, 멧돼지 85, 돼지 85)로 돈다 |
| 반응이 모두 느린 판단 주기에 묶임 | `DecisionInterval 0.5s` 단위로 `Decide` 실행 | 최대 0.5초 지연 + 개체 간 동시 반응, 반응 지연이 종과 무관하게 균일 |
| 경계 단계가 길고 평평함 | 위협 근접 → `StopToWatch` → `ObserveSeconds.x` 이상 관찰 후 `Avoiding`(걷는 속도로 회피) | 중간 단계가 모두 걷기 속도라 "느릿하게 피하는" 인상. 토끼·사슴류가 경계에서 도주로 가는 빠른 경로가 `FleeDistance` 한 줄뿐 |
| 정지 연출이 비어 있음 | 16종 중 `Observe` 클립은 염소·독사·늑대(APD) 3종, `Rest`는 독사·늑대 2종만 지정. 나머지는 `Idle` 하나로 대체 | 경계 중에도 평상시와 같은 모션 |
| 정지 시간이 김 | 사슴 `IdleSeconds 5~11s`, 늑대 5~12s, 독사 10~22s, 달팽이 12~25s | 한 번 멈추면 오래 가만히 있음 |
| 클립 단순 | 종당 Idle/Move/Flee/Feed 4개(토끼 `Hop` 등 보조 클립 비어 있음) | 걷기·질주 2단 기어만 존재 |
| 동일 로직·다른 숫자 | 모든 종이 하나의 `Roam → Watch → Avoid/Flee → Return` 상태 흐름 | 종 차이가 숫자 차이로만 존재해 행동 "문법" 자체가 같다 |
| 레거시 프로필 | `Deer/Pig/Wolf/Songbird.asset`은 `UseStagedResponse 0`, `Pig` 클립속도(0.75/3.5)가 사슴 값 복붙 | 단계 반응·재생속도 보정이 꺼져 있음 (APD 프로필과 병행 존재 — 사용 여부 확인 필요) |

### 2.2 확인하지 못한 것 (착수 전 측정 필요)
- 플레이어 달리기 속도와 월드 스케일. 동물 속도는 "플레이어 대비 체감"으로 정해야 하므로 먼저 실측한다.
- 동물 모델 크기(`entry.scale`)와 몸길이. 속도 기준을 **몸길이/초(BL/s)**로 잡기 위해 필요하다.
- 레거시 프로필(`Deer`, `Pig`, `Wolf`, `Songbird`)이 씬에서 실제 사용되는지.

## 3. 웹 조사 요약 (종별)

신뢰도: ◎ 학술/기관, ○ 사냥·사육 가이드 등 2차, △ 단일/저품질(참고만).

| 종 | 핵심 행동 특성 | 게임 행동으로의 번역 | 신뢰도 |
|---|---|---|---|
| 사슴 | 꼬리 반쯤 올림 → 완전히 올림(flag) 순으로 경계 단계가 올라간다. 귀를 레이더처럼 돌리고 머리를 높게 든다. 발 구르기·콧김(snort)으로 무리에 경고. **섭식 중 머리를 들 때마다 꼬리를 튕김.** 도주 시 흰 꼬리로 신호 | 섭식 중 주기적 머리 들기, 경계 3단(머리 듦 → 꼬리 반들림/발 구르기 → 도주), 도주는 한 마리가 시작하면 전체 연쇄 | ○ |
| 토끼 | 위험 시 **얼어붙기(freeze) / 은신처로 도주 / 뒷발 구르기** 중 선택. 지그재그 도약, 도주 중 흰 꼬리 신호. 섭식은 **굴 근처에서 가장 집중**, 굴에서 멀수록 경계가 늘어난다 | 홈(굴) 반경 안에서만 섭식, 거리에 비례한 머리 들기 빈도, 접근 시 먼저 얼어붙고 임계 거리에서 폭발적 도주 + 지그재그, 도착점은 굴(홈) | ◎(Wageningen 논문)/○ |
| 멧돼지 | 모계 무리(sounder), 땅을 코로 파헤치는 rooting이 핵심 섭식 행동. 무리 내 그르렁 소리로 서로 위치 유지. 사람 접근 시 도주 개시거리는 평균 약 93m로 **경계심이 높고** 비사냥구역·도시에서는 더 짧다. 한 마리가 놀라면 무리 전체가 조용해지거나 즉시 도주 | 땅 파기 섭식(고개 숙인 이동), 무리 동기 도주, 부상/궁지에서만 돌진 | ◎(GPS 연구) |
| 닭 | 활동 시간의 약 61%를 쪼기·긁기 섭식에 씀. 쪼기와 함께 **고개 까딱임**. 공중/지상 포식자별로 경보 울음이 다르고 반응도 다름(공중: 정지·숨기, 지상: 흩어져 경계). 갑작스런 질주는 정상 행동 | 쪼기-걷기-고개 두리번 짧은 사이클, 무리 연쇄 경보, 놀라면 짧고 빠른 흩어짐 후 다시 모임 | ○ |
| 늑대 | 이동은 **트롯 중심**(약 8km/h), 질주는 단거리. 코로 냄새를 추적. 휴식은 이동 중간에 끼워 넣는다. 무리는 가장 느린 개체에 속도가 제한 | 목적지 있는 일정한 트롯 순찰, 중간 냄새 맡기, 무리 속도 맞춤 | ○ |
| 곰 | 최고속은 약 56km/h로 매우 빠르지만 지속 시간은 짧다. 사람에 대한 반응은 **달리면 추격을 유발**한다는 자료가 많다. 회피·섭식 상세 자료는 못 찾음 | 기본은 느리고 무거운 보행, 경계 시 일어서기/냄새 맡기(자료 부족 → 추가 조사), 도주보다 거리 두기 후 물러남 | △ |
| 아이벡스(산양류) | 절벽·암벽을 은신처로 사용, 암벽/험지에서 경계 빈도가 더 높음(특히 아침), 새끼는 성체의 경계 타이밍을 따라 함. 방해 시 **고도를 올려** 멀리(중앙값 650m) 이동 | 위협 시 위쪽·험지 방향 도주 선호, 암벽 쪽 정착, 경계 동기화 | ◎ |
| 거위 | 평온할 때 낮은 대화음 → **소리가 갑자기 멎으면 경계 신호.** 위협 시 고개를 낮추고 쉭쉭(hiss), 날개 펴기, 공격 | 평온/경계/위협 3단 표현, 침입자에게 도주 대신 위협 자세(영역 방어) | ○ |
| 쥐 | 벽/엄폐물을 따라 이동(thigmotaxis), 개활지를 피한다. 방어 행동은 **얼어붙기·도주·굴 파기·벽 타기** | 엄폐물 사이 짧은 질주(dash) + 정지, 개활지에서 불안 | ◎(실험 연구)/자료 부족 |
| 독사 | 오래 가만히 있는 매복 포식자, 위장 의존. 방해 시 크게 쉭쉭거리며 똬리를 틀고 빠르게 옆으로도 일격 후 복귀. 햇볕 쬐기(basking) 빈도 높음 | 도주보다 **정지-경고 자세**, 일정 거리 안에서만 도약 공격, 햇볕 쬐는 긴 정지 | △(핵심은 ○) |
| 소·돼지(사육) | **도주 구역(flight zone)** 개념: 구역에 들어가면 움직이고 나가면 멈춘다. 흥분하면 구역이 커지고 진정에 20~30분. 돼지는 구역이 작고 무리를 따라간다 | 소/돼지는 "밀려나기"형 반응(천천히 비켜서기, 구역 크기=개체 흥분도), 무리 추종 | ◎(Grandin) |
| 보행 애니메이션 | 속도가 오르면 보폭은 커지고 발 접지 시간은 줄어든다. 걷기→트롯→캔터→질주로 **불연속 전환**한다. 발 미끄러짐은 속도를 보폭에 맞추거나 보폭을 속도에 맞춰 해결 | 2단 기어를 종에 따라 3단(걷기/트롯/질주)으로, 클립 재생속도와 이동속도 일치 | ◎(논문)/○ |

조사 한계: 개구리·도롱뇽·달팽이·나비·작은 새는 이번에 조사하지 않았다. 곰·쥐·독사는 자료가 부족하다. 구체적 도주 속도 수치는 신뢰할 수 있는 1차 자료를 찾지 못했으므로 **이 문서는 실측 속도를 설계 입력으로 쓰지 않고, 종 사이 상대 비율과 행동 형태를 입력으로 쓴다.**

## 4. 설계

### 4.1 원칙
1. **속도는 BL/s(몸길이/초) 기준 표**로 정한다. 모델 스케일이 바뀌어도 감각이 유지된다. 실제 값은 SO에 저장하고 BL/s는 저작 도구가 환산해 보여준다.
2. **행동 문법의 차이는 코드 분기가 아니라 데이터**로 둔다. 종 이름으로 분기하지 않는다. "도주 스타일", "경계 단계", "섭식 중 경계 주기" 같은 **행동 모듈 열거형 + 파라미터**로 표현한다.
3. **놀람은 가속의 문제**다. 도주 시작은 최고속의 일정 비율로 즉시 점프(스타틀 버스트)하고, 이후 가속은 별도 값을 쓴다.
4. 클립이 없으면 연출을 억지로 만들지 않고 **절차적 보조**(머리 방향 돌리기, 꼬리/귀 본 흔들기, 재생속도 변조)와 기존 클립 재사용으로 채운다. 새 클립 제작은 별도 요청 사항이다.

### 4.2 변경 영역

#### A. 이동 응답성 (공통, 가장 큰 체감 효과)
1. **스타틀 버스트**: `Fleeing` 진입 시 `agent.speed`를 `FleeSpeed × StartleSpeedRatio`로 즉시 설정하고 `agent.velocity`에 도주 방향 초속을 준다. 이후 `Acceleration`은 최고속 유지용으로만 사용.
2. **도주 중 회전 제동 제거**: `Fleeing`에서는 `TurnMoveSpeedRatio` 대신 `FleeTurnMoveSpeedRatio`(기본 0.7)를 사용. 첫 방향은 에이전트 경로가 아니라 몸을 먼저 돌리는 `ReactionTurn`(종별 0.05~0.3초) 후 출발한다.
3. **반응 판단을 `DecisionInterval`에서 분리**: 위협 거리 판정은 매 틱 싼 거리 검사로 하고, 비싼 경로 계산만 주기에 둔다. 반응 지연은 `ReactionDelay` 범위(종·개체 랜덤)로 둔다 → 무리가 한 프레임에 동시에 움직이는 일이 사라진다.
4. **도주 속도 재설정**: 4.4의 표를 기준으로 `FleeSpeed`, `RunCycleSpeed`, `Acceleration`을 재보정(클립 접지와 함께 재측정).

#### B. 반응 단계 파이프라인 (종마다 다른 조합)
현재의 `Watching → Avoiding → Fleeing`을 **단계 목록 데이터**로 일반화한다.

```
Detect → [Freeze | HeadUp | Stomp/Snort | Threaten] → Escape(스타일) → Regroup → Return
```
- `AlertStyle` 열거형: `HeadUp`(사슴/염소), `Freeze`(토끼/쥐/꿩), `Threaten`(거위/독사/멧돼지 수컷), `Push`(소/돼지 — flight zone형, 도주 대신 비켜섬), `Ignore`(기존 `FreezesWhenApproached`를 통합).
- `EscapeStyle` 열거형: `Straight`(사슴: 직선 장거리 → 정지 후 뒤돌아보기), `ZigZag`(토끼), `ToRefuge`(토끼/쥐: 홈/엄폐물로), `Uphill`(아이벡스/염소), `Scatter`(닭: 사방으로 흩어졌다 재집결), `Retreat`(곰/독사: 달리지 않고 물러나기).
- 개체별 **대담함(boldness)** 0~1을 스폰 시 랜덤 부여해 `AlertDistance`, `FleeDistance`, `ReactionDelay`를 ±15~25% 변조 → 같은 종이라도 개체 차이가 생긴다.
- 놀람의 **감쇠와 습관화**: 플레이어가 반복 접근하되 위협적 행동이 없으면 `AlertDistance`가 줄고, 공격/사냥 이후에는 늘어난다(`_hitThreatSeconds` 이미 존재 → 확장).

#### C. 정지·섭식 중 생동감
1. **마이크로 행동 스케줄러**: Idle/Feed 중 주기적으로 끼어드는 짧은 행동. `MicroBehavior { 머리 들기, 귀/꼬리 흔들기, 몸 털기, 주변 둘러보기, 자세 바꾸기 }`를 간격·가중치와 함께 프로필에 정의.
2. **섭식 중 경계 주기(vigilance bout)**: 섭식 N~M초마다 머리를 들어 `VigilanceSeconds` 동안 둘러본다. 간격은 거리·무리 크기에 따라 변한다(토끼는 홈에서 멀수록 짧게, 무리가 클수록 길게).
3. 정지 시간 단축과 분산: 기본 `IdleSeconds`를 종별 활동 리듬에 맞게 재조정(예: 토끼 1~4초 단위 움직임, 사슴 2~6초). 독사·달팽이처럼 의도적으로 긴 종은 예외.
4. `Observe` 클립이 없는 종은 `Idle`에 **절차적 머리 시선**(`LookAt`성 상체 회전, 위협 방향)을 적용한다.

#### D. 무리 행동
- `SocialAlertDistance`(현재 사슴·토끼만 > 0)를 닭·거위·멧돼지·염소·소에도 확장하고, 경보 **전파 지연**(0.1~0.4s, 거리 비례)을 둬서 파동처럼 퍼지게 한다.
- `Follow`: 무리 리더 1마리가 목적지를 정하고 나머지는 속도·간격을 맞춰 따라간다(`Cohesion`은 목적지 블렌딩만 한다는 한계 개선). 늑대는 가장 느린 개체에 맞추는 규칙.
- 무리 동기 섭식/휴식(소·거위): 한 마리가 눕거나 먹으면 일부가 따라 하는 확률 규칙.

#### E. 보행 품질
- 종별 **걷기/트롯/질주 3단 기어**: 현재 클립은 걷기(`Move`)/질주(`Flee`)뿐이므로, 중간 단계는 우선 기존 두 클립의 **재생속도 범위**(`PlaybackSpeedRange`)로 표현하고, 트롯용 클립이 확보되면 `Trot` 슬롯을 추가한다.
- `WalkCycleSpeed/RunCycleSpeed`를 "수동 눈대중"에서 **클립 루트 이동량 자동 측정값**으로 바꾸는 에디터 검증 도구(발 접지 영상 확인은 최종 승인 단계로).
- 돼지·쥐는 `RunCycleSpeed`가 걷기와 같다 → 달리기 클립 속도 기준 재측정.

### 4.3 데이터 구조 (신규 필드는 `WildlifeProfileSO`에 그룹으로 추가)
`WildlifeProfileSO`는 이미 약 70개 필드다. 이를 더 키우지 않기 위해 새 값은 **직렬화 구조체**로 묶는다.

| 구조체 | 내용 |
|---|---|
| `WildlifeReactionProfile` | `AlertStyle`, `EscapeStyle`, `ReactionDelay`, `StartleSpeedRatio`, `FleeTurnMoveSpeedRatio`, `BoldnessVariance`, 습관화 계수 |
| `WildlifeIdleBehavior` | `MicroBehavior[]`(종류·간격·가중치), 섭식 경계 주기, 절차적 시선 사용 여부 |
| `WildlifeGroupBehavior` | 전파 지연, 리더-추종 사용 여부, 동기 휴식 확률 |

세 구조체는 모두 신규 필드이므로 **기본값은 현재 동작과 같게** 두어 기존 에셋이 깨지지 않게 한다. `UseStagedResponse`를 켠 APD 프로필부터 점진 적용한다. 커스텀 인스펙터가 있다면 같이 갱신한다(데이터 수정 시 인스펙터 동기화 원칙).

### 4.4 종별 목표 프로필 (상대 값, BL/s는 측정 후 확정)
도주 속도는 **걷기 속도 대비 배율**과 **반응 형태**로 정의한다. 숫자는 시작값이며 플레이 테스트로 조정한다.

| 종 | 도주/걷기 배율(현재→목표) | 반응 형태 | 도주 형태 | 섭식·정지 특징 | 무리 |
|---|---|---|---|---|---|
| 사슴 | 5.3 → 7~9 | HeadUp → 꼬리/발구르기 → 도주 | Straight, 일정 거리 후 정지·뒤돌아보기 | 섭식 3~6초마다 머리 들기 + 꼬리 튕김 | 연쇄 경보 |
| 토끼 | 5.6 → 9~12 | Freeze(길게) → 임계거리 폭발 | ZigZag → 홈(굴) | 홈 반경 안 섭식, 멀수록 짧은 경계 주기, 뒷발 구르기 | 약한 전파 |
| 멧돼지 | 4.7 → 6~8 | HeadUp + 콧김 | Straight, 짧게 | 코 파기(고개 숙임) 중심, 무리 간 위치 유지 | sounder 동기 도주 |
| 돼지(사육) | 1.7 → 3~4 | Push(비켜서기) | 빠른 종종걸음 | 느슨한 무리 추종 | 추종 |
| 소 | 4.4 → 2.5~3.5 | Push(flight zone) | 느린 비켜섬, 무리 이동 | 동기 섭식·휴식 | 추종 |
| 닭 | 4.3 → 6~8 | Freeze(짧음) | Scatter → 재집결 | 쪼기-두리번-걷기 3박자 | 즉시 연쇄 경보 |
| 거위 | 4.3 → 침입자에 Threaten 우선 | Threaten → 필요 시 Scatter | 날개 퍼덕이며 도주 | 대화음 → 소리 멎음으로 경계 표현(사운드 협업) | 연쇄 |
| 염소/아이벡스 | 4.2~4.9 → 6~8 | HeadUp, 경계 동기 | Uphill(고지·험지로) | 험지에서 경계 빈도↑ | 동기 경계 |
| 늑대 | 4.7 → 트롯 위주 순찰 + 접근 시 거리 유지 | Freeze → Retreat | 질주는 짧게 | 이동 중 냄새 맡기, 이동 사이 휴식 | 느린 개체에 맞춤 |
| 곰 | 5.0 → 느린 걷기, 위협 시 Retreat | 서서 둘러보기(클립 필요) | Retreat(달리지 않음) | 느리고 무거운 보행 | 단독 |
| 쥐 | 1.9 → 6~10 | Freeze | 엄폐물 사이 질주-정지(Dash) | 엄폐물 따라 이동 | 단독 |
| 개구리/도롱뇽 | — | Freeze | 짧은 도약/은신 | 간헐적 이동 | 단독 |
| 독사 | — | Threaten(경고 자세) | 거리 안에서만 일격, 이후 후퇴 | 긴 정지, 햇볕 쬐기 | 단독 |
| 달팽이 | — | Ignore | 움츠림 | 의도적 극저속 유지 | 단독 |
| 나비/작은 새 | 별도 | 현행 유지, 나비는 추후 | 현행 `UseBirdMotion` | — | — |

게임성 판단: 생물 반응은 **사냥(현재 `MaxHealth`가 있는 종)** 과 연결된다. 토끼·사슴이 너무 빨라지면 사냥 난이도가 오른다. 따라서 도주 속도 상향은 **플레이어 전력 질주 속도 대비 몇 %인지** 명시해 정하고 (예: 토끼는 플레이어 달리기보다 빠르지만 지구력이 짧아 일정 거리 후 멈춤), 사냥 기획(`WILDLIFE_HUNTING_TEST_SPEC.md`)과 맞춘다. 이 부분은 4.6 결정 사항.

### 4.5 단계별 실행 계획
| 단계 | 내용 | 산출 | 검증 |
|---|---|---|---|
| 0 | 측정: 플레이어 속도, 동물 스케일/몸길이, 레거시 프로필 사용처, 클립 루트이동량 | 측정표 | 에디터 스크립트 출력 |
| 1 | **데이터만으로** 도주 속도·가속·IdleSeconds·TurnSpeed 재보정 (APD 프로필). 코드 변경 없음 | 에셋 수정 | Play 영상 비교 전/후 |
| 2 | 스타틀 버스트, 도주 중 회전 제동 완화, 반응 지연 분리 (A항) | `WildlifeCreature` 수정 | EditMode 테스트(상태 전이) + Play 확인 |
| 3 | 마이크로 행동 + 섭식 경계 주기 + 절차적 시선 (C항) | 신규 구조체 + 컴포넌트 | Play 확인 |
| 4 | AlertStyle/EscapeStyle 일반화, 종별 프로필 적용 (B항) | 열거형 + 프로필 | 종별 시나리오 체크리스트 |
| 5 | 무리 행동 (D항) | 전파 지연/리더 추종 | 무리 시나리오 |
| 6 | 보행 3단 기어/트롯 클립 확보 (E항) | 클립 요청 또는 확보 | 발 접지 검수 |

**1단계만으로도 체감 개선이 가장 크고 위험이 가장 낮다.** 먼저 진행하고 결과를 보고 나머지 범위를 확정하는 것을 권장한다.

### 4.6 결정이 필요한 사항
1. 도주 속도를 올릴 때 사냥 난이도 정책: (a) 플레이어가 따라잡을 수 있게 지구력 컷오프 적용, (b) 대부분 못 잡고 함정·원거리 위주.
2. 새 클립(트롯, 곰 서기, 거위 위협, 토끼 구르기 등) 확보 여부. 없으면 절차적 보조와 기존 클립 재사용만 한다.
3. 레거시 프로필(`Deer/Pig/Wolf/Songbird`)을 정리할지.
4. 곰·쥐·독사·양서류는 조사가 얇아 추가 자료 조사 후 확정할지, 현 문서 값으로 시작할지.

### 4.7 리스크
- 도주가 빨라지면 `HasSafePath`/`HasPersonalSpace`의 경로 거절이 더 자주 보일 수 있어 도주 실패(제자리 정지)가 눈에 띈다 → 실패 시 폴백 도주 방향 필요.
- 개체별 랜덤 변조는 재현 어려운 버그를 만들 수 있어 시드를 고정하는 디버그 옵션을 둔다.
- 서식지 단위 `Update`에서 돌기 때문에 거리 검사를 매 틱 하는 비용은 `SimulationDistance` 내 개체 수로 제한되는지 프로파일 확인.
- 신규 필드 추가는 기존 에셋 직렬화 호환 문제 없음(기본값 유지), 단 필드 이름을 바꾸지 않는다.

## 5. 출처
- 사슴 신호: [Farm and Dairy](https://www.farmanddairy.com/?p=583068), [Realtree](https://realtree.com/deer-hunting/galleries/20-things-to-know-about-deer-body-language-and-behavior), [Deer & Deer Hunting](https://www.deeranddeerhunting.com/article-index/8-messages-deer-send-hunters-with-their-tails), [iNaturalist](https://greece.inaturalist.org/posts/53455-the-white-tailed-deer-has-more-than-just-a-caudal-flag)
- 토끼: [Wageningen 논문](https://edepot.wur.nl/34807), [Better Planet Education](https://betterplaneteducation.org.uk/factsheets/rabbit-daily-life)
- 멧돼지: [PMC7680034](https://pmc.ncbi.nlm.nih.gov/articles/PMC7680034), [Frontiers 2017](https://frontiersin.org/articles/10.3389/fevo.2017.00157/pdf), [PeerJ](https://peerj.com/articles/10409.pdf)
- 닭: [Farms.com](https://www.farms.com/news/common-backyard-chicken-behaviors-186947.aspx), [Pet Circle](https://www.petcircle.com.au/discover/chicken-behaviour)
- 늑대: [NPS Fauna 7](https://www.nps.gov/parkhistory/online_books/fauna7/fauna5d.htm), [Smithsonian National Zoo](https://nationalzoo.si.edu/node/10397), [Wikipedia](https://en.wikipedia.org/wiki/Hunting_behavior_of_gray_wolves)
- 곰: [Outdoor Life](https://www.outdoorlife.com/conservation/how-fast-do-bears-run/), [PopSci](https://www.popsci.com/environment/how-fast-can-a-bear-run/)
- 아이벡스: [Swissinfo](https://www.swissinfo.ch/eng/ibex-tread-warily-in-avalanche-country/3746022), [Padova 대학](https://research.unipd.it/handle/11577/3608761), [Freiburg 논문](https://www.biometrie.uni-freiburg.de/lehre/PastThese/m.sc.thesis-hannah-weber.pdf)
- 거위: [USGS](https://www.usgs.gov/media/images/defensive-behavior-domestic-geese), [Backyard Poultry](https://backyardpoultry.iamcountryside.com/poultry-101/learn-to-speak-goose/)
- 쥐: [Refinement Database](https://refinementdatabase.org/publications/thigmotactic-responses-in-an-open-field/), [SciELO](https://www.scielo.br/j/bjmbr/a/4mFTPpRJDdSCg98qkqskNrk/?lang=en)
- 독사: [Bangor 대학](https://research.bangor.ac.uk/portal/files/57305635/hb157_14.pdf), [Kingsnake.com](https://kingsnake.com/blog/archives/2546-Whiskered-vipers-in-the-sand.html)
- 소·돼지: [Grandin flight zone](https://www.grandin.com/behaviour/principles/flight.zone.html), [Merck Vet Manual](https://www.merckvetmanual.com/behavior/behavior-of-production-animals/herd-behaviors-and-moving-herd-animals)
- 보행: [NYCU 말 보행 합성](https://ir.lib.nycu.edu.tw/bitstream/11536/21735/1/000318036300003.pdf), [Blender 포럼](https://blenderartists.org/t/how-to-use-stide-bone-with-a-four-legged-creature/459061)

## 6. 진행 상태와 추가 조사 (갱신)

### 6.1 결정 사항 반영
- 모델은 `APD_*`만 사용한다. 신규 클립은 만들지 않는다.
- 팩에 있으나 프로필에 연결되지 않은 기존 클립을 활용한다: 늑대 `Wolf_Howl`·`IdleB/C`, 염소 `Goat_Idle02`, 독사 `Viper_Hissing`·`Sleep`·`Glide`·`FastGlide`·`Attack`, 곰·멧돼지·소·염소·아이벡스 `*_Attack`(위협 연출 후보). 이 클립들로 4.2의 Observe/Rest/Threaten 슬롯을 채운다.
- 쥐·돼지는 팩에 달리기 클립이 없어 걷기 클립을 재사용 중이다. 도주 속도는 재생속도 배율(`PlaybackSpeedRange.y`)로 표현한다.

### 6.2 정밀 조사에서 새로 확인된 사실 (설계 보정)
| 종 | 확인 내용 | 설계 영향 |
|---|---|---|
| 사슴 | 흰꼬리사슴은 섭식 시간의 약 14%를 경계에 쓴다. 차량 접근 실험에서 도주 시작 거리는 접근 시작 거리의 약 40% (접근 속도와 무관). 식생이 빽빽하면 시야 상실로 더 멀리서 도주. 인간에 익숙한 개체군은 도주 거리가 짧다 | 섭식 중 경계 비율 약 14%를 기본값으로, 도주 거리에 개체차·시야 가림 보정을 둔다 |
| 토끼 | 꼬리 신호(tail flagging)는 은신처로부터 **중간 거리**에서 가장 많이 나타난다 (2025, 2,169회 도주 관찰). 은신처 거리가 FID와 경계에 영향 | 홈 거리에 따른 경계·도주 거리 변조, 중간 거리에서만 꼬리 신호 연출 |
| 곰 | 스칸디나비아 갈색곰 도주 개시 거리는 활동 중 평균 115m, 정지 중 69m이고 80%는 접근 중 자리를 떠난다. 반복 접근해도 **습관화되지 않는다** | 곰은 멀리서 먼저 물러나는 회피형, 습관화 대상에서 제외 |
| 쥐 | 먼 자극에는 얼어붙기, 거리가 가까워지면 소리·점프·공격으로 바뀌는 **거리 단계형 방어**. 농장 야생 쥐는 신기한 대상에 대한 경계가 약함 | 거리 단계 구조(Freeze → Flee → 근접 방어)를 공통 파이프라인으로 |
| 독사 | 방어 일격은 포식 일격보다 **약 2배 먼 거리**에서 시작된다. 행동 전환 거리가 대체로 고정 | 독사 경고(Hissing) → 일격 거리를 고정 임계값으로 |
| 닭 | 야외 방목 닭의 가장 흔한 활동은 서기·쪼기·걷기. 경계 빈도를 정량화한 연구는 찾지 못함 | 쪼기-서기-걷기 3박자 유지, 경계 수치는 사슴 비율 준용 후 플레이 조정 |
| 공통 | 개구리 연구: 거리에 따라 **정지에서 도주로** 전환 | 거리 단계 반응을 종 공통 구조로 확정 |

한계: 토끼·사슴 도주 최고 속도, 닭의 경계 빈도, 곰의 보행 속도에 대한 신뢰할 만한 1차 수치는 끝내 찾지 못했다. 이 값들은 상대 비율 + 플레이 테스트로 정한다.

### 6.3 구현 현황
| 항목 | 상태 |
|---|---|
| 레거시 `Pig`(프로필·프리팹·Hunting 드롭·클립 5개) 제거, 테스트 케이스 정리 | 완료. 참조 검색상 씬·레이아웃 미사용 |
| 레거시 `Deer`·`Songbird`·`Butterfly_01~03` | **제거 불가 — 사용 중.** `LakeVillageWorld` 씬과 `LakeOfLife.wildlife.json`이 사슴 8, 새 6, 나비 12 배치를 참조. 사슴·나비는 APD로 교체 가능, `Songbird`는 APD에 새가 없어 대체 불가 |
| 레거시 `Wolf` | 보존. 앰비언트가 아니라 `WoodlandWolf` 전투 몬스터 정의가 참조 |
| 코드: `AvoidSpeed`(회피 속도), `FleeAcceleration`(도주 가속), `FleeTurnMoveSpeedRatio`(도주 중 회전 감속 완화) 추가 | 완료. 기본값은 기존 동작과 동일. `Assembly-CSharp` dotnet 빌드 오류 0 |
| APD 프로필 13종 수치 재보정 (도주 속도·가속·회피 속도·회전·일부 정지 시간) | 완료. **발 접지·체감 미검증** |
| EditMode 테스트·Play 확인 | **미수행** (Unity MCP 연결 불안정으로 테스트 결과를 받지 못함) |
| 미변경 | 독사·도롱뇽·달팽이·나비·`UseBirdMotion` 새 |
| 남은 단계 | 거리 단계 반응 일반화(4.2 B), 마이크로 행동·섭식 경계(C), 무리(D), 미연결 팩 클립 연결 |

### 6.4 갱신 (새·나비 예외 허용 이후)
- 결정: 새(`Songbird`)와 나비(`Butterfly_01~03`)는 APD 전용 방침의 예외로 유지한다. 두 종의 프로필·프리팹·`UseBirdMotion` 코드는 그대로 둔다.
- `LakeVillageWorld` 씬의 레거시 사슴 6마리(`Deer_01~06`)를 같은 위치·방향의 `APD_Deer`/`APD_DeerMale`로 교체하고 저장했다(NavMesh·접지 재검증 통과).
- 수치 원본 동기화: `Tools/Wildlife/behavior-tuning.json`이 "관찰 행동 데이터 적용" 도구의 원본이라 에셋만 고치면 재실행 때 덮어써진다. 13종 값을 JSON에도 반영하고, 도구의 `BehaviorSettings`에 `AvoidSpeed`·`FleeAcceleration`·`FleeTurnMoveSpeedRatio`를 추가했다. 쥐·돼지의 `PlaybackSpeedRange`는 도구가 다루지 않으므로 에셋에만 있다.
- 레거시 `Deer` 프로필·프리팹은 **보존**: 테스트 씬 `WildlifeBehavior.unity`의 `WildlifePlayerProbe._deerPrefab`이 아직 참조한다. 이 참조를 `APD_Deer`로 바꾼 뒤 제거할 수 있다(Unity 연결 불안정으로 미완). 구 씬 `LakeOfLife.unity`는 레거시 프리팹이 아닌 자체 사슴을 쓴다.

### 6.5 갱신 (레거시 사슴 제거, 접근 속도 도주, 섭식 경계)
- 레거시 `Deer` 완전 제거: 프로필·프리팹 2개·드롭 테이블·클립 5개 삭제, `GroundSpecies.catalog.json`·`Hunting.catalog.json`·`LakeOfLife.wildlife.json`의 `Deer` 항목 정리, 테스트 씬의 `_deerPrefab`을 `APD_Deer`로 교체. 제거 직후 Wildlife EditMode 34개 통과.
- 신규 프로필 필드(기본값은 기존 동작): `FleeTimeToContact`(접근 속도 기반 조기 도주), `FeedBurstSeconds`·`VigilanceSeconds`(섭식 중 머리 들기). 판단 주기 사이 거리 변화로 접근 속도를 구하고 걷는 접근(2m/s 미만)은 제외한다. 11종에 값 적용(`behavior-tuning.json`과 에셋 동시).
- 신규 EditMode 테스트 3개 추가(`WildlifeGroundTests`): 빠른 접근 도주 2케이스, 섭식 경계 1케이스. **실행 결과 미확인** — 컴파일(dotnet) 오류 0, Unity 연결이 끊겨 결과를 받지 못했다.
- 아직 안 한 것: 거리 단계 반응의 종별 일반화(AlertStyle/EscapeStyle), 지그재그·홈 귀환 도주, 무리 전파 지연·추종, 팩 미연결 클립(늑대 하울링, 독사 쉭쉭 등) 연결, Play 모드 체감·발 접지 검수.

### 6.6 종별 도주 방식·개체 차·무리 전파·위협 모션 (구현 완료, 체감 검수 대기)

- 6.5의 테스트 3개 포함 Wildlife EditMode 39개 모두 통과. 컴파일 오류 0.
- `WildlifeProfileSO` 신규 필드(전부 데이터, 코드에 종 분기 없음)
  - `ZigZagAngle`: 도주 판단마다 좌우로 번갈아 꺾는다. 토끼 40, 쥐 50.
  - `ScatterAngle`: 개체별 고정 방향 편차. 닭 60, 거위 30.
  - `BoldnessVariance`: 개체별 경계·회피·도주 거리 배율 편차(0.15~0.3).
  - `SocialFleeDistance` / `AlarmDelaySeconds`: 같은 프로필 동료가 도주하면 반응 지연 뒤 같은 위협에서 따라 도주. 소 0.3~0.9초로 느리게, 토끼·닭은 짧게.
  - `ThreatenDistance` / `Threaten`: 대치 거리 안에서 `*_Attack` 클립을 위협 표현으로 재생. 멧돼지·염소·아이벡스·소·늑대·곰.
- 클립 연결은 `behavior-tuning.json`의 `threatenSource`로 관리하며 `ApplyAnimalBehavior`가 복사한다. 늑대 IdleB/C, 염소 Idle02, 독사 Hissing/Sleep은 이미 연결돼 있었다.
- 새 테스트: `도주하는_동료를_보면_반응_지연_뒤_같은_위협에서_따라_도주한다`.
- 미검수: Play 모드 체감, 발 접지, 위협 모션이 Attack 클립의 돌진 동작처럼 보이지 않는지, 지그재그 각도, 무리 도주 방향.
- 남은 미튜닝 종: 독사·도롱뇽·달팽이·나비·새(기존값 유지).
