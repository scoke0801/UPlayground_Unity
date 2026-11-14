# P4 기존 클립 조사

2026-11-14 artist가 읽기 전용 Unity 조회로 MotionSet의 연결 AnimationClip·길이·fps·커브를 확인했다. 아래는 **후보 확보**이며 실제 리그/무기 재생의 품질 인수가 아니다. 이름만으로 재조합 적합성이나 신규 제작 필요를 확정하지 않았다.

| 후보 | 실제 연결/측정 | 현재 판단 |
|---|---|---|
| GreatSword 차지→슬램 | `Humanoid/GreatSword/Humanoid_GreatSwordAnimationSet_Skill_1~3.asset` → Frank `GreatSword_Skill01/02/03`, 3.0/3.666687/3.666687초, 60fps, Humanoid, 각130커브. 판정 1.388789~1.888789 / 1.812321~2.312321 / 2.127507~2.627507초 | 준비 구간이 있는 후보. 차지 자세·수직 슬램 궤적 사용자 재생 확인 대기 |
| Katana 납도 후 정지 | `Humanoid/Katana/Katana_UnEquip.asset` → `Frank_RPG_Katana_Unequip.FBX/Take 001`, 2.833333초,60fps,Humanoid,137커브, 전체 구간·속도1 | 납도 후보. 칼집 접촉·칼날 수납·정지 자세 확인 대기 |
| Staff 영창 | `Humanoid/Staff/` 에셋0개. 별도 `Assets/10.Datas/Ability/Actor/P09Staff/Motion_P09Staff_Beam.asset` → `Magic_Attack_1.fbx`, 2.166667초,30fps,Humanoid,133커브. 오른손 별도 클립 0~0.033333초 유지 | 기존 마법/그립 보정 후보. 영창 유지 적합성 확인 대기 |
| Bow 조준 | `Humanoid/Bow/Humanoid_Bow_Skill_1.asset` → `Bow_Skill_4.fbx`, 2.666667초,30fps,Humanoid,133커브, 발사1.3397645초. Skill2~5 길이1.9/1.833333/1.666667/3.666667초 | 발사 전 구간 후보. 조준 유지·활시위·화살·카메라 확인 대기 |
| Whip 비접촉/접촉 피니시 | `Humanoid/Whip/WhipCharacter_Skill_Critical.asset` → `08_Frank_Whip_Combo_Skill_All.FBX/Frank_Whip@Critical_Attack`,3.166667초,60fps,Humanoid,605커브. `R_Hand_Weapon/Sword_Root/Sword_Blade01` 등 무기 본 회전. Skill1/2 길이2.5/3.166668초 | 비접촉 후보. 무기 본 이식·궤적 확인 대기. 접촉/T3는 사용자 결정 전 미구현 |

MotionSet 기준 루트: `Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/`.

## 원본

- GreatSword: `Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_GreatSword/FBX_Animation/Frank_RPG@GreatSword_Skill0{1,2,3}.FBX`
- Katana: `Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Katana/FBX_Animation/In_Place/Frank_RPG_Katana_Unequip.FBX`
- Staff: `Assets/ExternalAssets/AnimationOnly/RPG_Animations_Pack/FBX_Animations/Magic/Magic_Attack_1.fbx`
- Bow: `Assets/ExternalAssets/AnimationOnly/RPG_Animations_Pack/FBX_Animations/Bow/Skill/Bow_Skill_4.fbx`
- Whip: `Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Whip/Anim_FBX_Pack/08_Frank_Whip_Combo_Skill_All.FBX`

주의: `Katana_Finish.asset`는 납도가 아니라 `Two Hand Base/Fatal/Attack/TwoHand_Base_Fatal_Attack_1.fbx`(3.833333초)의 양손 Fatal 공격이다. 납도 재사용 근거로 삼지 않는다.

## 제작 결정

신규가 필요하다고 확인된 항목은 아직 없다. 기존 후보의 실제 대상 모델·무기/칼집·활시위·채찍 본 조합을 검수한 뒤 불가능한 것만 제작한다. 따라서 현재 Blender 신규 클립·원본 리그 변경·새 궁극기 에셋을 만들지 않았다. 기존 MotionSet/Ability 판정도 변경하지 않았다. Blender 확인과 Unity 실제 대상 재생 화면 확인 모두 미수행이다.
