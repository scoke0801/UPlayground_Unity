# 생명의 호수 제1장 스토리 일러스트 제작 프롬프트

> 기준 문서: `LAKE_OF_LIFE_CHAPTER1_STORY_FLOW_REVISION_SPEC.md`  
> 상태: 제작 요청안  
> 범위: 기존 오프닝·조사 이미지를 제외한 제1장 진행용 신규 일러스트

## 1. 결론

기존 오프닝 배경과 라온·아린 전경은 재사용한다. 붉은 천과 거대한 흔적도 기존 조사 이미지가 있으므로 다시 만들지 않는다.

신규 제작 후보는 총 **7장**이다.

| 우선순위 | 장면 | 수량 | 서사 기능 |
|---|---|---:|---|
| P1 | Scene 17 신전 첫 공개 | 1 | 오프닝에서 보았던 목적지가 실제 위험 공간으로 바뀌었음을 회수 |
| P1 | Scene 19 최근 사용 흔적 | 1 | 누군가 먼저 신전을 사용했다는 정보를 설명 없이 전달 |
| P0 | Scene 20 라온 경로의 다른 가능성 등장 | 1 | 라온과 닮았지만 달라진 존재를 즉시 인식 |
| P0 | Scene 20 아린 경로의 Nenmir 등장 | 1 | 외형 닮음 대신 제단 반응과 시선으로 아린과의 관계를 전달 |
| P0 | Scene 22 라온 경로의 소멸 | 1 | `아직 아니야` 뒤 보물이 다시 드러나는 여운 전달 |
| P0 | Scene 22 아린 경로의 소멸 | 1 | Nenmir가 아린만 바라보고 사라지는 관계 전달 |
| P1 | Scene 23 제1장 종료 | 1 | 같은 호수와 신전을 달라진 의미로 다시 보여 주며 종료 |

P0 네 장은 최종 보스 경로별 핵심 정보를 전달하므로 필수다. P1 세 장은 실제 3D 환경과 카메라가 같은 기능을 충분히 수행하면 인게임 연출로 대체할 수 있다.

## 2. 기존 자산과 참조 기준

### 신규 제작하지 않는 자산

- 오프닝 배경: `Assets/04.Images/UI/Story/LakeOfLife/Story_Lake_Intro_Field.png`
- 아린 오프닝 전경: `Assets/04.Images/UI/Story/LakeOfLife/Story_Lake_Intro_Arin_Back3Q.png`
- 라온 오프닝 전경: `Assets/04.Images/UI/Story/LakeOfLife/Story_Lake_Intro_Raon_Back3Q.png`
- 붉은 천 조사 이미지: `Assets/04.Images/UI/dialogue/img_dlg_hint_lian.png`
- 거대한 흔적 조사 이미지: `Assets/04.Images/UI/dialogue/img_dlg_hint_track.png`

### 최종 보스 경로 참조

- 라온: 오프닝 라온 전경과 실제 라온 모델을 기준으로 한다.
- 라온의 다른 가능성: `Assets/04.Images/UI/Portrait/MonsterRaon_Dialogue_Portrait.png` 및 실제 보스 모델을 함께 제공한다.
- 아린: 오프닝 아린 전경과 실제 아린 모델을 기준으로 한다.
- 아린 경로의 상대: 현재 데이터상 `Nenmir`에 대응하는 실제 캐릭터 모델·활·의상 참조를 제공한다. 다른 캐릭터를 임의로 섞지 않는다.
- 보물: 구슬·낡은 거울·봉인된 결정 중 최종 선택된 실제 신물 참조를 제공한 뒤 Scene 20·22를 제작한다.

## 3. 공통 제작 사양

