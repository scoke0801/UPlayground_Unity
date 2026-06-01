# DissolveController lilToon 적용 계획

작성일: 2026-06-01

## 목적

현재 `DissolveController`는 Addressables의 `DissolveMaterial`로 렌더러 머티리얼을 전부 교체한 뒤 `_DissolveAmount`를 `MaterialPropertyBlock`으로 올리는 방식이다.

lilToon 대상 모델에는 이 방식이 적합하지 않다. lilToon 머티리얼의 셰이딩, 아웃라인, 투명/컷아웃 설정, 각 캐릭터별 색/텍스처/마스크 설정을 잃기 쉽고, 머티리얼 슬롯별 원본 속성 복사 범위도 계속 늘어난다.

목표는 기존 `StartDissolve`, `CompleteDissolve`, `ResetDissolve`, `RefreshRenderers` 호출부를 유지하면서, lilToon 머티리얼은 원본 셰이더를 유지한 채 lilToon 내장 Dissolve 파라미터를 런타임 제어하도록 바꾸는 것이다.

## 조사 요약

### lilToon 공식 문서

공식 Dissolve 문서는 lilToon의 Dissolve가 머티리얼 트랜지션, 등장/퇴장, 변신, 부분 소거 표현에 쓰이는 기능이라고 설명한다. 파라미터는 효과 종류, 형태, 범위, 블러, 마스크, 좌표, 방향, 노이즈, 색으로 구성된다.

공식 기본 설정 문서 기준으로 lilToon은 불투명, 컷아웃, 반투명 등 렌더링 모드를 구분한다. 컷아웃은 투명도를 사용하지만 반투명은 표현하지 않고, 반투명은 알파 블렌딩을 사용하되 겹침 문제가 생길 수 있다.

참고:
- https://lilxyzw.github.io/lilToon/ja_JP/advanced/dissolve.html
- https://lilxyzw.github.io/lilToon/ja_JP/base/base.html

### 구현 사례

일본어 구현 사례에서는 lilToon Dissolve를 런타임에서 제어할 때 `_DissolveParams` 벡터를 사용한다. 핵심 값은 다음과 같이 정리된다.

- `x`: Dissolve 종류. 투명도 방식은 `1`.
- `z`: Dissolve 범위. 진행도 제어의 핵심 값.
- `w`: 블러/경계 흐림 값.

해당 사례는 기본값을 `(1, 0, -1, 0.1)`, 목표값을 `(1, 0, 1, 0.1)`로 두고 `z` 값을 시간에 따라 보간한다.

참고:
- https://kurokumasoft.com/2024/07/28/liltoon-dissolve-effect/

### 로컬 패키지 확인

현재 프로젝트는 `Packages/manifest.json`에서 `jp.lilxyzw.liltoon`을 GitHub master 경로로 참조한다.

로컬 PackageCache 확인 결과:
- 패키지 위치: `Library/PackageCache/jp.lilxyzw.liltoon@96d62c9cdbdb`
- 기본 셰이더 `lts.shader`에 `_DissolveMask`, `_DissolveNoiseMask`, `_DissolveNoiseStrength`, `_DissolveColor`, `_DissolveParams`, `_DissolvePos`가 존재한다.
- `_DissolveParams` 기본값은 `(0,0,0.5,0.1)`이다.
- lilToon의 주 텍스처는 `_MainTex`가 메인 프로퍼티이고, `_BaseMap`은 호환용 숨김 프로퍼티로 존재한다.

## 현재 코드 상태

대상 파일:
- `Assets/02.Scripts/GameActor/Component/Common/DissolveController.cs`

현재 구조:
- `RendererInfo`는 `Renderer`, `_BaseMap` 텍스처, 원본 `sharedMaterials`를 저장한다.
- `Awake`에서 Addressables `DissolveMaterial`을 로드한다.
- `StartDissolve`는 로드 완료를 기다린 뒤 `SwapToDissolveMaterials`를 호출한다.
- `SwapToDissolveMaterials`는 렌더러 슬롯 수만큼 `DissolveMaterial` 인스턴스를 만들고 `_BaseMap`만 복사한다.
- `SetDissolveAmount`는 렌더러 단위 `MaterialPropertyBlock`에 `_DissolveAmount`를 설정한다.

