# 사운드 시스템 / AudioMixer 셋업 가이드

`SoundManager`가 사운드 재생·풀링·버스 라우팅을 담당합니다. 이 문서는 **에디터에서 직접 해야 하는 작업**만 정리합니다. 코드 작업은 이미 완료되어 있습니다.

---

## 📌 동작 개요 (코드는 손댈 필요 없음)

- `SoundManager`는 `GameManager`에 매니저로 등록되어 자동 초기화됩니다.
- `AudioMixer`와 6개 버스 그룹(`Master / BGM / SFX / UI / Voice / Ambience`)을 **두 가지 방식 중 하나**로 가져옵니다.
  1. **(권장) Addressable 자동 로드** — `SoundManager`가 `"AudioMixer"` 키로 믹서를 로드하고, 그룹을 **이름으로 자동 매핑**합니다. 씬에 매니저를 배치하지 않아도 동작합니다.
  2. **직접 할당** — `SoundManager` 컴포넌트 인스펙터에 그룹을 직접 드래그하면 그 값이 **우선**되고 Addressable 로드는 건너뜁니다.
- 볼륨 슬라이더 적용(`SettingsApplier.ApplyAudio`)은 `SettingsManager`가 담당하며, 자기 믹서가 비어 있으면 `SoundManager`가 로드한 믹서를 **폴백으로 자동 사용**합니다. 믹서가 늦게 로드돼도 로드 완료 시점에 저장된 볼륨이 **자동 재적용**됩니다.

> 즉, **권장 경로(Addressable)** 만 따르면 매니저를 씬에 배치하지 않아도 라우팅과 볼륨 제어가 모두 동작합니다.

---

## ✅ 에디터에서 해야 할 일 (체크리스트)

### 1. AudioMixer 에셋 생성
- Project 창에서 **우클릭 → Create → Audio Mixer** (예: `Assets/10.Datas/Sound/MainAudioMixer.mixer`)

### 2. 믹서 그룹 구성 (이름이 정확히 일치해야 함)
- **Audio Mixer** 창에서 `Master` 아래에 다음 그룹들을 추가합니다.
- 그룹 이름은 코드의 `SoundBusType` enum 이름과 **대소문자까지 정확히 일치**해야 합니다:

| 그룹 이름 | 용도 |
|-----------|------|
| `Master`  | 최상위 (기본 생성됨) |
| `BGM`     | 배경음악 |
| `SFX`     | 효과음 |
| `UI`      | UI 사운드 |
| `Voice`   | 음성/대사 |
| `Ambience`| 환경음 |

> 코드는 `AudioMixer.FindMatchingGroups(name)`로 **정확한 이름 매칭**을 우선 선택합니다. 이름이 다르면 해당 버스만 매핑 실패(=기본 출력으로 재생)합니다.

### 3. 볼륨 파라미터 노출 (Exposed Parameters)
- 각 그룹의 **Volume**을 우클릭 → **Expose '...' to script** 한 뒤, 우상단 **Exposed Parameters** 드롭다운에서 이름을 다음으로 변경:

| 노출 파라미터 이름 | 대상 그룹 Volume |
|--------------------|------------------|
| `MasterVolume`     | Master |
| `BGMVolume`        | BGM |
| `SFXVolume`        | SFX |
| `VoiceVolume`      | Voice |

> 이 이름들은 `SettingsApplier.ApplyAudio`가 `mixer.SetFloat(...)`로 직접 참조합니다. 이름이 틀리면 볼륨 슬라이더가 동작하지 않습니다.
> (현재 코드 기준 UI/Ambience 볼륨 슬라이더는 없으므로 노출 파라미터도 불필요합니다. 추후 추가 시 `SettingsApplier`도 같이 수정.)

### 4. 믹서를 Addressable로 등록 (권장 경로)
- 믹서 에셋 선택 → Inspector에서 **Addressable** 체크
- 주소(Address)를 정확히 **`AudioMixer`** 로 지정 (`SoundManager.AudioMixerKey` 상수와 일치)

### 5. SoundDatabase 준비 (key 기반 재생용)
- `SoundDatabaseSO` 에셋을 만들고 사운드 엔트리(key, clip, bus, 거리 설정 등)를 등록
- 이 에셋을 Addressable 주소 **`SoundDatabase`** 로 등록 (`SoundManager.SoundDatabaseKey`와 일치)
- key 없이 `AudioClip`을 직접 넘기는 재생(`PlayClip`)은 DB 없이도 동작합니다.

### 6. 믹서 그룹을 직접 할당하려는 경우 (선택 — Addressable 대신)
- 씬/프리팹에 `SoundManager` 컴포넌트를 배치하고 인스펙터의 **Mixer Groups** 항목에 6개 그룹(또는 일부)을 드래그
- 하나라도 직접 할당되어 있으면 Addressable 로드는 건너뜁니다.
- 이 경우 `Audio Mixer` 필드도 함께 할당하는 것을 권장(미할당 시 그룹에서 역참조로 채움).

---

## 🔧 코드 측 키/이름 참조 표 (확인용)

| 항목 | 상수 위치 | 값 |
|------|-----------|-----|
| 믹서 Addressable 키 | `SoundManager.AudioMixerKey` | `"AudioMixer"` |
| 사운드 DB Addressable 키 | `SoundManager.SoundDatabaseKey` | `"SoundDatabase"` |
| 설정 데이터 Addressable 키 | `SettingsManager.SETTINGS_DATA_KEY` | `"SettingsData"` |
| 버스 그룹 이름 | `SoundBusType` enum | `Master / BGM / SFX / UI / Voice / Ambience` |
| 노출 볼륨 파라미터 | `SettingsApplier.ApplyAudio` | `MasterVolume / BGMVolume / SFXVolume / VoiceVolume` |

---

## ❓ 문제 해결

| 증상 | 원인 / 확인 |
|------|-------------|
| 콘솔에 `'AudioMixer' AudioMixer를 찾을 수 없습니다` | 믹서가 Addressable로 등록 안 됐거나 주소가 `AudioMixer`가 아님 → 4단계 확인 |
| 소리는 나는데 버스 볼륨이 안 먹음 | 그룹 이름 불일치(2단계) 또는 노출 파라미터 이름 불일치(3단계) |
| 볼륨 슬라이더가 전혀 반영 안 됨 | 노출 파라미터 이름 확인(3단계), 믹서 자체가 없으면 `ApplyAudio`가 스킵됨 |
| `사운드 key를 찾을 수 없습니다` 경고 | `SoundDatabaseSO`에 해당 key 엔트리 미등록(5단계) |
| 특정 버스만 무음/기본 출력 | 그 그룹 이름만 오타 → `FindMatchingGroups` 매칭 실패 |
