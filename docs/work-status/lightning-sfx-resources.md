# 번개·천둥 SFX 리소스 조사 및 확보

확인일: 2026-11-13. 범위: 웹 조사, 무료 원본 확보, Unity 임포트 확인. 게임 실행 데이터 연결과 최종 청감 검수는 포함하지 않았다.

## 확보한 파일

Unity 사용 폴더: [Lightning](../../Assets/06.Sounds/Environment/Lightning). 원본·배포 페이지 증거: [ArtSource/Audio/Lightning](../../ArtSource/Audio/Lightning).

| 파일 | 실제 길이 | 형식 | 후보 용도 | 출처 |
|---|---:|---|---|---|
| BSB_Thunder_3113.wav | 5.03초 | WAV, 모노, 48kHz/24bit | 짧은 낙뢰의 첫 후보 | [Thunder #2](https://bigsoundbank.com/thunder-2-s3113.html) |
| BSB_Thunder_3114.wav | 11.20초 | WAV, 모노, 48kHz/24bit | 중간 길이 천둥 변주 | [Thunder #3](https://bigsoundbank.com/thunder-3-s3114.html) |
| BSB_Thunder_3115.wav | 17.20초 | WAV, 모노, 48kHz/24bit | 긴 천둥 여운 후보 | [Thunder #4](https://bigsoundbank.com/thunder-4-s3115.html) |
| BSB_Thunder_3179.wav | 13.76초 | WAV, 모노, 48kHz/24bit | 긴 천둥 변주 | [Thunder #6](https://bigsoundbank.com/thunder-6-s3179.html) |
| Rubberduck_Thunder_01.ogg | 5.26초 | OGG, 스테레오, 48kHz | 짧은 천둥 대안 | [100 CC0 SFX #2](https://opengameart.org/content/100-cc0-sfx-2) |

BigSoundBank 4종은 배포자가 비가 없는 단발 천둥으로 설명한 음원이다. 후보 용도는 길이와 배포 설명에 따른 제안이며 직접 청취·믹싱 품질 판정은 아니다. 웹 표기 길이는 반올림될 수 있으므로 위 표는 Unity가 실제 로드한 길이를 사용했다.

## 라이선스와 원본

- 확보한 5종 모두 CC0. 상업 게임 사용·편집 가능, 의무 크레딧 없음. [BigSoundBank 공식 이용 조건](https://bigsoundbank.com/licenses.html), [CC0](https://creativecommons.org/publicdomain/zero/1.0/).
- BigSoundBank: Joseph SARDIN / BigSoundBank.com. 개별 원본 페이지의 공동 저자 표기도 Evidence HTML에 보존했다. 선택적 크레딧: `Additional sounds: Joseph SARDIN - BigSoundBank.com`.
- OGA: rubberduck. `sfx_100_v2.zip`에서 `sfx100v2_thunder_01.ogg`만 선택하고 파일명만 변경했다. 오디오 변환·편집 없음. 선택적 크레딧: `Thunder SFX: rubberduck / OpenGameArt.org (CC0)`.
- WAV는 개별 공식 페이지 JSON-LD의 `contentUrl`에 명시된 BWF/WAV 원본을 받았다. 일반 다운로드 POST는 음원 대신 다운로드 안내 HTML을 반환해 파일 헤더 검사에서 걸러졌고, 같은 작업의 안내 사본은 실제 WAV로 교체했다.
- 원본 WAV 4개와 ZIP 1개, 공식 배포·라이선스 HTML 6개, SHA-256 목록을 [원본 목록](../../ArtSource/Audio/Lightning/README.md)에 보관한다. ZIP 전체를 Assets에 풀지 않았다.

## 조사한 대안

| 출처 | 조건·판단 |
|---|---|
| [Thunder / Jerimee](https://opengameart.org/content/thunder) | CC-BY 3.0. 합성 천둥. 저자·출처·라이선스·편집 여부 표기가 필요해 이번 CC0 묶음에는 미포함 |
| [Ice & Electricity Magic / qubodup](https://opengameart.org/content/ice-electricity-magic) | CC-BY 3.0. 전기 마법·방전 질감 추가 후보. 실제 확보·청취하지 않았음 |
| [Electric Sound Effects Library](https://opengameart.org/content/electric-sound-effects-library) | CC-BY 3.0. 전기 whoosh·합성음 위주이며 자연 천둥과 용도가 다름. 미확보 |
| [Sonniss GDC](https://gdc.sonniss.com/gdc-game-audio-bundle/) | 대규모 무료 게임 오디오 후보. 개별 천둥 파일과 현재 적용 EULA를 확정하지 않아 이번에는 미확보 |

## 검증과 남은 확인

- 수행: WAV RIFF 헤더 4/4, ZIP 선택 추출, Unity 동기 임포트 및 AudioClip 로드 5/5, 길이·채널·샘플레이트·GUID 조회, meta 생성 확인, 원본과 사용본 SHA-256 대조.
- 미수행: 직접 청취, 게임 내 재생·거리 감쇠·믹서 밸런스·반복 피로 검수. 게임 데이터 연결과 재생 중 클리핑 검증도 미수행.
- 사용자 테스트 대기: Unity Project에서 Lightning 폴더의 5개 클립을 선택해 Preview 재생. 찢어짐·갑작스러운 꼬리 잘림·어색한 배경 잡음이 없는지 확인하고, 선호 파일명을 선택한다.
- 연결 시 제안: 먼저 3113의 짧은 천둥과 나머지 긴 천둥을 비교한다. 실제 게임에서는 낙뢰 거리별 재생 지연과 중복 천둥의 음량 누적을 별도로 조정한다. 이번 작업에서 수치나 SO는 변경하지 않았다.
- 부산물: 안내 HTML 가짜 WAV는 정상 원본으로 교체. 나머지는 출처 증명과 재추출을 위한 원본이므로 보존. 신규 테스트 스크립트·빌드·임시 압축 해제 폴더 없음.

[분야 위키](../wiki/combat.md)
