# 설정 메뉴 시스템 가이드

## 개요

설정 메뉴는 `SettingsData` ScriptableObject를 중심으로 동작한다. 런타임에서는 `SettingsManager`가 Addressable 키 `SettingsData`로 데이터를 로드하고, `PlayerPrefs`에 저장된 값을 덮어쓴 뒤 `SettingsApplier`를 통해 Unity 시스템에 반영한다.

UI는 `UI_Scene_SettingMenu`가 `SettingsManager.Data`를 각 설정 페이지에 전달하고, 페이지 스크립트가 자식 컨트롤을 자동 수집해 `SettingsData`에 값을 기록한다.

## 주요 파일

- `Assets/02.Scripts/Data/Config/SettingsData.cs`
  - 설정값 저장용 ScriptableObject.
  - `Save()`, `Load()`, `ResetToDefault()` 제공.
  - PlayerPrefs 키: `GameSettings_v1`.
- `Assets/02.Scripts/Manager/SettingsManager.cs`
  - Addressable `SettingsData` 로드.
  - 시작 시 저장값 로드 후 전체 적용.
- `Assets/02.Scripts/Manager/SettingsApplier.cs`
  - 그래픽/오디오 설정을 실제 Unity 시스템에 반영.
- `Assets/02.Scripts/UI/Scene/UI_Scene_SettingMenu.cs`
  - 설정 메뉴 루트 UI.
  - Apply/Cancel/Reset 처리.
  - 각 설정 페이지 바인딩 호출.
- `Assets/02.Scripts/UI/Scene/SettingPage/UISettingPageGamePlay.cs`
- `Assets/02.Scripts/UI/Scene/SettingPage/UISettingPageGraphic.cs`
- `Assets/02.Scripts/UI/Scene/SettingPage/UISettingPageAudio.cs`
- `Assets/02.Scripts/UI/Scene/SettingPage/UISettingPageKeyBinding.cs`

## 현재 구현된 기능

### 게임플레이

- 수평 감도 저장 및 인게임 카메라 반영: `SettingsData.sensitivityX`
- 수직 감도 저장 및 인게임 카메라 반영: `SettingsData.sensitivityY`
- Y축 반전 저장 및 인게임 카메라 반영: `SettingsData.invertY`
- 화면 흔들림 저장: `SettingsData.screenShake`
- 타겟 보정 저장: `SettingsData.aimAssist`
- 언어 인덱스 저장: `SettingsData.languageIndex`

카메라 감도는 `InGameCameraBehavior`에서 Look 입력에 적용된다. 기본값 5는 기존 회전 속도와 동일하며, 내부 배율은 `감도 / 5`다. 예를 들어 10은 기존의 2배, 1은 기존의 0.2배다.

화면 흔들림, 타겟 보정, 언어는 현재 데이터 저장까지만 연결되어 있다. 전투/카메라 쉐이크/언어 시스템에 실제로 반영하는 코드는 별도 작업이 필요하다.

### 그래픽

- 해상도 선택 저장 및 적용
  - `1920x1080`
  - `1280x720`
  - `2560x1440`
- 창 모드 저장 및 적용
  - `전체화면` -> `FullScreenMode.ExclusiveFullScreen`
  - `경계없는 창` -> `FullScreenMode.FullScreenWindow`
  - `창 화면` -> `FullScreenMode.Windowed`
- FPS 저장 및 적용
  - `Application.targetFrameRate`
  - 적용 시 `QualitySettings.vSyncCount = 0`
- 품질 인덱스 적용
  - `QualitySettings.SetQualityLevel`
- 밝기 저장

밝기는 현재 저장만 된다. 실제 화면 밝기 변경은 URP Volume, Color Adjustments, 노출 보정 등 별도 렌더링 파이프라인 연결이 필요하다.

### 오디오

- 마스터 볼륨 저장 및 적용
- BGM 볼륨 저장 및 적용
- SFX 볼륨 저장 및 적용

`SettingsApplier.ApplyAudio()`는 AudioMixer 파라미터에 dB 값으로 변환해 반영한다.

필요한 AudioMixer exposed parameter 이름:

- `MasterVolume`
- `BGMVolume`
- `SFXVolume`
- `VoiceVolume`