- 화면비: 16:9, 최소 2048×1152, 최종 출력은 UI 안전 영역을 확인한다.
- 스타일: 고품질 스타일라이즈드 동아시아 판타지 액션 RPG 시네마틱 일러스트.
- 인물 외형과 의상은 제공된 프로젝트 참조를 그대로 유지한다. 프롬프트만으로 재설계하지 않는다.
- 글자, 대사, 지역명, 로고, 프레임, UI를 이미지에 넣지 않는다.
- 하단 22%는 대사 UI가 올라와도 핵심 얼굴·손·보물·행동이 가려지지 않게 한다.
- 보물의 정체와 작동 원리를 설명하는 문양, 환영, 글, 상징을 새로 만들지 않는다.
- 감정은 표정 과장이 아니라 거리, 시선, 손의 위치, 빛과 정적으로 전달한다.

## 4. Scene 17 — 신전 첫 공개

### 제작 목적

오프닝의 아름다운 목적지를 더 가까운 거리에서 다시 보여 주되, 생물 소리가 사라진 듯한 정적과 신전 주변의 파손으로 분위기가 달라졌음을 전달한다. 수호자를 미리 전신으로 공개하지 않는다.

### 프롬프트

```text
Using the supplied Lake of Life opening background as a strict location and art-direction reference, create a new cinematic establishing shot from much closer to the shrine. The forest path has just opened onto the lakeshore. Show the calm lake, the colossal crimson-leaf tree, and the ancient East-Asian shrine together in one readable composition. The same place that looked inviting in the opening must now feel unnaturally quiet.

Add restrained physical evidence of recent danger near the shrine approach: several trees pushed in one direction, deep broad disturbances in wet soil, a few cracked stone railings, thin ground mist, and birds conspicuously absent from the sky. Keep the shrine intact enough to remain beautiful and explorable. Suggest that something massive passed through, but do not show the guardian itself. Use cooler, lower-saturation light than the opening, with faint warm light still touching the crimson canopy. Premium stylized fantasy action RPG cinematic illustration, painterly environment, strong atmospheric depth, clear navigational geography.

No characters, no guardian body or silhouette, no treasure, no alternate self, no giant crystal, no portal, no apocalyptic destruction, no horror gore, no text, no logo, no UI, no border, no watermark.
```

## 5. Scene 19 — 최근 사용 흔적

### 제작 목적

오래된 신전에 누군가 최근까지 있었다는 사실만 보여 준다. 사용자의 정체, 의식의 목적, 보물의 작동 방식은 밝히지 않는다.

### 프롬프트

```text
Create a square investigation illustration matching the supplied Lake of Life clue-image style: a close view of an old stone preparation altar inside an East-Asian fantasy shrine. Most surfaces are covered with undisturbed age-old dust, but one hand-width area was wiped clean recently. Show a newly shifted small offering bowl, a thin fresh scrape across old dust, slightly warm gray ash in a shallow burner, and one recent footprint stopping just outside the altar edge. The evidence must clearly say “someone used this place recently” without identifying who or why.

Use restrained dark teal, weathered stone gray, faded crimson lacquer, and a faint indirect warm glow from outside the frame. Painterly game investigation art with irregular vignette edges and a transparent outer background, consistent with existing red-cloth and track clue illustrations.

No person, no hand, no readable writing, no blood, no ritual circle, no cult symbol, no treasure, no magical diagram, no explicit answer, no text, no logo, no UI, no border, no watermark.
```

## 6. Scene 20 — 라온 경로의 다른 가능성 등장

### 제작 목적

라온과 즉시 닮았다고 느끼지만 완전히 같은 인물은 아니라는 70% 동일 / 30% 변형 원칙을 한 컷에서 읽힌다.

### 프롬프트

