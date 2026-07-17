# 사운드 시스템 에디터 작업

## 현재 확인된 상태

- `SoundManager` 기반 2D/3D 재생, 풀링, 거리 감쇠, BGM 크로스페이드와 기존 시스템 연결 코드는 구현되어 있다.
- 프로젝트에 `SoundDatabase.asset`이 없다.
- 프로젝트에 `.mixer` 에셋이 없다.
- Addressables에 `SoundDatabase`, `AudioMixer` 주소가 등록되어 있지 않다.
- VitalOrb 3종의 `collectSoundName`이 모두 비어 있다.

현재 상태에서는 직접 전달된 클립은 기본 출력으로 재생될 수 있지만, key 기반 재생과 설정 볼륨/버스 라우팅은 완성되지 않는다.

## 1. AudioMixer 생성

권장 경로:

`Assets/10.Datas/Audio/UPlaygroundAudioMixer.mixer`

- [ ] AudioMixer 에셋 생성
- [ ] 그룹 이름을 정확히 다음과 같이 구성

```text
Master
├── BGM
├── SFX
├── UI
├── Voice
└── Ambience
```

`SoundManager`는 그룹 이름으로 자동 검색하므로 대소문자가 정확히 일치해야 한다.

- [ ] Master 그룹 Volume을 `MasterVolume`으로 Expose
- [ ] BGM 그룹 Volume을 `BGMVolume`으로 Expose
- [ ] SFX 그룹 Volume을 `SFXVolume`으로 Expose
- [ ] Voice 그룹 Volume을 `VoiceVolume`으로 Expose
- [ ] UI를 별도 설정값으로 제어하지 않을 경우 SFX 하위로 두거나 SFX 볼륨 정책과 일치시킴
- [ ] Ambience의 볼륨 정책 결정

코드는 위 네 exposed parameter를 문자열로 호출한다. 철자나 대소문자가 다르면 설정 슬라이더가 실제 음량에 반영되지 않는다.

## 2. AudioMixer Addressables 등록

- [ ] Addressables Groups 창 열기
- [ ] 생성한 Mixer를 적절한 데이터 그룹에 등록
- [ ] Address를 정확히 `AudioMixer`로 설정
- [ ] 중복 주소가 없는지 확인

씬에 `SoundManager`를 직접 배치하거나 믹서 그룹을 수동 할당할 필요는 없다. `GameManager`가 매니저를 생성하고 Addressables에서 Mixer를 로드한다.

## 3. SoundDatabase 생성

프로젝트 창에서:

`Create → UPlayGround → Audio → Sound Database`

권장 경로:

`Assets/10.Datas/Audio/SoundDatabase.asset`

- [ ] `SoundDatabase` 에셋 생성
- [ ] Addressables에 등록
- [ ] Address를 정확히 `SoundDatabase`로 설정
- [ ] 중복 주소가 없는지 확인

## 4. 필수 사운드 키와 클립 등록

최소 등록 항목:

| key | 권장 Bus | 권장 거리 | 용도 |
|---|---|---|---|
| `footstep_default` | SFX | Logarithmic3D | 기본 발자국 |
| `LevelUp` 또는 실제 호출부와 일치하는 키 | UI | None2D | 레벨업 피드백 |
| VitalOrb별 `collectSoundName` | UI | None2D | 오브 획득음 |

- [ ] 키 문자열의 실제 코드/데이터 값과 대소문자 일치
- [ ] 모든 엔트리에 AudioClip 할당
- [ ] UI 사운드는 `None2D`, `preCullByMaxDistance` 끔
- [ ] 월드 SFX는 `Logarithmic3D` 또는 `Linear3D` 설정
- [ ] 3D SFX의 `minDistance < maxDistance` 확인
- [ ] 발자국과 다단 히트에 적절한 `cooldown`, `maxSimultaneous` 설정
- [ ] 피치 변주가 필요한 사운드는 `pitchMin <= pitchMax` 범위 지정

현재 VitalOrb 데이터:

- `VitalOrbObjectData_BattleChip.asset`
- `VitalOrbObjectData_GuardShard.asset`
- `VitalOrbObjectData_SoulOrb.asset`

각 에셋의 `collectSoundName`에 SoundDatabase의 실제 키를 입력한다. 같은 클립을 쓸 경우 같은 키를 사용해도 된다.

`LevelUp`의 정확한 키는 호출부와 데이터가 일치해야 한다. TODO 계획서의 `LevelUp`과 예시 코드의 `level_up` 표기가 다르므로, 플레이 검증 전에 하나로 통일한다.

## 5. 데이터 검증

`SoundDatabase.asset` 인스펙터에서:

- [ ] `Validate Sound Database` 실행
- [ ] 중복 key 0개
- [ ] 빈 key 0개
- [ ] null clip 0개
- [ ] 잘못된 거리/피치/쿨다운/동시 재생 제한 0개
- [ ] `Custom3D` 엔트리의 curve 누락 0개
- [ ] `None2D` 엔트리의 거리 프리컷 경고 0개

## 6. MotionEvent 및 콘텐츠 연결 확인

- [ ] 기존 MotionSet의 `PlaySound` 이벤트에 AudioClip이 연결되어 있는지 확인
- [ ] 3D 사운드는 `is3D` 활성화
- [ ] UI/전역 사운드는 2D 설정
- [ ] 공격 시작음과 실제 타격음을 구분해 헛스윙에서 타격음이 나지 않도록 확인
- [ ] 필요한 BGM key를 SoundDatabase에 추가
- [ ] 씬 진입 또는 게임 흐름에서 `PlayBgm` 호출이 존재하는지 확인

## 7. 플레이 모드 검증

부팅 로그:

- [ ] `[SoundManager] SoundDatabase 로드 완료`
- [ ] `[SoundManager] AudioMixer 로드 및 그룹 매핑 완료`
- [ ] Addressable 로드 실패/누락 key 경고 없음

기능:

- [ ] Master/BGM/SFX/Voice 설정 슬라이더가 실제 출력에 반영됨
- [ ] UI 사운드는 거리와 관계없이 들림
- [ ] 3D SFX는 발생 위치와 거리에 따라 음량이 변함
- [ ] `maxDistance` 밖 사운드는 재생되지 않음
- [ ] MotionEvent의 2D/3D 옵션이 구분됨
- [ ] 발자국이 중복 폭주하지 않음
- [ ] VitalOrb 3종 획득음이 재생됨
- [ ] 레벨업음이 재생됨
- [ ] BGM 교체 시 A/B 크로스페이드가 자연스러움
- [ ] 일시정지 중 UI/BGM 정책이 의도대로 동작함

성능:

- [ ] Audio Profiler에서 반복 SFX 시 AudioSource GameObject가 무한 증가하지 않음
- [ ] 반복 재생 중 GC Alloc이 과도하지 않음
- [ ] 씬 전환 후 active sound가 정리됨

## 완료 판정

`SoundDatabase`와 `AudioMixer`가 각각 정확한 Addressables 주소로 로드되고, 필수 키·클립·믹서 파라미터를 구성한 뒤 플레이 모드 검증을 통과해야 한다.