현재 UI 스크린샷 기준으로 오디오 페이지에는 마스터/BGM/SFX 슬라이더만 연결되어 있다. Voice 슬라이더가 추가되면 `UISettingPageAudio`에 4번째 슬라이더 매핑을 추가해야 한다.

## 에디터 체크리스트

### SettingsData 확인

`Assets/10.Datas/System/SettingsData.asset`을 선택해 다음 필드가 있는지 확인한다.

- `Window Mode Index`
- `Target Frame Rate`
- `Brightness`

기본값 기준:

- `Resolution Index`: 0
- `Window Mode Index`: 1
- `Fullscreen`: true
- `Quality Index`: 2
- `Target Frame Rate`: 60
- `Brightness`: 5

### UI_Scene_SettingMenu 프리팹 확인

`Assets/03.Prefabs/UI/Scene/UI_Scene_SettingMenu.prefab`에서 `UI_Scene_SettingMenu` 컴포넌트의 참조가 유지되어 있는지 확인한다.

- `_panelGameplay`
- `_panelGraphics`
- `_panelAudio`
- `_panelKeys`
- `_btnGamePlay`
- `_btnGraphic`
- `_btnAudio`
- `_btnKeyBinding`
- `_btnApply`
- `_btnCancel`
- `_btnReset`
- `_audioMixer`

오디오 적용을 위해 `_audioMixer`에는 실제 게임에서 사용하는 AudioMixer를 연결해야 한다.

### 컨트롤 순서 주의

현재 페이지 스크립트는 각 패널의 자식 컨트롤 순서로 자동 매핑한다. 프리팹에서 컨트롤 순서를 바꾸면 잘못된 설정값에 연결될 수 있다.

기대 순서:

- 게임플레이 슬라이더
  - 0: 수평 감도
  - 1: 수직 감도
- 게임플레이 스위치
  - 0: Y축 반전
  - 1: 화면 흔들림
  - 2: 타겟 보정
- 게임플레이 드롭다운
  - 0: 게임 언어
- 그래픽 드롭다운
  - 0: 해상도
  - 1: 창 모드
- 그래픽 슬라이더
  - 0: FPS
  - 1: 화면 밝기
- 오디오 슬라이더
  - 0: 마스터
  - 1: 배경음악
  - 2: 효과음

컨트롤 순서 의존을 없애려면 각 페이지 스크립트에 명시적 `[SerializeField]` 참조를 추가하고 프리팹에서 직접 연결하는 방식으로 리팩터링한다.

## Play Mode 테스트

1. 설정 메뉴를 연다.
2. 게임플레이, 그래픽, 오디오 탭을 각각 열어 값이 현재 저장값과 동기화되는지 확인한다.
3. 값을 변경하고 `적용`을 누른다.
4. 메뉴를 다시 열었을 때 변경값이 유지되는지 확인한다.
5. `취소`를 눌렀을 때 메뉴를 열기 전 값으로 돌아가는지 확인한다.
6. `초기화`를 눌렀을 때 기본값으로 UI가 갱신되는지 확인한다.
7. 그래픽 탭에서 해상도/창 모드/FPS가 실제 적용되는지 확인한다.
8. 오디오 탭에서 AudioMixer 볼륨이 실제로 변하는지 확인한다.

## 미구현 및 후속 작업

- 화면 흔들림 실제 반영
  - 카메라 쉐이크 실행 지점에서 `screenShake`가 false면 스킵하거나 강도를 0으로 처리해야 한다.
- 타겟 보정 실제 반영
  - 락온/조준 보정 로직에서 `aimAssist`를 읽어 활성 여부를 결정해야 한다.
- 밝기 실제 반영
  - URP Volume 또는 별도 화면 보정 시스템과 연결해야 한다.
- 언어 실제 반영
  - Localization 시스템이 확정된 뒤 `languageIndex`를 Locale 변경으로 연결해야 한다.
- 키 설정
  - Unity Input System 리바인딩 UI, 저장, 로드 구조가 필요하다.

## 검증

`dotnet build UPlayground.sln --no-restore` 기준 컴파일 오류는 없다.

현재 남는 경고는 기존 Unity 패키지 참조 충돌 및 외부 에셋 경고다.