문제:
- lilToon 원본 머티리얼 설정을 버린다.
- `_BaseMap`만 복사하므로 lilToon의 `_MainTex`, 색, 알파, 아웃라인, 림, 발광, 마스크, 렌더링 모드 같은 설정이 유지되지 않는다.
- 렌더러 단위 MPB는 여러 머티리얼 슬롯을 같은 값으로 제어할 때는 편하지만, 원본 머티리얼 인스턴스별 초기 `_DissolveParams` 복원에는 불리하다.
- Addressables `DissolveMaterial` 로드 실패 시 즉시 파괴하는 폴백은 lilToon 대상에서는 불필요하게 강하다.

## 설계 방향

### 핵심 방침

lilToon 머티리얼은 교체하지 않는다. 원본 `sharedMaterials`를 기반으로 런타임 전용 인스턴스를 만들고, 각 인스턴스의 `_DissolveParams`를 직접 제어한다.

기존 커스텀 디졸브 머티리얼 경로는 비-lilToon 폴백으로 남긴다. 프로젝트 안에 lilToon이 아닌 외부 VFX/무기/임시 모델이 섞여 있으므로 전면 제거는 위험하다.

### 감지 기준

머티리얼별로 다음 순서로 처리 방식을 결정한다.

1. 머티리얼이 null이면 제외.
2. 셰이더명이 `Particle`을 포함하면 기존처럼 제외.
3. `material.HasProperty(_DissolveParams)`이면 lilToon 내장 Dissolve 경로.
4. 그렇지 않으면 기존 `DissolveMaterial` 교체 경로.

셰이더 이름 문자열로 `lilToon`만 검사하지 않는 이유는 lilToon이 `Hidden/lilToonCutout`, `Hidden/lilToonTransparent`, `Hidden/lilToonMulti` 같은 변형 셰이더를 사용할 수 있기 때문이다. 실제 필요한 기능은 `_DissolveParams` 지원 여부다.

### 데이터 구조 변경안

`RendererInfo`는 렌더러 단위 원본 복원 데이터를 유지하고, 슬롯별 머티리얼 정보를 추가한다.

예상 구조:

```csharp
private struct RendererInfo
{
    public Renderer renderer;
    public Material[] originalSharedMaterials;
    public MaterialSlotInfo[] slots;
}

private struct MaterialSlotInfo
{
    public Material originalMaterial;
    public Texture baseMap;
    public Texture mainTex;
    public bool supportsLilToonDissolve;
    public Vector4 originalDissolveParams;
}
```

런타임에서 생성한 머티리얼은 `_instancedMaterials`에 계속 모아 `OnDestroy`, `ResetDissolve`, `RefreshRenderers`에서 해제한다.

## 구현 단계

### 1단계: 프로퍼티 ID 추가

추가할 ID:

```csharp
private static readonly int LilDissolveParamsID = Shader.PropertyToID("_DissolveParams");
private static readonly int LilDissolveColorID = Shader.PropertyToID("_DissolveColor");
private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
```

기존 ID:

```csharp
private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
```

### 2단계: 옵션 노출

컨트롤러에 lilToon 제어값을 직렬화 필드로 둔다.

```csharp
[SerializeField] private bool _useLilToonDissolve = true;
[SerializeField] private float _lilToonStartRange = -1f;
[SerializeField] private float _lilToonEndRange = 1f;
[SerializeField] private float _lilToonBlur = 0.1f;
[SerializeField] private Color _lilToonDissolveColor = Color.white;
```

초기값은 조사 사례와 맞춰 `z = -1`에서 완전 표시, `z = 1`에서 소거 완료로 둔다. 단, 일부 머티리얼에서 완전 소거 임계값이 다를 수 있으므로 인스펙터에서 조절 가능하게 둔다.

