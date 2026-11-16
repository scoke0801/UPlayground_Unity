# Sol 웹 조사·리드 재구성

2026-11-15. 사용자 지시대로 `gpt-6.1-sol`에게 웹 조사를 위임했다. Sol은 파일·Unity를 변경하지 않았고 리드가 결과를 받아 설계를 작성했다. **조사와 동작 재설계 반영, 신규 모션 제작·실행 적용 미착수.** 이전 품질 개선을 궁극기 미술 인수 완료로 보지 않는다.

정본: [궁극기 모션 재설계](../../../Assets/docs/design/combat/player/ULTIMATE_MOTION_REDESIGN.md). [기존 수정 기록](QUALITY_REVISION.md) · [전투 위키](../../wiki/combat.md).

## 조사 범위와 신뢰도

Sol은 공식 영상·공식 전투 소개·제작사 발표문을 조사했다. YouTube web.open 실패와 브라우저 `No browser is available`로 실제 영상 시청은 못 했다. 따라서 프레임·초·카메라 경로·손발 위치를 관찰한 조사로 취급하지 않는다. 리드도 핵심 원문을 열어 교차 확인했으나 YouTube는 오류/본문 없음, 명조 페이지는 읽을 수 있는 본문이 없었다. 영상 링크 확보와 동작 검증은 다르다.

### 텍스트로 확인한 참고

| 사례·출처 | 확인 범위 | 적용 판단 |
|---|---|---|
| [명조 공식 CBT 전투 소개](https://wutheringwaves.kurogames.com/en/main/news/detail/393) | Sol이 검색 결과에서 기염의 궁극기 후 대검→창 전환 설명 확보. 2023 CBT 자료이며 리드 직접 열기에서는 본문 미노출 | 무기 운용 자체의 차별화 참고. 현행 스킬·수치·동작 프레임의 검증 근거로 쓰지 않음 |
| [Kuro Crimson Weave 보도자료](https://www.gamespress.com/pl/Punishing-Gray-Ravens-New-Patch-Wintry-Shackles-Unveiled) | 리드 원문 확인. 에너지 축적과 Nightblaze 발도 설명. 궁극기 전용이라는 명시는 없음 | 축적→발동의 인과 참고. 궁극기 영상 분석으로 분류하지 않음 |
| [miHoYo 발표문 수록: 미야비](https://www.gematsu.com/2024/12/zenless-zone-zero-version-1-4-update-a-storm-of-falling-stars-launches-december-18) | 빠른 베기·전방 강공격·납도 설명은 강화공격 설명 | 마무리 행동 참고. 궁극기로 오인하지 않음 |

### 확보한 후속 시청 목록 — 아직 동작 검증 근거 아님

| 게임·캐릭터 | 링크 | 검증할 부분 |
|---|---|---|
| 원신 라이덴 | [공식 데모](https://www.youtube.com/watch?v=mvrW4aKwAXw) | 평소 무기와 발동 무기, 첫 일격 구별 |
| 스타레일 아케론 | [공식 해설](https://www.youtube.com/watch?v=Y9vz34wximk) | 발도 전후·연속타·마지막 일격의 위계 |
| 젠레스 미야비 | [전투 정보](https://www.youtube.com/watch?v=HY4gBFsGpkI) | 궁극기와 강화공격 식별, 준비와 잔심 |
| 퍼니싱 Crimson Weave | [공식 PV](https://www.youtube.com/watch?v=0FuSe6bXVkE) | 축적과 발도, 실제 궁극기 구간 식별 |
| 원신 타르탈리아 | [공식 데모](https://www.youtube.com/watch?v=3f2-EANyrNM) | 쌍검/활 동작과 궁극기 구별 |
| 원신 이토 | [공식 데모](https://www.youtube.com/watch?v=iXeJ94AFZDw) | 무기 전환·전신 중량 사용 |
| 원신 감우 | [공식 데모](https://www.youtube.com/watch?v=FHRXP8ceeBU) | 사격과 별도 소환 행동 구별 |
| 원신 느비예트 | [공식 데모](https://www.youtube.com/watch?v=v2QSNQXRSkE) | 빔은 강공격 참고. 궁극기와 혼동 금지 |
| 퍼니싱 Liv: Empyrea | [공식 PV](https://www.youtube.com/watch?v=ZKkj_FsudNc) | 전신 개방/수렴과 보조기 관계 |
| 명조 기염 | [공식 소개](https://wutheringwaves.kurogames.com/en/main/news/detail/393) | 무기 전환 후 찌르기/휩쓸기 운동 |
| 명조 금희 | [쇼케이스 후보](https://www.youtube.com/watch?v=0caRWqAQFMc) | 궁극기와 강화 스킬 식별. 외부 게시물에서 확보, 채널 직접 검증 미수행 |
| 스타레일 비소 | [공식 해설 게시물](https://www.hoyolab.com/article/32879577) | 쌍무기 역할·최종 일격. 턴제 공간 통제는 이식 제외 |

채찍·검방패 직접 대응 사례는 충분히 확보하지 못했다. 표의 링크만으로 12사례 분석 완료를 주장하지 않는다. 타 작품 동작·FX를 그대로 복제하지 않는다.

## 리드 재구성 결과

현재 문제는 모션 길이나 카메라 강도만이 아니다. 일반 공격용 움직임을 짧은 고정 표에 넣고 부족한 준비·전신 전달·마무리를 FX로 덮은 제작 순서를 바꾼다. 이 판단은 사용자 반복 피드백과 현행 코드/제작 스크립트 조사에 기반한 리드의 해석이다.

새 설계는 12명 모두 준비 자세→전신 축적→발동→대표 순간→관성 회수를 갖게 한다. 화린은 연속 회전과 양도끼 내려찍기, 레이네는 전신 투척과 착지, 세라는 무게를 받는 수직 강타를 대표 3종으로 먼저 저작한다. 이 셋을 FX 없이 연속 재생해 판정하기 전 나머지를 대량 생성하지 않는다. 아린/라온·세라/릴리·연화/묘묘도 준비/마무리 동작을 분화한다. 자세·순서·실패 기준·보존 계약은 정본에 기록했다.

## 수행·미수행

- 수행: Sol 조사 보고 수신, 핵심 원문 교차 조회, 현행 프레임표·모션 제작 스크립트 검토, 12명 신규 모션 제작 기준 작성, 기존 문서 우선순위 정리.
- 미수행: 실제 레퍼런스 동영상 재생, 신규 Blender/Unity 클립 제작, 새 타이밍 판정 저작, 전투 테스트. 문서만 바뀐 단계이므로 이전17/17 테스트를 새 모션 검증으로 승계하지 않는다.
- 다음 작업: 레퍼런스 실제 시청 가능한 환경에서 후보를 검증하고 화린/레이네/세라 전신 모션 블록아웃을 저작. 아직 이 기준을 만족한 신규 모션은 없다.
- 보존: 사용자·다른 작업의 미커밋 C#/에셋·씬 변경은 건드리지 않았다. 이번 단계 신규 임시 대용량 산출물 없음. 커밋·빌드 없음.
