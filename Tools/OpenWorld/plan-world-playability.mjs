import fs from 'node:fs';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';

// 기존 씬 직접 저작 위에 적용할 콘텐츠 입력. 전체 월드 생성기는 실행하지 않는다.
const id = key => createHash('sha256').update(`LakeWorld.Playability.${key}`).digest('hex').slice(0,32);
const sites = [
  {id:'quarry',name:'폐채석장',entry:[542,0,1040],points:[[542,0,1080],[524,0,1110],[564,0,1105]],intent:'작업마당에서 방패병을 읽고 갱구의 궁수를 우회한다.',
    moves:[['4149d453e15e4ae78508c61b909e11b3',[539,0,1084]],['d15f54c2d8434816885af2f83883f436',[524,0,1110]]],
    loot:[[564,0,1105,'companion_exp_potion',2],[519,0,1107,'rare_hp_potion',2]]},
  {id:'crypt',name:'석관 묘역',entry:[1525,0,1030],points:[[1525,0,1075],[1525,0,1100],[1557,0,1108]],intent:'중앙 참배로의 경계를 돌파하거나 동쪽 묘실로 돌아 보상을 찾는다.',
    moves:[['441cb75bbce64560a4215b570a20d476',[1521,0,1075]],['e946fd4fb7d44d5397cf17ad267ea4b9',[1530,0,1092]]],
    loot:[[1525,0,1097,'companion_exp_potion',2],[1556,0,1108,'rare_hp_potion',2]]},
  {id:'fort',name:'숲 보루',entry:[1845,0,570],points:[[1845,0,605],[1823,0,620],[1867,0,620],[1845,0,646]],intent:'정문에서 적을 확인하고 양쪽 병영을 이용해 궁수에게 접근한다.',
    moves:[['43b183023fa048d5b5e5b99b51f3cdd0',[1843,0,605]],['ea10773ad1bf4b1daf978669e8b33bfe',[1867,0,620]]],
    loot:[[1823,0,627,'rare_hp_potion',2],[1851,0,640,'companion_exp_potion',2]]},
  {id:'waystation',name:'버려진 역참',entry:[2025,0,135],points:[[2025,0,170],[2025,0,193],[1993,0,185],[2025,0,214]],intent:'숙영마당을 지나 보급품을 회수하고 북쪽 붕괴부로 귀환한다.',
    moves:[['2b309c93fe1e4a86887e057a4d29f2b0',[2020,0,170]],['e9158dc3cde848059adf154da5da8a06',[2030,0,190]]],
    loot:[[1993,0,185,'rare_hp_potion',3],[2029,0,202,'companion_exp_potion',3]]},
  {id:'hall',name:'묻힌 등불전',entry:[330,104,520],points:[[330,104,535],[325,104,560],[330,104,578],[330,104.1,585]],intent:'등불을 따라 전실을 지나 두 수호자를 상대하고 후면 보상을 회수한다.',
    enemies:[['LakeWorld_Skeleton_Sword_Lv10',[326,104,560]],['LakeWorld_Skeleton_Common_Lv10',[335,104,577]]]},
  {id:'chain',name:'사슬 제단',entry:[550,0,840],points:[[590,140,840],[585,140,860],[580,140.1,860],[590,140,870]],intent:'좁은 입구를 빠져나와 측면 수호자를 상대하고 제단 옆 보상을 회수한다.',
    enemies:[['LakeWorld_Skeleton_Sword_Lv10',[584,140,859]],['LakeWorld_Skeleton_Bow_Lv10',[593,140,869]]]},
  {id:'ring',name:'가시울 옛터',entry:[1770,0,910],points:[[1850,110,912],[1880,110,912],[1900,110.5,898],[1930,110.1,910]],intent:'서쪽 숲길로 진입해 돌원의 식물들을 피해 동쪽 작업터 보상을 회수한다.',
    enemies:[['LakeWorld_ChildPlant_1_Lv10',[1887,110,907]],['LakeWorld_Plant_1_Lv10',[1910,110,921]]]},
];
for(const site of sites) {
  site.moves=(site.moves??[]).map(([guid,position])=>({guid,position,yaw:180}));
  site.enemies=(site.enemies??[]).map(([actorId,position],i)=>({actorId,position,yaw:180,guid:id(`${site.id}.enemy.${i}`)}));
  site.loot=(site.loot??[]).map(([x,y,z,item,count],i)=>({position:[x,y,z],item:`Assets/10.Datas/Item/Consume/${item}.asset`,count,guid:id(`${site.id}.loot.${i}`),yaw:180}));
}
const guids=sites.flatMap(s=>[...s.moves,...s.enemies,...s.loot].map(v=>v.guid));
assert.equal(new Set(guids).size,guids.length);
for(const s of sites) for(const l of s.loot) assert(fs.existsSync(l.item),l.item);
const plan={version:1,scene:'Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity',
  pickupModel:'Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/prefabs/PROPS/containers/chests/P_PROP_chest_dungeon_02.prefab',
  interaction:'Assets/10.Datas/Actor/Interaction/DropItem.asset',
  persistence:'기존 SceneEntityId/WorldStateManager 계약. 획득한 상자는 시간 재생 없이 소비 상태 유지. 몬스터는 기존 지역 리스폰 정책. 별도 회차/퀘스트 플래그 없음.',sites};
// 작업 흔적과 풍화가 서로 다른 지면으로 장소의 용도를 구분한다.
plan.ground = [
  {site:'quarry',center:[542,1090],radius:[47,37],weights:[0,.28,.68,0,0,.04,0],strength:.72},
  {site:'crypt',center:[1525,1075],radius:[50,42],weights:[.28,.6,.12,0,0,0,0],strength:.7},
  {site:'fort',center:[1845,615],radius:[43,38],weights:[.3,.58,.12,0,0,0,0],strength:.68},
  {site:'waystation',center:[2025,175],radius:[47,40],weights:[.15,.6,.1,0,0,.15,0],strength:.7},
];
plan.weathering = [
  {site:'quarry',center:[542,1090],sides:['north','west'],drop:1.65},
  {site:'crypt',center:[1525,1075],sides:['north','west'],drop:1.25},
  {site:'fort',center:[1845,615],sides:['north','east'],drop:1.4},
  {site:'waystation',center:[2025,175],sides:['north','east'],drop:1.7},
];
for(const p of plan.ground) assert(Math.abs(p.weights.reduce((a,b)=>a+b)-1)<.00001);
fs.writeFileSync('Tools/OpenWorld/world-playability-plan.json',JSON.stringify(plan,null,2)+'\n');
console.log(JSON.stringify({sites:sites.length,moved:8,newEnemies:6,rewards:8,stableIds:guids.length}));
