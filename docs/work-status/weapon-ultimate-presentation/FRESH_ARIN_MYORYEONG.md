# 아린·묘령 컷인 신규 작화 (2026-11-14)

이 문서는 사용자 화풍 피드백으로 대체된 이전 버전 기록이다. 현재 적용본과 아린 사용자 통과·묘령 전투감 후속은 [최신 상태](ARIN_APPROVED_MYORYEONG_BATTLE.md)를 따른다.

[전투 위키](../../wiki/combat.md) · [궁극기 상태](STATUS.md)

사용자 요청 범위는 `CutIn_Arin`, `CutIn_MyoRyeong` 두 장이다. 다른 플레이어블 제작은 사용자 완료 선언을 따르며 H09는 제외한다. 기존 컷인 위 덧칠/눈 부분 수정은 번짐 피드백으로 폐기하고, **원본 전신 초상화만 입력해 내장 ImageGen으로 신규 작화**했다.

## 반영

- [아린 PNG](../../../Assets/04.Images/SkillCutIn/CutIn_Arin.png): 정면의 같은 목표를 보는 양눈, 상체를 세운 발도 준비, 깨끗한 선과 셀 명암. 참조는 `Assets/04.Images/UI/Portrait/Player_Arin_Portrait.png` 한 장이다.
- [묘령 PNG](../../../Assets/04.Images/SkillCutIn/CutIn_MyoRyeong.png): 웅크린 도약 자세를 대체해 상체를 세우고 방패 쪽 어깨를 앞으로, 검 쪽 어깨를 뒤로 둔 방어 후 반격 준비. 원본 고글·귀·머리·의상 보존 요청. 참조는 `Assets/04.Images/UI/Portrait/Player_MyoRyeong_Full.png` 한 장이다. 무기/손은 얼굴 컷인 프레임 밖이다.
- 두 PNG는1536×1024 RGBA. Arin 1,574,357 bytes, SHA256 `5B05C0550DA2C7CC7960A9C7CD421D0FEF7AAF5A55CDAB059460F09697C290E6`; MyoRyeong 1,712,255 bytes, SHA256 `F2DBCA2F2B5640A92BDBC4926346A56AD4D626F4EEF6A95EDC42E3142B93F5C8`.
- 기존 GUID 보존: Arin=`17431f09c5fefe44690262d804d92a4d`, MyoRyeong=`2b856a73e577d6040bd23b33fee763e3`. 최초 Unity 조회에서 두 장 모두 Default 텍스처/NPOT 리사이즈(2048×1024)로 로드됐다. Sprite Single·알파 투명·mipmap 꺼짐·NPOT None 정리를 시도한 호출은 Editor 연결 종료로 실패했으며, 디스크 `.meta`가 변경되지 않았음을 확인했다. importer와 실행 SO 연결은 기존 상태다.
- 생성 원본 폴더: `C:/Users/scoke/.codex/generated_images/01a1d999-d952-7843-99cd-386859388205/`. Arin=`exec-bf475183-b9c9-40a7-8fd3-fe7c04fce30a.png`, MyoRyeong=`exec-a54145fc-73eb-4967-949d-5a48ef0e3648.png`.
- [이전 아린](art-before-fresh-two/CutIn_Arin.png)·[이전 묘령](art-before-fresh-two/CutIn_MyoRyeong.png) 4,105,330 bytes 보존. 부분 눈 수정 시도는 기본 생성 폴더에만 남아 있으며 채택하지 않았다. H09 새 컷인은 이 작업에서 추가/연결하지 않는다.

## 검증과 남은 확인

AI가 생성 결과를 열어 신규 구도·양눈·깨끗한 선·묘령의 세운 자세를 확인했다. PNG 크기/32비트 알파/투명 모서리2/2 확인. Unity 재임포트 후 Texture 로드2/2 확인, 기존 Default 설정 때문에 Sprite로드0/2·NPOT리사이즈·mipmap켜짐·alphaIsTransparency=false가 관찰됐다. 이후 Editor 연결 종료로 최종 Sprite 임포트 검증은 미수행이다. 다른 컷인 PNG 해시는 작업 시작 기준 모두 동일. 코드·게임플레이 데이터 변경이 없어 C# 테스트/빌드는 실행하지 않았다.

- F01 **사용자 테스트 대기**: 아린 이미지에서 양눈 시선이 한 목표를 향하는지, 선/색면 번짐이 없는지 확인.
- F02 **사용자 테스트 대기**: 묘령의 방어 후 반격 인상이 검&방패와 어울리는지 확인.
- F03 **사용자 테스트 대기**: 실제 궁극기 패널의 얼굴 식별·귀/머리 잘림·가장자리 혼합 확인. 기존 SO의 두 ultimateFaceSprite는 미연결 상태임을 조회했으며 이번에는 이미지 재제작 요청 범위로 연결을 추가하지 않았다.

회신: `ID / 통과·실패·미수행 / 실제 결과 / 화면(필요 시)`.

## 실제 신규 생성 프롬프트