### 3단계: 렌더러 초기화 개선

`InitializeRendererData`에서 `r.sharedMaterial` 하나만 보지 말고 `r.sharedMaterials` 전체 슬롯을 순회한다.

각 슬롯마다:
- `_MainTex`가 있으면 `mainTex` 저장.
- `_BaseMap`이 있으면 `baseMap` 저장.
- `_DissolveParams`가 있으면 `supportsLilToonDissolve = true`, 원본 벡터 저장.

렌더러 제외 여부는 “모든 슬롯이 null 또는 Particle 계열”일 때 제외하는 식으로 바꾼다. 현재처럼 `r.sharedMaterial` 첫 슬롯만 보고 제외하면 멀티 슬롯 모델에서 일부 슬롯이 누락될 수 있다.

### 4단계: 머티리얼 준비 분기

기존 `SwapToDissolveMaterials`를 다음 책임으로 재구성한다.

- 메서드명 후보: `PrepareDissolveMaterials`
- 각 슬롯이 lilToon Dissolve 지원이면 원본 머티리얼을 `new Material(original)`로 복제한다.
- 복제 직후 `_DissolveParams`를 `(1, 0, _lilToonStartRange, _lilToonBlur)`로 초기화한다.
- `_DissolveColor`가 있으면 `_lilToonDissolveColor`를 설정한다.
- 비-lilToon 슬롯은 기존처럼 `_dissolveSourceMaterial` 인스턴스를 사용하고, `_MainTex` 또는 `_BaseMap`을 복사한다.

중요한 차이:
- lilToon 슬롯이 하나라도 있으면 Addressables 로드 대기가 없어도 진행 가능하다.
- 비-lilToon 슬롯이 존재할 때만 `DissolveMaterial` 로드 대기가 필요하다.

### 5단계: 진행도 설정 분기

`SetDissolveAmount(float amount)`는 슬롯별 머티리얼 직접 제어로 바꾼다.

lilToon:

```csharp
float range = Mathf.Lerp(_lilToonStartRange, _lilToonEndRange, amount);
material.SetVector(LilDissolveParamsID, new Vector4(1f, 0f, range, _lilToonBlur));
```

기존 커스텀 디졸브:

```csharp
material.SetFloat(DissolveAmountID, amount);
```

`MaterialPropertyBlock`은 커스텀 셰이더 경로에서 유지할 수도 있지만, 통일성과 슬롯별 제어를 위해 인스턴스 머티리얼 직접 설정으로 바꾸는 편이 단순하다. 이미 액터마다 머티리얼 인스턴스를 생성하는 구조라 공유 머티리얼 오염 위험도 없다.

### 6단계: Reset/Refresh 복원

`ResetDissolve`:
- 코루틴 중지.
- 각 렌더러 `enabled = true`.
- `renderer.sharedMaterials = originalSharedMaterials`.
- 생성한 인스턴스 머티리얼 파괴.

기존 복원 방식은 유지한다. lilToon 경로도 원본 `sharedMaterials`로 되돌아가므로 `_DissolveParams` 원본값을 다시 개별로 써줄 필요는 없다.

`RefreshRenderers`:
- 인스턴스 머티리얼 파괴.
- 렌더러 재수집.
- Addressables 로드 상태는 건드리지 않는다.

### 7단계: CompleteDissolve 처리

`CompleteDissolve`는 준비가 안 된 경우에도 준비 후 즉시 `amount = 1`을 적용한다.

주의점:
- 모든 슬롯이 lilToon이면 Addressables 실패 여부와 무관하게 완료 가능해야 한다.
- 비-lilToon 슬롯이 있는데 `DissolveMaterial`이 로드되지 않았다면 기존처럼 즉시 파괴 또는 경고 후 파괴를 유지할 수 있다.

## 권장 코드 구조

최소 침습으로는 기존 public API를 유지하고 private 메서드만 분리한다.

