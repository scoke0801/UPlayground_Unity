# ADR 0002 — G1 백엔드 증명과 의존성 고정

- 상태: 채택 (2026-11-12) · 실행 환경: Unity 6000.3.21f1 (Mono 6.13, Editor), Windows 11 22631, i5-12400F x64

## 고정한 의존성 (Editor 전용, `Packages/com.uplayground.hotreload/Editor/ThirdParty/Harmony/`)

| 파일 | 출처(nuget.org) | SHA-256 |
| --- | --- | --- |
| 0Harmony.dll | Lib.Harmony.Thin 2.3.6 `lib/net48` (MIT, commit f41da7608c5c) | 965a55183dcfa53c5f43b77a981b4e2f1cbfa4502ab8449c3e7bd988014d3484 |
| MonoMod.Core.dll | MonoMod.Core 1.2.3 `lib/netstandard2.0` | 3c69434356918a345a0feb3966d7d5db1b0b070828863d5dd714752d9691e304 |
| MonoMod.Iced.dll | MonoMod.Core 1.2.3 `lib/netstandard2.0` | 753034d149940c776bae61d3c1cf5046ca7c41283e4423cba9390b196588832b |
| MonoMod.Utils.dll | MonoMod.Utils 25.0.8 `lib/netstandard2.0` | 44ba300f42f2f81081efc731ba67276c2f6751b29f8b0be4b7ff190183b19f86 |
| MonoMod.Backports.dll | MonoMod.Backports 1.1.2 `lib/netstandard2.1` | b3fc7aac52ffe6579ae1009a285a3d892b167151b0e18b5c1066ccb88e8b44a4 |
| MonoMod.ILHelpers.dll | MonoMod.ILHelpers 1.1.0 `lib/netstandard2.1` | a13590452f2bdf72718f43b410f43e110f273fce57fabd58ac5043a59b9be049 |

Mono.Cecil 0.11.5는 반입하지 않고 프로젝트 기존 `com.unity.nuget.mono-cecil` 1.11.6(어셈블리 0.11.5.0)을 런타임에 사용한다. 모든 plugin meta는 `Any: 0 / Editor: 1 / isExplicitlyReferenced: 1`.

## 재현한 실패와 해결

1. 병합본 `Lib.Harmony` 2.3.6 → Burst `Failed to find entry-points … Error while hashing type reference '1000134' in assembly '0Harmony.dll' … Read out of bounds`. → Thin 빌드로 교체 후 재발 없음.
2. Thin + MonoMod net452 → `Unable to resolve reference 'MonoMod.Iced'` → 같은 패키지의 Iced 추가.
3. MonoMod.Backports net452가 `System.Span` 등을 자체 정의 → 로드된 모든 어셈블리를 참조하는 CodeDom 컴파일(MCP execute_code)이 `CS1685`로 실패. → netstandard2.0/2.1 빌드(BCL로 type-forward)로 교체 후 해소. 의존성 변경 뒤 G1 재실행 7/7 통과.

## Harmony 예외 블록 규약(실측)

`PatchProcessor.GetOriginalInstructions`로 fixture `Guarded`를 읽어 확인: try 첫 명령에 `BeginExceptionBlock`, handler 첫 명령에 `BeginCatchBlock(type)`/`BeginFinallyBlock`, 마지막 handler의 마지막 명령(원본 `leave`/`endfinally` 유지)에 `EndExceptionBlock`. 같은 try를 공유하는 handler는 한 블록. `PreparedBody.AttachExceptionBlocks`가 이 규약을 따른다.

## G1 결과

`G1BackendProofTests` 7/7 PASS(2회, 의존성 교체 전후). 증거: [evidence/G1/probe-result.json](../evidence/G1/probe-result.json).
T01 private 필드 연속(2→12→원복 13), T02 static 저장소 공유·cctor 1회, T03 Vector3/float/bool/string/참조/property 반환, T04 overload·nested·private helper, T08 캐시 delegate 5회 교체+hook 1개, Play Mode에서 Unity가 직접 호출하는 `Update`와 private helper 새 본문·InstanceID 유지·원복, 실패한 교체는 직전 본문 유지.