```text
Using the supplied Raon, alternate-Raon, shrine interior, and confirmed relic references as strict production references, create a cinematic 16:9 story illustration inside the shrine’s central altar chamber. Raon stands in the near foreground in back three-quarter view, slightly left of center, facing the altar. His companions remain farther behind as subdued, out-of-focus silhouettes. A small mysterious relic rests on the central stone altar, never a giant gemstone.

On the far side of the altar, alternate Raon has just stepped out of pale, unstable light. He must read at first glance as the same person’s other possibility: the same face structure, body proportions, resting posture, and katana handling, with only two to four controlled changes taken from the approved alternate-Raon reference, such as silver hair, altered eye color, restrained teal-violet costume accents, and unfamiliar afterimage energy. He looks only at Raon and draws his katana in the same practiced manner. Preserve a tense empty gap between them. The companions should not compete for attention.

Use oppressive quiet, muted shrine colors, a narrow pool of cold altar light, and subtle reflections in polished stone. The image should create immediate recognition followed by discomfort, without explaining the phenomenon. Premium stylized fantasy action RPG cinematic illustration.

No dialogue text, no floating explanation symbols, no evil grin, no demonic horns, no mirror portal, no literal reflection surface unless it is the confirmed relic, no copied combat effects, no active attack, no giant treasure, no logo, no UI, no border, no watermark.
```

## 7. Scene 20 — 아린 경로의 Nenmir 등장

### 제작 목적

Nenmir가 아린과 닮았다고 거짓말하지 않는다. 제단의 반응, Nenmir의 시선, 두 사람에게만 이어지는 빛과 자세로 `아린에게서 나온 가능성`이라는 관계를 전달한다.

### 프롬프트

```text
Using the supplied Arin, Nenmir, shrine interior, Nenmir bow, and confirmed relic references as strict production references, create a cinematic 16:9 story illustration inside the shrine’s central altar chamber. Arin stands in the near foreground in back three-quarter view, slightly left of center, facing the altar. Her companions remain behind her as subdued, out-of-focus silhouettes. A small mysterious relic rests on the central stone altar, never a giant gemstone.

On the far side of the altar, Nenmir has just stepped out of pale, unstable light with her established face, silhouette, clothing, and bow preserved exactly. Do not make her physically resemble Arin. Instead, make the relationship readable through staging: Nenmir ignores every companion and fixes her gaze only on Arin; the altar light reacts toward Arin first and then continues as a restrained matching eye-and-afterimage color around Nenmir; both hold a quiet, poised stillness across the same axis of the altar. Nenmir begins to ready her bow without attacking yet. The visual connection must feel specific but unexplained.

Use oppressive quiet, muted shrine colors, a narrow pool of cold altar light, and subtle reflections in polished stone. The image should make the player ask why Nenmir is responding only to Arin, not falsely conclude that they have the same face. Premium stylized fantasy action RPG cinematic illustration.

No face morphing, no merged bodies, no familial resemblance invented by the artist, no dialogue text, no floating explanation symbols, no evil grin, no demonic features, no mirror portal, no active projectile, no giant treasure, no logo, no UI, no border, no watermark.
```

## 8. Scene 22 — 라온 경로의 소멸

### 제작 목적

다른 가능성의 라온이 처음이자 마지막으로 라온을 직접 바라본 뒤 사라지고, 그 뒤에 보물이 여전히 남아 있음을 보여 준다.

### 프롬프트

```text
Using the approved Raon, alternate-Raon, shrine altar, and confirmed relic references, create a quiet cinematic 16:9 aftermath illustration. Alternate Raon is down on one knee in front of the central altar, weapon lowered and no longer threatening. He looks directly at Raon with restrained certainty, not pain, rage, or pleading. The first fine particles of pale light are lifting from the edge of his body, while most of his face and posture remain clearly readable.

Place Raon at the near edge of the composition, seen partly from behind, stopped rather than advancing. As alternate Raon dissolves, the small relic behind him becomes visible again on the altar. The relic remains silent and unexplained. Use large areas of still darkness, a narrow cold light between both figures, and a very subtle surviving pulse from the relic. The emotional center is the held gaze and the unfinished meaning, not victory spectacle.

No written dialogue, no smile, no tears, no corpse, no gore, no explosion, no triumphant party pose, no loot shower, no giant gemstone, no answer encoded in the light, no logo, no UI, no border, no watermark.
```

