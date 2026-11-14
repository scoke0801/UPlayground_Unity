# P0 얼굴 이미지 제작 기록

2026-11-14 기본 내장 ImageGen으로 신규 생성 후 배경 알파 추출 편집을 각 1회 수행했다. 요청서의 Codex CLI ImageGen은 이미지 생성 수단으로 해석했고 직접 API/키는 사용하지 않았다. 상태는 **잠정 채택, 사용자 스타일 확인 대기**다.

| 캐릭터 | 파일 | 크기/용량 | SHA256 |
|---|---|---|---|
| Raon | [PNG](../../../Assets/04.Images/SkillCutIn/CutIn_Raon.png) | 1536×1024 RGBA / 1,801,451 bytes | 26E7332BEE6BCF1C7EE54388737CDC70954995D27D84D77B9FF99872F6DA6A87 |
| Hwarin | [PNG](../../../Assets/04.Images/SkillCutIn/CutIn_Hwarin.png) | 1536×1024 RGBA / 1,830,041 bytes | A9358ADCF212D63781484EE97DE8DC852E8F4FEA54337D708328558638D76476 |

PNG는 Git ignore 대상이므로 `.meta`와 함께 추적 밖 아트로 보존한다. 기존 파일은 덮어쓰지 않았다. 생성 원본/편집본은 `C:/Users/scoke/.codex/generated_images/01a1d8a7-3583-7473-84fe-4ba9cbca1202/`에 보존한다.

## 공통 스타일 프롬프트

```text
Use case: stylized-concept
Asset type: transparent PNG ultimate-skill face cut-in for a game UI.
Create ONE new polished anime action-game bust illustration, preserving identity from supplied character references. New artwork, not a portrait crop.
Style: restrained thin dark outlines, crisp cel shading with softly blended secondary shadows, detailed readable eyes, controlled fabric highlights.
Composition: landscape 1536x1024, centered head-to-upper-chest, complete hair silhouette and ornaments, transparent margin. Slight three-quarter turn toward screen right, eyes toward viewer and slightly down, strong determined gaze, closed mouth.
Lighting: neutral soft upper-left key, restrained cool highlights ON character only.
Background: genuinely transparent alpha. No panel, text, border, hands, weapon, watermark, scenery, haze, aura or exterior glow.
UI will scale the bust beyond the short panel; do not bake a panel into the artwork.
```

## 개별 외형 프롬프트

Raon은 `Player_Raon_Full.png` 외형 정본과 `Screenshots/Raon.png` 보조 참조를 사용했다.

```text
Raon: youthful male East Asian fantasy swordsman; pale skin, short tousled charcoal-black hair, long center bangs partly crossing face, small curved ahoge, muted reddish-brown eyes. Long green-jade and brown tassel earring on right ear, dark green jade bead necklace with pale jade teardrop pendant. Black high-collared coat, muted burgundy shoulder lapels, lavender-gray edging, fine gold piping, burgundy inner tunic, small gold knotted frog fasteners. Focused calm combat readiness. Preserve identity, jewelry and clothing colors. No Hwarin features.
```

Hwarin은 `Player_Hwarin_Full.png`, `dialogue/Portraits/Hwarin/Hwarin_Serious.png` 외형 정본과 생성한 Raon의 스타일을 참조했다. 실제 `PlayerModel_Hwarin`의 `Honoka/ho_hair_alt1.mat`, `ho_face_alt4.mat`와 텍스처도 직접 확인했다. 구형 `Screenshots/Hwarin`/`Hwarin_Dialogue_Portrait`의 백발·붉은 피부·청색 외투와 혼용하지 않는다.

```text
Hwarin: young adult female East Asian fantasy oni warrior, light warm skin, ruby-red eyes. Short charcoal-black hair with red tips, straight bangs, small high bun with red accents. Two complete upward tapered warm red-orange forehead horns. Black choker with small gold geometric motif, silver chain and single silver magatama pendant. Black hooded short coat, dark red lining, red cord knots, small circular silver tomoe medallions at shoulders. Red inner kimono lapels over black top with small pale flower detail. Fierce determined gaze. Use current BLACK-HAIRED LIGHT-SKINNED version, never obsolete white-haired red-skinned alternate. Match Raon illustration finish only, never his appearance or clothing.
```

## 알파 보정 프롬프트

```text
Use case: background-extraction
Extract only the supplied finished character onto real transparent alpha. Remove all exterior gray/purple/red glow, smoky haze, ambient halo, bloom and shadow. All pixels outside hair, horns, ear, jewelry, skin and clothing must be alpha zero, retaining natural contour antialiasing. Preserve face, gaze, pose, clothing, ornaments, colors, lighting ON character, composition and exact 1536x1024 crop. No redraw, panel or text.
```

## 점검

아티스트가 외형 참조/최종 이미지를 확인했고 리드도 최종 2종을 열어 확인했다. 8픽셀 간격 24,576점 알파 표본: Raon alpha0=15,705, partial=8,871 / Hwarin alpha0=14,426, partial=10,150. 외부 지정 6점 모두 alpha0, 내부 최고 alpha254다. 썸네일이 투명 영역 RGB 광무리를 보여줄 수 있어 Unity 합성 외곽은 별도 확인한다. 이는 미술 인수나 최종 외형 승인과 다르다. 나머지 캐릭터는 스타일 승인 전 생성하지 않는다.
