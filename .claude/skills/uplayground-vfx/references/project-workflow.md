# 프로젝트 연결과 도구 사용

아래 경로는 저장소 루트 기준이다. 설치된 스킬 폴더에서 상대 경로로 찾지 않는다. 2026-11-14의 소스를 바탕으로 한 탐색 진입점이며 호출 전 현재 구현을 다시 확인한다.

## 기존 연결 경로

| 대상 | 먼저 읽을 파일 | 주의점 |
|---|---|---|
| 일반 파티클 이벤트 | `Assets/02.Scripts/GameActor/Animation/MotionEvents/MotionEvent_Particle.cs` | `BeginParticleEvent`의 소켓·부착·분리·수명·종료 처리와 기존 이벤트를 확인한다 |
| 참격 이벤트 | `Assets/02.Scripts/GameActor/Animation/MotionEvents/MotionEvent_SlashVFX.cs` | `SlashVFXEvent`의 직접 prefab과 스포너 우선순위, 좌표계, `attachToActor`를 확인한다 |
| 참격 스폰·프리셋 | `Assets/02.Scripts/GameActor/Particle/WeaponSlashVfxSpawner.cs`, `Assets/02.Scripts/Data/Event/Animation/SlashVFXPresetSO.cs` | 소켓 탐색·칼날 Base/Tip·시드·설정 소비 경로를 확인한다. 프리셋 필드가 있다는 이유만으로 실제 연결됐다고 가정하지 않는다 |
| 무기 궤적 | `Assets/02.Scripts/GameActor/Component/Common/ActorWeaponTrailController.cs`, `Assets/02.Scripts/Data/Combat/ElementalWeaponTrailLibrarySO.cs` | 기존 속성별 프리팹 매핑을 사용하고 새 TrailRenderer를 중복 생성하지 않는다 |
| 입자 풀 | `Assets/02.Scripts/Manager/Object/GameObjectManager.Particles.cs` | `IParticleEffectService.RentParticles/ReturnParticles`의 상한·자연 소멸·즉시 clear 계약을 확인한다. 모든 기존 FX가 이 풀을 쓴다고 가정하지 않는다 |

`SlashVFXEvent`의 `World`는 액터 루트 회전 기준이다. 절대 월드 고정으로 오해하지 않는다. 칼날 포즈를 읽는 참격과 스냅샷 생성은 본 평가 뒤 실행되는 기존 `RequiresPostEvaluation` 계약을 유지한다. 미리보기 정렬만 맞고 실제 회전/루트모션에서 뒤집히는 경우를 확인한다.

Katana FX를 다루면 `docs/work-status/player-katana-fx.md`를 읽는다. 실제 장착 무기의 최종 MotionSet/오버라이드를 추적하며, 동일 모션에 이미 연결된 `BeginParticleEvent`/참격을 확인한다. 과거 scale·시작 시간은 새 모션의 정답이 아니다. 보류 중인 기존 오류를 신규 FX 작업의 통과로 덮지 않는다.

## MCP 호출

우선 현재 도구 스키마를 읽는다. Coplay `manage_vfx`는 최상위 `action`, `target`, `search_method`, 선택적 `component_index`와 **액션별 `properties` 객체**를 받는다. 같은 이름의 객체가 여럿이면 ID나 검증된 계층 경로로 지정한다.

현재 패키지 소스는 `Packages/manifest.json`에서 버전을 확인한 뒤 `Library/PackageCache/com.coplaydev.unity-mcp@*/Editor/Tools/Vfx/`에서 찾는다. `ManageVFX.cs`는 액션 라우팅, `ParticleWrite.cs`/`ParticleControl.cs`는 파라미터와 부작용의 근거다. 캐시는 읽기 전용으로 조사한다.

기존 대상에 대한 호출 형태 예시:

```json
{"action":"particle_get_info","target":"<확인한 인스턴스 ID>","search_method":"by_id"}
```

```json
{"action":"particle_set_main","target":"<확인한 인스턴스 ID>","search_method":"by_id","properties":{"looping":false,"playOnAwake":false,"startLifetime":0.25,"maxParticles":32}}
```

두 번째 예시의 값은 API 형태 설명용이며 제작 기본 수치가 아니다. `particle_set_main`은 머티리얼 자동 할당도 할 수 있으므로 명시적 재질을 확인하고 저장 diff를 검사한다. `particle_create`는 없는 대상을 새 루트 객체로 만들 수 있어 경로 문자열만으로 원하는 부모 밑에 생성된다고 가정하지 않는다. 대상 생성·계층 설정 후 ID로 편집하는 편이 안전하다.

지원 액션 예: `particle_set_emission`, `particle_set_shape`, `particle_add_burst`, `particle_set_color_over_lifetime`, `particle_set_size_over_lifetime`, `particle_set_renderer`, `trail_get_info`, `trail_set_properties`, `trail_clear`. 미리보기 후 `particle_stop`/`particle_clear` 등으로 이번 작업의 객체만 정리한다. 배열/곡선/색 인자는 해당 액션 구현을 읽고 구성한다.

VFX Graph는 `vfx_list_templates`, `vfx_create_asset`, `vfx_assign_asset`, `vfx_get_info`, `vfx_set_float`, `vfx_send_event` 등 템플릿·노출값·재생 경로를 제공한다. `UNITY_VFX_GRAPH` 컴파일 조건 때문에 패키지가 설치됐어도 도구가 미지원 응답을 낼 수 있다. 응답의 성공 여부와 원인을 확인하며 무조건 패키지를 재설치하거나 define을 변경하지 않는다. 노드 추가/링크 API를 이 액션군에 있다고 가정하지 않는다.

## 촬영·측정의 한계

현재 사용 가능한 `manage_scene` 촬영 스키마나 기존 프로젝트 캡처 도구를 먼저 확인한다. 없는 screenshot 액션·Scenario의 `ut_vfx`·AgentKit 함수를 호출하지 않는다. 작은 편집 API 보조가 실제로 필요하면 코드 저작/Hot Reload 지침을 적용하고, 지속 사용할 때만 저장소 도구로 남긴다.

고정 시드·같은 시간/카메라는 비교에 유용하지만 Shader의 시간, TrailRenderer의 위치 이력, VFX Graph의 GPU 갱신은 별도로 확인해야 한다. `aliveParticleCount`는 지연/갱신 시점의 영향이 있어 이벤트를 보낸 같은 프레임의 값만으로 실패를 단정하지 않는다. 캡처를 만들지 못했으면 미수행 이유와 사용자 확인 절차를 남긴다.