## 9. Scene 22 — 아린 경로의 소멸

### 제작 목적

Nenmir가 마지막 순간에도 동료가 아니라 아린만 바라본다는 사실로 둘의 관계를 남긴다.

### 프롬프트

```text
Using the approved Arin, Nenmir, shrine altar, Nenmir bow, and confirmed relic references, create a quiet cinematic 16:9 aftermath illustration. Nenmir is down on one knee in front of the central altar, her bow lowered beside her and no longer threatening. She looks directly and exclusively at Arin with restrained certainty, not pain, rage, affection, or pleading. The first fine particles of pale altar-colored light are lifting from the edge of her body, while most of her face and established appearance remain clearly readable.

Place Arin at the near edge of the composition, seen partly from behind, stopped rather than advancing. Keep the companions distant and visually secondary; Nenmir’s gaze must not drift toward them. As Nenmir dissolves, the small relic behind her becomes visible again on the altar. The relic remains silent and unexplained. Use large areas of still darkness, a narrow cold light linking Arin, Nenmir, and the altar, and a very subtle surviving pulse from the relic. The emotional center is the unanswered connection, not victory spectacle.

No written dialogue, no smile, no tears, no corpse, no gore, no explosion, no romantic framing, no triumphant party pose, no loot shower, no giant gemstone, no answer encoded in the light, no logo, no UI, no border, no watermark.
```

## 10. Scene 23 — 제1장 종료

### 제작 목적

오프닝과 같은 장소를 다시 보여 주되, 위험은 줄었어도 질문은 남았다는 감각으로 닫는다.

### 프롬프트

```text
Using the supplied Lake of Life opening background as a strict location and composition reference, create a matching cinematic 16:9 closing image from nearly the same lakeshore viewpoint. The colossal crimson tree, lake, and shrine must occupy recognizably similar positions so the player immediately understands that this is the same place seen at the beginning.

Change the meaning rather than the geography. The lake surface is calmer and clearer, some natural color has returned to the shore, and the heavy mist has thinned. The shrine remains standing in the distance. However, one extremely faint, restrained pulse of pale light still survives deep inside it, small enough to be questioned rather than announced. Use cool post-storm air with a narrow warm break in the clouds, quiet reflections, and a sense that the immediate danger passed but the central mystery did not.

No characters, no celebration, no restored festival, no treasure visible, no guardian, no alternate self, no portal, no magical beam, no text, no chapter title, no logo, no UI, no border, no watermark.
```

## 11. 제작하지 않을 장면

- 화린·리안 구조: 플레이어가 직접 개입하는 전투와 전투 후 대화가 핵심이므로 정지 삽화로 대체하지 않는다.
- 묘령 대치와 합류: 전투 스타일과 패배 후 태도 변화가 행동으로 보여야 한다.
- 수호자 등장과 격파: 실제 모델의 크기, 흔적, 텔레그래프가 일치해야 하므로 인게임 카메라와 전투 연출이 우선이다.
- 제1장 종료 카드: 기존 UI 폰트와 전환 규칙으로 제작하며 배경 이미지에 글자를 굽지 않는다.

## 12. 최종 검수

- 라온 경로는 첫눈에 `라온과 닮았다`고 읽힌다.
- 아린 경로는 Nenmir가 아린과 닮아 보이지 않으며, 시선과 제단 반응으로만 관계가 읽힌다.
- 두 경로 모두 보물의 정체나 작동 원리를 설명하지 않는다.
- Scene 22는 승리의 쾌감보다 `아직 끝나지 않았다`는 여운이 먼저 남는다.
- Scene 23은 오프닝과 같은 장소임을 알아볼 수 있지만 완전히 안전해졌다고 보이지 않는다.
- 모든 이미지의 핵심 정보가 대사 UI에 가려지지 않는다.