```csharp
private bool HasFallbackDissolveSlots()
private bool IsLilToonDissolveMaterial(Material material)
private void PrepareDissolveMaterials()
private Material CreateLilToonDissolveInstance(Material source)
private Material CreateFallbackDissolveInstance(MaterialSlotInfo slot)
private void SetDissolveAmount(float amount)
```

`DissolveRoutine`의 대기 로직은 다음처럼 바꾼다.

```csharp
bool needsFallbackMaterial = HasFallbackDissolveSlots();
while (needsFallbackMaterial && _dissolveSourceMaterial == null)
{
    ...
}
```

## 검증 계획

Unity 에디터에서 다음 케이스를 확인한다.

1. lilToon 캐릭터 사망 디졸브
   - 원본 색, 그림자, 림, 아웃라인이 유지되는지 확인.
   - 디졸브가 `duration` 동안 자연스럽게 진행되는지 확인.
   - 완료 후 `destroyOnComplete`가 정상 동작하는지 확인.

2. 멀티 머티리얼 캐릭터
   - 몸, 머리, 의상, 무기 등 모든 슬롯이 같은 진행도로 사라지는지 확인.
   - 첫 슬롯만 처리되는 문제가 없는지 확인.

3. 비-lilToon 모델
   - 기존 `DissolveMaterial` 폴백이 여전히 동작하는지 확인.
   - `_MainTex`만 가진 머티리얼과 `_BaseMap`만 가진 머티리얼 모두 텍스처가 유지되는지 확인.

4. Reset/Refresh
   - 내장 무기 복원 또는 모델 교체 시 원본 머티리얼로 돌아오는지 확인.
   - 반복 호출 시 머티리얼 인스턴스 누수나 누적 생성이 없는지 Unity Profiler/Memory로 확인.

5. 로드 실패 폴백
   - Addressables `DissolveMaterial` 로드가 실패해도 lilToon-only 모델은 디졸브가 진행되는지 확인.
   - 비-lilToon 슬롯이 있는 모델은 기존 경고/즉시 파괴 정책이 유지되는지 확인.

## 리스크와 대응

- lilToon 머티리얼이 불투명 모드일 때 Dissolve가 기대대로 보이지 않을 수 있다.
  - 대응: 대상 캐릭터 머티리얼은 컷아웃 또는 반투명 모드에서 Dissolve 설정을 확인한다.

- `_DissolveParams.x = 1`이 투명도 방식이라는 구현 사례에 의존한다.
  - 대응: 로컬 패키지와 에디터 Inspector에서 대상 모델의 Dissolve 모드를 한번 수동 확인한다. 필요하면 `x` 값을 직렬화 필드로 노출한다.

- `MaterialPropertyBlock`으로 `_DissolveParams`가 모든 lilToon 변형에서 안정적으로 적용되는지 확정하지 않는다.
  - 대응: 런타임 인스턴스 머티리얼 직접 설정을 기본으로 한다.

- lilToon 패키지가 GitHub master를 참조하므로 향후 업데이트에서 내부 프로퍼티 의미가 바뀔 가능성이 있다.
  - 대응: 프로퍼티명은 `HasProperty`로 방어하고, 문서에 로컬 확인 패키지 해시를 남긴다.

## 최종 권장안

`DissolveController`는 다음 형태로 수정한다.

1. lilToon Dissolve 지원 머티리얼은 원본 복제 후 `_DissolveParams.z`를 보간한다.
2. 비-lilToon 머티리얼만 기존 Addressables `DissolveMaterial` 폴백을 사용한다.
3. 렌더러 첫 머티리얼이 아니라 모든 머티리얼 슬롯을 수집한다.
4. 기존 public API와 호출 흐름은 유지한다.
5. Addressables 로드 실패는 lilToon-only 모델에는 영향을 주지 않게 한다.

이 방식이 프로젝트에 가장 덜 위험하다. 캐릭터 외형 품질을 유지하면서, 기존 사망/복원/모델 교체 호출부를 흔들지 않고 적용할 수 있다.
