# Anime Character Auto Shading Generator 설계 문서

> 작성일: 2026-06-07
> 대상 환경: Unity 6 (6000.0.60f1), URP, lilToon (`jp.lilxyzw.liltoon`, uber `lts.shader`)
> 레퍼런스: 명조/원신/세키로 캐릭터 셰이딩, lilToon 공식 셰이더 소스(설치 패키지), [AnimeShadingPlus](https://github.com/EricHu33/AnimeShadingPlus-Anime-Toon-Shader) Face Shadow Map 베이킹 워크플로
> 상태: **설계 (미구현)**

---

## 0. 핵심 결론 (먼저 읽을 것)

> **전제:** 이 프로젝트는 **프로그래머 1인 개인 프로젝트이며, 아티스트가 제작한 멀티앵글 마스크(`LightAngle_00~180`)가 없다.** 따라서 도구는 **아트 에셋 없이(zero-asset) 코드만으로 동작**해야 한다. 풀 SDF 페이스 섀도우는 그 입력(오써링된 앵글 마스크)이 없으면 성립하지 않는다 — 이는 *"SDF 중심으로 만들지 말라"* 던 원본 기획서의 본래 직관을 다시 정당화한다.

| 항목 | 원본 기획 판단 | 실제 결론 (마스크 없음 전제) |
|------|----------------|------------------------------|
| 풀 SDF 페이스 섀도우 (`_ShadowMaskType=2`) | 난이도 ★★★★★ → 포기 | lilToon 네이티브로 렌더는 되지만 **오써링된 앵글 마스크가 입력으로 필요** → **현 단계 보류** |
| 얼굴 그림자 MVP | 1D 그래디언트 근사 | **lilToon Flat 그림자(`_ShadowMaskType=1`)** — `flatN`을 오브젝트 정면에서 **절차 계산**, 광원 방향에 반응, 코·입 노이즈 없음, **픽셀 오써링 0** (§2.4) |
| 1클릭 결과물 | — | **Back Light + Rim + Rim Shade + Flat 페이스 그림자 = 모두 무(無)아트 에셋, 프로퍼티 구동** |

**lilToon 그림자에는 두 경로가 있다:**

1. **Flat (`_ShadowMaskType == 1`) — 채택.** `flatN = LIL_MATRIX_M · (0,0.25,1)` (오브젝트 정면, 살짝 위로 틸트)와 광원 방향의 내적으로 부드러운 단일 경계 그림자를 만든다. 메시 지오메트리(코·눈두덩)를 무시하므로 **깔끔**하고, **픽셀 단위 마스크가 필요 없다**. 광원이 캐릭터를 돌면 경계가 따라 움직인다 — 스타일라이즈드 얼굴 반그림자.
2. **SDF (`_ShadowMaskType == 2`) — 보류.** `_ShadowStrengthMask`의 R/G/B 채널에 오써링된 앵글 거리장이 있어야 한다(§2.4). **마스크가 없으면 사용 불가.** 추후 아트 에셋이 생기면 활성화하는 V2 옵션으로만 문서화한다.

> **자동으로 N개 각도에서 얼굴을 렌더해 마스크를 생성하는 접근은 막다른 길이다.** 자동 렌더 마스크는 SDF가 애초에 제거하려는 지오메트리 자기그림자 노이즈(코·입·눈두덩)를 그대로 재현해 일반 N·L보다 오히려 나쁘다. 옵션이 아니라 **알려진 사장(死藏) 경로**로 표기한다.

또한 원본 기획의 **런타임 `MaterialPropertyBlock` 세팅 접근은 lilToon에 부적합**하다(§2.2). 이 도구는 **에디터 타임에 머티리얼 에셋 자체에 값을 베이크**하는 생성기로 설계한다.

---

## 1. 목표

캐릭터 Prefab(또는 머티리얼 세트) 하나를 입력하면, 1클릭으로 서브컬처 캐릭터 셰이딩 세팅을 완료한다.

자동 생성 대상:

```
Face Shadow (Flat — 절차 계산, 마스크 없음)
Hair Shadow (앞머리 → 얼굴 투영 마스크)
Back Light  (역광)
Rim Light   (외곽광)
Rim Shade / AO Boost (목·귀·머리카락 밑 음영 강조)
```

핵심 원칙:
- **lilToon 네이티브 기능을 켜고 값을 채운다.** 자체 셰이더를 만들지 않는다.
- **에디터 타임 베이크.** 런타임 주입이 아니라 머티리얼 에셋·텍스처 에셋을 생성/수정한다.
- **비파괴 우선.** 원본 머티리얼을 복제한 `*_AutoShade` 머티리얼/변형을 만들고, 되돌리기를 보장한다.

---

## 2. 현황 분석 (설계의 출발점)

표준 "Auto Shading" 튜토리얼을 그대로 적용할 수 없는 **이 프로젝트·lilToon 고유 제약**이 있다. 설계는 이 제약 위에서 성립해야 한다.

### 2.1 lilToon 실제 프로퍼티 (스펙의 가상 프로퍼티명 교정)

원본 기획서가 사용한 프로퍼티명은 **lilToon에 존재하지 않는다.** 실제 프로퍼티로 매핑해야 한다. (출처: 설치 패키지 `Shader/Includes/lil_common_input.hlsl`, `Shader/lts.shader`)

| 기획서 가상명 | lilToon 실제 토글 | lilToon 실제 값 프로퍼티 |
|---------------|-------------------|--------------------------|
| `_HairShadowStrength`, `_HairShadowOffset` | `_UseShadow` (=1) | `_ShadowStrengthMask`(텍스처), `_ShadowStrength`, `_ShadowBorder`, `_ShadowBlur`, `_ShadowColor` |
| Face Shadow (SDF) | `_UseShadow` + `_ShadowMaskType`(Flat/SDF) | `_ShadowStrengthMask`(SDF, R/G/B 채널), `_ShadowFlatBorder`, `_ShadowFlatBlur` |
| `_BackLightColor`, `_BackLightPower`, `_BackLightIntensity` | `_UseBacklight` (=1) | `_BacklightColor`, `_BacklightMainStrength`, `_BacklightBorder`, `_BacklightBlur`, `_BacklightDirectivity`, `_BacklightViewStrength`, `_BacklightReceiveShadow` |
| `_RimPower`, `_RimColor`, `_RimIntensity` | `_UseRim` (=1) | `_RimColor`, `_RimBorder`, `_RimBlur`, `_RimFresnelPower`, `_RimEnableLighting`, `_RimShadowMask`, `_RimDirStrength`, `_RimDirRange` |
| `_AOBoostMask` (AO Boost) | `_UseRimShade` (=1) **또는** `_ShadowStrengthMask` AO 베이크 | `_RimShadeColor`, `_RimShadeBorder`, `_RimShadeBlur`, `_RimShadeFresnelPower` / `_ShadowAOShift`, `_ShadowPostAO` |

> **작성 원칙:** 코드/문서의 프로퍼티명은 반드시 위 lilToon 실제명을 사용한다. 기획서 가상명을 코드에 쓰지 않는다.

### 2.2 lilToon 빌드 시 기능 스트립 (가장 중요한 제약)

lilToon은 빌드 직전(`lilToonBuildProcessor.OnPreprocessBuild`) 셰이더 최적화로 **사용되지 않는 기능을 `#define`(`LIL_FEATURE_*`)에서 제거**한다. 사용 여부 판정은 *"빌드 씬에서 참조된 머티리얼"* 만 스캔한다(`lilToonSetting.WalkAllSceneReferencedAssets`).

이 프로젝트는 이미 동일 문제를 디졸브에서 겪었고 `LilToonDissolveKeepAliveSetup`(`Assets/02.Scripts/Tool/Editor/`)으로 우회 중이다 — Boot 씬에 keep-alive 머티리얼을 참조하는 비활성 오브젝트를 두어 최적화 스캔에 잡히게 한다.

**설계 함의:**
1. 생성된 `*_AutoShade` 머티리얼은 반드시 **빌드 씬에서 참조**되어야(=캐릭터 프리팹이 씬/Addressables 경유로 참조) Backlight/Rim/Shadow 기능이 빌드에서 살아남는다.
2. Addressables-only 로 로드되는 캐릭터라면 디졸브와 같은 **KeepAlive 패턴**을 적용하거나, 해당 기능을 켠 머티리얼을 빌드 씬에 참조시켜야 한다.
3. `MaterialPropertyBlock`/`material.SetFloat`로 런타임에 기능을 "켤" 수 없다 — 기능은 셰이더 컴파일 타임 `#define`으로 결정되며 스트립된다. **→ 에디터 타임 베이크가 필수인 핵심 근거.**

### 2.3 머티리얼 셰이더 전제 조건

- 기존 캐릭터 머티리얼(예: `ExternalAssets/Character/【SE】Komoe`)은 uber `lilToon` 셰이더(`_lilToonVersion: 45`)를 사용한다 → `material.SetFloat("_UseBacklight", 1)` 등이 **에디터에서 정상 동작**한다.
- 단, lilToon이 머티리얼을 **per-material 최적화 셰이더로 베이크**한 경우(`Optimize` 적용), 기능이 이미 `#define` 아웃되어 `SetFloat`로 복원되지 않는다. 이런 머티리얼은 **건너뛰거나 uber 셰이더로 되돌린 뒤** 처리해야 한다.
- **전제 검사:** 처리 전 각 머티리얼의 `shader.name`이 uber lilToon(`lilToon`, `lilToonOutline`, `Hidden/lts...` 변형 제외)인지 확인한다.

### 2.4 lilToon 얼굴 그림자 — Flat vs SDF (소스 확인)

`lil_common_frag.hlsl`에는 `_ShadowMaskType`으로 갈리는 **두 개의 광원 방향 반응 경로**가 있다.

**(A) Flat 경로 `_ShadowMaskType == 1` (`:1053~1062`) — 채택, 마스크 불필요:**
```hlsl
if(_ShadowMaskType == 1)
{
    // flatN = 오브젝트 정면(+Z)을 살짝 위로 틸트. 메시 노멀이 아니라 변환행렬에서 절차 계산.
    float3 flatN = normalize(mul((float3x3)LIL_MATRIX_M, float3(0.0, 0.25, 1.0)));
    float lnFlat = saturate((dot(flatN, fd.L) + _ShadowFlatBorder) / _ShadowFlatBlur);
    ...
    lns = lerp(lnFlat, lns, shadowStrengthMask.r);   // ← R로 Flat ↔ 일반 N·L 블렌드
}
```
- `flatN`이 **오브젝트 정면**이므로 코·눈두덩 같은 메시 디테일을 무시 → 깔끔한 단일 경계. **픽셀 마스크·노멀 편집 불필요.**
- 광원(`fd.L`)이 캐릭터를 돌면 경계가 이동 → 스타일라이즈드 반그림자.
- **단, 마지막 줄의 `shadowStrengthMask.r` 주의:** 기본 `_ShadowStrengthMask`=흰색(R=1)이면 `lerp(lnFlat, lns, 1)=lns`라 **Flat이 무효**가 된다. **적용할 영역에서 R을 0**으로 만들어야 한다.
  - **얼굴이 별도 머티리얼인 경우(권장 경로):** `_ShadowStrengthMask`에 **솔리드 검정 텍스처 1장**(또는 R=0 단색)만 넣으면 얼굴 전체에 Flat 적용. **픽셀 오써링 0.**
  - **얼굴이 몸과 같은 머티리얼인 경우:** UV/서브메시에서 얼굴 영역만 R=0인 **이진 영역 마스크**를 절차 생성해야 한다(이건 거리장이 아니라 단순 영역 구분이라 자동화 쉬움).

**(B) SDF 경로 `_ShadowMaskType == 2` (`:953~967`) — 보류, 오써링 마스크 필요:**
```hlsl
if(_ShadowMaskType == 2) {
    float sdf = LdotR < 0 ? shadowStrengthMask.g : shadowStrengthMask.r; // 좌/우 광원별 채널
    ...
    lns = lerp(saturate(lnSDF*0.5 + sdf*0.5 + 0.25), lns, shadowStrengthMask.b);
}
```
`_ShadowStrengthMask`의 R/G/B에 오써링된 거리장이 있어야 동작한다. **마스크가 없으면 사용 불가** → 현 단계 보류.

| 모드 | `_ShadowMaskType` | 입력 | 품질 | 현 채택 |
|------|-------------------|------|------|---------|
| Flat | 1 | 없음(또는 단색/이진 영역 마스크) | 깔끔·단순 경계 | ✅ MVP |
| SDF | 2 | 오써링된 R/G/B 거리장 마스크 | 아트 의도 반영 최상 | ⏸ V2(에셋 생기면) |

공통으로 `_ShadowFlatBorder`/`_ShadowFlatBlur`로 경계 위치·소프트니스를 조절한다.

> **한계:** `flatN`은 렌더러의 변환행렬(`LIL_MATRIX_M`) 기반이라 **머리 본의 회전을 따라가지 않는다**(캐릭터 몸 정면 기준). 대부분의 경우 허용 가능하나, 고개를 크게 돌리는 연출에선 경계가 얼굴과 어긋날 수 있다.

---

## 3. 시스템 구성

에디터 타임 파이프라인. 각 단계는 독립 클래스로 분리하고, 마지막에 Applier가 머티리얼에 일괄 적용한다.

```
[입력] Character Prefab / 머티리얼 세트
        │
        ▼
Character Analyzer ── Head/Face/Hair/Body Renderer·Bone 탐색 → CharacterShadingInfo
        │
        ├─▶ Face Detector  ── Face 렌더러·머티리얼·UV·정면 방향 식별
        ├─▶ Hair Detector  ── 앞머리 렌더러 식별(bounds.y > head.y / 이름 규칙)
        │
        ▼
Texture Generators (선택 — MVP는 대부분 텍스처 불필요)
        ├─ (MVP) Face=Flat 그림자 → 단색 검정 마스크 1장 또는 마스크 없음 (베이크 X)
        ├─ HairShadowBaker        ── 앞머리 메시 → 얼굴 UV 투영 → 그림자 마스크 (V2.5)
        ├─ AoBoostBaker           ── 목/귀/머리밑 영역 → AO 마스크 (V3, 선택)
        └─ FaceShadowSdfBaker     ── (V2/보류) LightAngle_XX 마스크 → SDF 맵. 아트 에셋 생기면.
        │
        ▼
Material Generator ── 원본 복제(*_AutoShade) + lilToon 토글·값 세팅 (Flat/Backlight/Rim/RimShade)
        │
        ▼
Preview Window ── 라이트 회전 0~360° 실시간 미리보기 + 기능별 On/Off 토글
        │
        ▼
[출력] *_AutoShade.mat (+ MVP는 텍스처 0~1장; V2+에서 *_HairShadow.png 등)
```

---

## 4. Character Analyzer

### 역할
Prefab을 분석해 얼굴/머리/몸 렌더러와 Head 본을 식별하고, 이후 생성기가 공유할 정보 객체를 만든다.

### 탐색 규칙
- **Head 본:** Animator(휴머노이드)면 `animator.GetBoneTransform(HumanBodyBones.Head)`. 아니면 이름 규칙(`Head`, `head`, `J_Bip_C_Head`).
- **Face 렌더러:** Head 본 하위 SkinnedMeshRenderer 중 이름(`Face`, `Body`(VRoid는 Face가 Body에 포함되기도)) / 머티리얼명 매칭.
- **Hair 렌더러:** 이름 포함(`Hair`, `FrontHair`, `Bang`) **또는** `renderer.bounds.center.y > head.position.y` 보조 판정.
- **결과 객체:**

```csharp
namespace UPlayGround.Tool.Shading
{
    public struct CharacterShadingInfo
    {
        public Transform Head;
        public SkinnedMeshRenderer Face;
        public SkinnedMeshRenderer Hair;
        public SkinnedMeshRenderer[] Body;
        public Material[] FaceMaterials;   // uber lilToon 만 필터링
        public Material[] HairMaterials;
    }
}
```

자동 탐색 실패 시 미리보기 창에서 **수동 지정 슬롯**을 제공한다(사용자가 직접 드래그).

---

## 5. Face Shadow Generator

> **마스크 없음 전제(§0).** 풀 SDF가 아니라 **lilToon Flat 그림자**를 채택한다. 픽셀 오써링 없이 코드로 머티리얼 프로퍼티만 세팅한다.

### 5.1 채택 — Flat 그림자 (MVP, 무 아트 에셋)

#### 목표
Face 머티리얼에 lilToon Flat 그림자를 활성화해, 광원 방향에 반응하는 깔끔한 얼굴 반그림자를 만든다. 입력 텍스처 베이크 없음(또는 단색/이진 영역 마스크 1장).

#### lilToon 머티리얼 세팅
```
_UseShadow      = 1
_ShadowMaskType = 1            // Flat (소스 :1053 확인, 정수값 in-editor 재확인)
_ShadowFlatBorder ≈ 0.0        // 경계 위치(-2~2). 0 근처 = 얼굴 절반
_ShadowFlatBlur   ≈ 0.1~0.3    // 경계 소프트니스(0.001~2)
_ShadowColor, _Shadow2ndColor = 1차/2차 그림자 색(서브컬처 톤: 채도 있는 보라/청색)
```

#### `_ShadowStrengthMask` 처리 (Flat 적용 영역, §2.4 주의)
Flat 결과는 `lerp(lnFlat, lns, _ShadowStrengthMask.r)`로 블렌드된다. 기본 흰색(R=1)이면 무효이므로 **적용 영역에서 R=0**이 되게 한다:

| 케이스 | 처리 | 비용 |
|--------|------|------|
| **얼굴이 별도 머티리얼** (권장) | `_ShadowStrengthMask`에 **솔리드 검정** 텍스처 1장 할당 (또는 R=0 단색) | 픽셀 오써링 0 |
| **얼굴이 몸과 공유 머티리얼** | UV/서브메시로 얼굴 영역만 R=0인 **이진 영역 마스크** 절차 생성 | 거리장 아님 → 자동화 쉬움 |
| **몸 전체에 Flat 허용** | 마스크 그대로(R=0 단색) — 몸도 Flat 반그림자 | 0 |

> 도구는 먼저 Face가 별도 머티리얼인지 판정(§4)하고, 그렇다면 1×1 검정 텍스처를 `_ShadowStrengthMask`에 넣는 가장 단순한 경로를 택한다.

### 5.2 보류 — 풀 SDF (`_ShadowMaskType = 2`, V2: 아트 에셋 생기면)

오써링된 멀티앵글 마스크(`LightAngle_00~180`, 권장 5장)가 **생겼을 때만** 활성화하는 경로. 베이커 사양은 향후 구현을 위해 기록만 한다(현재 미구현):

- 픽셀별로 N장을 스캔해 "그림자에 처음 덮이는 광원 각도" → 0~1 거리장.
- **R = 오른쪽 광원 거리장, G = 왼쪽 광원 거리장, B = SDF↔N·L 블렌드 비율** (§2.4 채널 규약). 좌우 대칭 얼굴이면 한 시퀀스를 반전해 R/G 생성.
- `<Face>_FaceSDF.png` 저장(sRGB off, 무손실/고품질). `_ShadowMaskType=2`, `_ShadowStrengthMask`에 할당.

> **막다른 길 — 자동 마스크 생성 금지(§0):** 도구가 얼굴을 N개 각도로 렌더해 마스크를 "자동 생성"하는 방법은 SDF가 제거하려는 코·입 자기그림자 노이즈를 재현해 일반 N·L보다 나쁘다. 채택하지 않는다.

### 5.3 품질 업그레이드 — 구형 노멀 전사 (V2, 메시 수정 동반)

별도 옵션. 얼굴 메시 노멀을 부드러운 타원체에서 투영한 노멀로 **덮어써** lilToon 일반 N·L이 코·눈두덩 노이즈 없이 깔끔하게 셰이딩되게 한다(서브컬처 표준 기법). **마스크 불필요·코드만으로 가능**하나 **메시 에셋(스킨드 메시)을 수정**하므로 난이도가 있어 MVP가 아니다. Flat으로 충분치 않을 때의 상위 옵션으로 문서화한다.

> 1D 그래디언트(`head.right · lightDir`) 근사는 Flat보다 나을 게 없어(둘 다 무 마스크, Flat이 더 깔끔) **별도 폴백으로 두지 않는다.**

---

## 6. Hair Shadow Generator (V2)

### 목표
앞머리가 얼굴(이마)에 드리우는 그림자를 자동 생성.

### 방식
1. Hair 렌더러의 메시 bounds를 추출.
2. Face의 **정면 방향(`head.forward`)** 기준으로 앞머리 실루엣을 얼굴 UV 공간에 정사영(orthographic projection)해 그림자 마스크를 렌더(베이크용 임시 카메라/`Graphics.Blit` + 투영 행렬).
3. 블러·강도 조절 후 `<Face>_HairShadow.png` 저장.

### lilToon 적용
앞머리 그림자는 **2차 그림자 마스크**로 합성하는 것이 자연스럽다 → `_Shadow2ndColorTex` 또는 `_ShadowBorderMask`에 곱해 넣거나, Face SDF 맵 위에 오버레이. (정확한 합성 슬롯은 구현 중 in-editor 검증 필요.)

> 정적 마스크라 머리카락이 흔들리는 런타임 그림자는 표현 못 한다. AAA식 동적 앞머리 그림자(shadowmap/SDF projection)는 범위 밖.

---

## 7. Back Light Generator (MVP)

### 목표
명조 스타일 역광(빛 번짐).

### lilToon 세팅
```
_UseBacklight        = 1
_BacklightColor      = (캐릭터 키컬러 기반 따뜻한 색)
_BacklightMainStrength ≈ 0.3
_BacklightBorder, _BacklightBlur = 경계
_BacklightDirectivity, _BacklightViewStrength = 시점 의존 정도
_BacklightReceiveShadow = 1 (그림자 영역에선 약화)
```
기획서의 `Power=3, Intensity=0.3`은 각각 `_BacklightBorder`/소프트니스와 `_BacklightMainStrength`로 매핑한다(1:1 수치 아님, 추천 프리셋으로 제공).

---

## 8. Rim Light Generator (MVP)

### 목표
캐릭터 외곽 강조. 계산식 `1 - dot(normal, view)`는 lilToon `_RimFresnelPower`가 내장 처리.

### lilToon 세팅
```
_UseRim          = 1
_RimColor        = (외곽광 색)
_RimFresnelPower ≈ 4   (기획서 Power=4)
_RimBorder, _RimBlur = 경계
_RimEnableLighting = 1 (광원 색 반영)
_RimShadowMask   = 1 (그림자 영역 림 억제)
_RimDirStrength, _RimDirRange = 광원 방향 쪽만 림(명조식 방향성 림)
```
추천 강도 ≈ 0.2(기획서 Intensity=0.2)는 `_RimColor` 알파/밝기로 반영.

---

## 9. AO Boost / Rim Shade Generator (V3)

### 목표
목·귀·머리카락 밑 음영 강조.

두 가지 경로 — 캐릭터/요구 품질에 따라 선택:

- **A. Rim Shade (절차적, 마스크 불필요):** lilToon `_UseRimShade=1`. 시야 기준 가장자리에 어두운 음영을 절차 생성 → AO 유사 효과. `_RimShadeColor`, `_RimShadeBorder`, `_RimShadeFresnelPower`. **MVP 친화적(베이크 0).**
- **B. AO 마스크 베이크:** 목/귀/머리밑 영역을 식별해 AO 마스크(`<Body>_AO.png`)를 만들고 `_ShadowStrengthMask`(또는 메인 AO)에 곱한 뒤 `_ShadowAOShift`/`_ShadowPostAO`로 강조. 품질 높지만 영역 식별·베이크 비용.

권장: **V3에서 A(Rim Shade) 먼저**, B는 선택.

---

## 10. Material Generator & 비파괴 정책

- 원본 `Foo.mat` → **복제** `Foo_AutoShade.mat` 생성(또는 Unity Material Variant). 렌더러의 슬롯을 복제본으로 교체.
- 원본 머티리얼·텍스처는 절대 직접 수정하지 않는다(아티스트 자산 보호).
- 생성 결과를 기록한 **사이드카 에셋**(예: `AutoShadeManifestSO` 또는 머티리얼명 규칙)으로 "되돌리기"와 "재생성(overwrite)"을 지원.
- 재실행 시 기존 `*_AutoShade` 가 있으면 텍스처는 보존/덮어쓰기 선택, 프로퍼티는 재적용.

---

## 11. 에디터 도구 구성

프로젝트 컨벤션(메뉴 루트 `UPlayGround/`, 한국어, `UPlaygroundMenuPriority` 상수, 네임스페이스 `UPlayGround.*`, Editor 코드는 `Editor/` 하위)을 따른다.

| 클래스 | 위치 | 역할 |
|--------|------|------|
| `CharacterShadingAnalyzer` | `Assets/02.Scripts/Tool/Shading/Editor/` | 프리팹 분석 → `CharacterShadingInfo` |
| `FaceShadowSdfBaker` | 〃 | 멀티앵글 마스크 → SDF 맵 베이크 |
| `HairShadowBaker` | 〃 | 앞머리 → 얼굴 투영 마스크 |
| `AoBoostBaker` | 〃 | AO 마스크(선택) |
| `LilToonShadingApplier` | 〃 | lilToon 토글/값/텍스처 일괄 세팅 + 비파괴 복제 |
| `AutoShadingWindow` | 〃 | `EditorWindow` — 입력 슬롯·생성·프리뷰 |
| `AutoShadingPreset` (SO) | `Assets/10.Datas/Rendering/` | 색·강도 추천 프리셋(서브컬처 톤) |

메뉴 경로(안): `UPlayGround/렌더링/자동 셰이딩 생성기`.

### Preview Window
- 라이트 회전 슬라이더 0~360° → SDF 그림자 경계가 따라 움직이는지 실시간 확인.
- 기능별 토글: `Face Shadow / Hair Shadow / Back Light / Rim Light / Rim Shade(AO)` On/Off.
- 프리뷰는 인스펙터/`PreviewRenderUtility`에서만 그리고, **확정 전엔 머티리얼 에셋을 수정하지 않는다**(InputPrompt 에디터 프리뷰 선례와 동일 원칙).

---

## 12. 워크플로

**MVP (무 아트 에셋 — 현 환경):**
```
사용자: 메뉴 → 자동 셰이딩 생성기 → Prefab 드래그 → (프리셋 선택) → Generate
        │
        ▼ (~10초)  ※ 텍스처 베이크 없음. lilToon 토글/값만 세팅 + 얼굴 단색 마스크 1장(필요 시)
*_AutoShade.mat 생성·렌더러 적용 (Flat 페이스 그림자 + Backlight + Rim + RimShade)
        │
        ▼
Preview에서 라이트 회전 0~360° → 경계 이동 확인 → 프리셋/강도 조절 → 확정
```

**V2 (아트 에셋이 생겼을 때 — 풀 SDF 옵션):**
```
아티스트/직접: LightAngle_00~180.png 제작 (얼굴 UV, 권장 5장)
        │
사용자: 생성기에서 "SDF 모드" 토글 + 마스크 폴더 지정 → Generate
        │
        ▼
*_FaceSDF.png 베이크 + _ShadowMaskType=2 적용
```

---

## 13. 구현 우선순위

| 단계 | 범위 | 비고 |
|------|------|------|
| **MVP** | Character Analyzer + **Face Shadow(Flat)** + Back Light + Rim Light + Rim Shade(절차적 AO) + Material Generator(비파괴) + Preview | **전부 무 아트 에셋**, lilToon 토글/값만으로 동작. 즉시 1클릭 가치. |
| **V2** | 구형 노멀 전사(§5.3, 메시 수정) 또는 풀 SDF(§5.2, 아트 에셋 생기면) | 얼굴 품질 상위 옵션. 둘 다 선택 사항. |
| **V2.5** | Hair Shadow Generator (앞머리 투영 마스크) | |
| **V3** | AO 마스크 베이크(§9 경로 B), 프리셋 라이브러리 확장 | |

> **마스크 없음 전제(§0)로 MVP를 재구성:** 얼굴 그림자는 오써링 SDF 대신 **lilToon Flat 그림자**로 MVP에 포함된다(무 에셋, 광원 반응, 깔끔). 풀 SDF는 아트 에셋이 생겼을 때의 V2 옵션으로 보류한다. MVP 전체가 텍스처 베이크 0 — 프로그래머 1인 환경에 맞는 즉시 동작 1클릭 경험.

---

## 14. 주의 사항 / 미해결 (구현 중 in-editor 검증 필요)

1. **빌드 스트립(§2.2)** — 생성 머티리얼이 빌드 씬에서 참조되는지, 아니면 KeepAlive 패턴이 필요한지 캐릭터 로드 방식(씬 직접 배치 vs Addressables)별로 확정.
2. **기능 활성화 메커니즘** — `material.SetFloat("_UseBacklight",1)`만으로 uber 셰이더에서 기능이 켜지는지, lilToon 에디터 API(`lilMaterialUtils`/인스펙터 apply) 호출이 추가로 필요한지 in-editor로 확인. 최적화 베이크된 머티리얼은 건너뛰거나 uber로 환원.
3. **`_ShadowMaskType` 정수값** — 소스상 Flat=1, SDF=2(`lil_common_frag.hlsl:1053`/`:953`). lilToon 인스펙터 enum과 대조해 코드 상수화.
4. **Flat 적용 `_ShadowStrengthMask.r`(§2.4·§5.1)** — 기본 흰색이면 Flat 무효. 얼굴 머티리얼에 R=0(검정) 마스크를 넣어야 적용됨을 실제로 확인. 얼굴이 별도 머티리얼인지(단색 마스크로 충분) 공유인지(이진 영역 마스크 필요) 케이스 검증.
5. **풀 SDF 채널 규약(보류 항목)** — V2에서 활성화 시: R=오른쪽광 / G=왼쪽광 / B=블렌드(§2.4·§5.2). `LdotR` 부호 정의를 베이커와 일치시킬 것.
6. **머티리얼 다양성** — Booth/VRoid/Asset Store 캐릭터는 lilToon 외 셰이더(Poiyomi 등)를 쓰기도 한다. uber lilToon이 아닌 머티리얼은 **명시적으로 스킵하고 로그**.
7. **자동 탐색 실패** — Head/Face/Hair 자동 식별이 캐릭터마다 다르므로 수동 지정 폴백을 필수로 제공.

---

## 15. 참고 (조사 출처)

- 설치 패키지 `Library/PackageCache/jp.lilxyzw.liltoon@.../Shader/Includes/lil_common_input.hlsl`, `lil_common_frag.hlsl`, `Shader/lts.shader` — 프로퍼티·SDF 로직 원본
- 프로젝트 선례: `Assets/02.Scripts/GameActor/Component/Common/DissolveController.cs`, `Assets/02.Scripts/Tool/Editor/LilToonDissolveKeepAliveSetup.cs` (lilToon 프로퍼티 조작 + 빌드 스트립 우회)
- [lilToon Lighting and Shadows (DeepWiki)](https://deepwiki.com/lilxyzw/lilToon/5.2-lighting-and-shadows)
- [AnimeShadingPlus — Face Shadow Map 베이킹 워크플로](https://github.com/EricHu33/AnimeShadingPlus-Anime-Toon-Shader)