### 아린

```text
Use case: stylized-concept, NEW original illustration. Create an entirely fresh anime ultimate-skill cut-in drawing from scratch. The attached source portrait is ONLY a character design reference, not an edit target: invent a new composition and redraw every line cleanly. Landscape 1536x1024, one head-and-upper-chest character illustration on genuine transparent alpha. Production anime key animation quality: confident clean unbroken ink contours, clearly separated flat local colors, two-tone CEL SHADOWS with precise edges, minimal selective soft shading only on skin. Crisp focused eyes, simple coherent hair clumps, accurately drawn accessories. NO painterly texture, no airbrushed haze, no smeared blending, no double contours, no ghost lines, no overpaint residue, no glossy over-rendering or bloom. Preserve original youthful soft face shape, exact hair/eye/outfit colors and core accessories. Battle-cutscene intensity and purposeful acting, not a friendly neutral dialogue pose. Full head and hair silhouette within canvas with clear margin at top; neck and shoulders visible. No scenery, vignette, aura, fog, ambient glow, sparks, text, frame or watermark. Only character is opaque; all exterior pixels transparent. ARIN: faithful to reference young adult female swordswoman. Long natural black layered hair, red-brown eyes, black choker, black sailor-style cropped top with white/teal collar, teal necktie with small muted gold chevrons, black roomy jacket with teal lining and white sleeve bars. NEW COMPOSITION: upright poised stance before a decisive sword draw, shoulders turned 35 degrees toward viewer LEFT, head turned toward camera with a slight downward chin angle. Neck tall, leading shoulder compact and relaxed, no hunched forward lean. Hair sweeps cleanly behind toward viewer RIGHT; tie moves subtly to left. Quiet fierce focus, lips closed, eyebrows intent but not exaggerated. CRITICAL EYES: both eyes focus DIRECTLY ON THE VIEWER at the same distant target, normal aligned optical axes, irises at matching natural vertical height and appropriately positioned within each eye for the head perspective; single round pupil per eye, one consistent catchlight. No sideways/off-target gaze, no divergent or crossed eyes. Her eyeline must be unambiguous direct combat focus. Bust only, hands and sword outside image, do not invent armor. Design reference only; this is a newly composed sharp clean illustration.
```

### 묘령

```text
Use case: stylized-concept, NEW original illustration. Create an entirely fresh anime ultimate-skill cut-in drawing from scratch. The attached source portrait is ONLY a character design reference, not an edit target: invent a new composition and redraw every line cleanly. Landscape 1536x1024, one head-and-upper-chest character illustration on genuine transparent alpha. Production anime key animation quality: confident clean unbroken ink contours, clearly separated flat local colors, two-tone CEL SHADOWS with precise edges, minimal selective soft shading only on skin. Crisp focused eyes, simple coherent hair clumps, accurately drawn accessories. NO painterly texture, no airbrushed haze, no smeared blending, no double contours, no ghost lines, no overpaint residue, no glossy over-rendering or bloom. Preserve original youthful soft face shape, exact hair/eye/outfit colors and core accessories. Battle-cutscene intensity and purposeful acting, not a friendly neutral dialogue pose. Full head and hair silhouette within canvas with clear margin at top; neck and shoulders visible. No scenery, vignette, aura, fog, ambient glow, sparks, text, frame or watermark. Only character is opaque; all exterior pixels transparent. MYORYEONG, young adult cat-eared woman faithful to supplied original full model portrait: long black hair, two narrow side braids, black feline ears with white decorative accents, silver-rim aviator goggles ABOVE forehead, golden amber eyes, black choker with round silver ring, dark olive utility jacket with orange lining worn off shoulders over black inner top, original straps and buckles. NEW COMPOSITION specifically for a SWORD AND SHIELD fighter preparing an ultimate COUNTERATTACK: erect braced torso, proud upright neck, shoulders squared but turned 30 degrees to viewer RIGHT. Left shoulder on VIEWER RIGHT held firmly FORWARD and slightly raised as the shield-side guard; right sword shoulder on VIEWER LEFT held BACK to prepare the counter cut. Left upper arm bends forward below lower frame to brace an unseen shield, right upper arm pulled behind; hands and equipment outside the cropped bust. Keep stern golden eyes looking over the shielding side toward an enemy just to viewer-right of camera. Lips firmly closed with steady confidence. Camera is slightly LOW looking up at her steady defense; head held upright, chin level, NO crouch, NO pounce, NO bowed torso, NO animal stalking posture, NO steep top-down angle. A grounded protector holding her line before a powerful counter, not an assassin. Hair travels behind shoulders in one broad controlled sweep toward viewer LEFT, cat ears fully inside frame with 6 percent clear space above, goggles completely legible. Frame head and upper chest, do not zoom into cleavage; retain original neckline. Dramatic asymmetry comes from shield shoulder forward and sword shoulder back. Original costume, no invented plate armor, no new weapons in frame. Clean crisp newly drawn anime keyframe, exact identity from original reference.
```
