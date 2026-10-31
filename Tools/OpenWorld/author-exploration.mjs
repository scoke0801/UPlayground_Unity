// 탐험 지역 저작용. 최종 생성 입력은 city-layout.json이며 원본 씬은 수정하지 않는다.
import fs from 'node:fs';
import assert from 'node:assert/strict';
import { authorTradeDiscovery } from './author-trade-discovery.mjs';
import { authorCorridorDiscovery } from './author-corridor-discovery.mjs';
import { authorForestDiscovery } from './author-forest-discovery.mjs';
import { authorUplandDiscovery } from './author-upland-discovery.mjs';
import { authorDungeons } from './author-dungeons.mjs';
const path=process.argv[2] || 'Tools/OpenWorld/city-layout.json';
const r=JSON.parse(fs.readFileSync(path,'utf8'));
const L=r.landscape, vec=(x,y,z)=>({x,y,z});
const route=(id,experience,points,halfWidth=3.4)=>({id,experience,points:points.map(p=>vec(...p)),halfWidth,followTerrain:true});
const setRoute=q=>{const i=r.routes.findIndex(v=>v.id===q.id);if(i>=0)r.routes[i]=q;else r.routes.push(q);};
L.maximumRoadEarthwork=.5;L.reliefAmplitude=2.4;L.reliefFrequency=.006;L.edgeTransition=140;
r.navigationOffRoadArea='OpenWorldOffRoad';r.navigationPreferredWidth=16;
r.navigationOpenAreas=[{x:930,y:335,width:200,height:200},{x:590,y:480,width:110,height:132}];
for(const q of r.routes)q.followTerrain=true;
for(const f of L.landforms)f.widthVariation=f.exposedBed?.12:.32;
const form=(id,innerWidth,outerWidth,points)=>{const f={id,innerWidth,outerWidth,points:points.map(p=>vec(...p)),exposedBed:false,widthVariation:.3};const i=L.landforms.findIndex(v=>v.id===id);if(i>=0)L.landforms[i]=f;else L.landforms.push(f);};
form('stone-pass-shelves',100,340,[[390,99,460],[400,114,650],[480,142,880],[570,170,1110],[790,182,1270]]);
form('upland-meadows',145,430,[[790,184,1370],[1030,192,1470],[1280,189,1500],[1480,176,1370],[1620,136,1150]]);
form('forest-floor',105,340,[[1640,129,1120],[1790,91,890],[1680,69,640],[1480,59,450]]);
form('western-foothill',55,180,[[350,110,80],[450,97,290],[650,84,370],[850,76,390],[1000,66,330]]);
form('trade-valley',55,155,[[600,62,270],[850,63,250],[1100,56,155],[1350,54,80],[1550,51,-100],[1900,50,-100]]);
form('pass-west-shoulder',10,130,[[340,128,640],[380,155,850],[430,185,1080]]);
form('forest-east-knoll',5,95,[[1840,108,760],[1870,114,900]]);
form('upland-eroded-spur',15,145,[[1130,174,1260],[1200,177,1320]]);
form('corridor-rock-basin',50,150,[[610,100,490],[625,104,590]]);
form('pass-saddle',45,160,[[565,194,1120],[650,197,1170],[700,193,1205]]);
form('east-forest-ridge',45,360,[[2250,180,1350],[2170,194,1130],[2240,170,920],[2140,160,750],[2240,140,400]]);
L.landforms.find(f=>f.id==='east-forest-ridge').widthVariation=.5;
L.platforms=[{bounds:{x:592,y:482,width:102,height:126},height:99.86,transition:32}];
setRoute(route('stone-pass','계곡 접근 → 서쪽 산허리 굽이 → 넓은 안부 → 반대편 하강과 고원 진입',[[740,68,300],[675,80,360],[585,88,420],[495,96,490],[420,105,580],[390,115,650],[425,125,715],[490,138,795],[550,145,840],[515,154,910],[490,156.08,970],[495,160.23,990],[615,176.79,1010],[650,189.89,1080],[650,196.57,1170],[800,186.81,1230]],3.8));
setRoute(route('shadow-forest','입구의 벌목 흔적 → 닫힌 수관 → 빛 드는 빈터 → 북쪽 출구',[[1630,133,1100],[1680,117,1030],[1725,105,965],[1770,94,910],[1750,91,870],[1715,85,815],[1685,80,775],[1700,78,740],[1730,75,700],[1690,71,655],[1650,69,620],[1600,64,575],[1540,62,510],[1410,59,420],[1300,56,345],[1190,54,300]],3));
setRoute(route('forest-water-loop','목재 수로 흔적과 약초 빈터를 보고 원래 숲길로 복귀',[[1700,78,740],[1640,78,750],[1605,83,805],[1650,89,855],[1750,91,870]],2.1));
setRoute(route('upland-lake-loop','바람막이 암반 → 호수·성채 조망 → 고원 초지로 복귀',[[1040,190,1410],[1100,183,1350],[1200,177,1320],[1310,176,1310],[1400,181,1365],[1360,190,1420]],2.5));
setRoute(route('quarry-rest-loop','고갯길에서 옛 채석 쉼터를 돌아 본길로 합류',[[425,125,715],[480,134,720],[530,139,760],[490,138,795]],2.5));
setRoute(route('trade-shelter-loop','수레 쉼터와 경작지 가장자리에서 본길 복귀',[[800,64,220],[825,64,170],[900,62,150],[960,61,190],[1000,59,180]],2.3));
setRoute(route('corridor-approach','서쪽 산기슭 갈림길에서 석문 안으로 진입',[[585,88,420],[610,93,450],[650,99,478],[650,100,500]],3.5));
setRoute(route('corridor-return','회랑 동쪽 귀환로에서 입구와 외부 산길 재발견',[[682,100,588],[692,100,580],[692,100,540],[690,100,490],[650,99,478]],3));
// 이전의 일정 간격 길가 군락을 장소 중심 군락으로 재배치한다.
const source=L.props.filter(p=>!p.id.startsWith('Explore_'));
for(const p of source)if(/^Expansion_(mountain|cliff)_/.test(p.id))p.slopeEmbedDepth=4;
// 안부 접근로와 겹치던 암반은 산체 쪽으로 옮겨 지면 통로를 확보한다.
const saddleCliff=source.find(p=>p.id==='Expansion_cliff_770');
if(saddleCliff)saddleCliff.position=vec(575,0,1090);
const trees=[...new Map(L.props.filter(p=>(p.id.startsWith('Expansion_')||p.id.startsWith('Explore_'))&&/Tree|Birch|Maple|Elm|Pine/.test(p.prefab)).map(p=>[p.prefab,p])).values()].sort((a,b)=>a.prefab.localeCompare(b.prefab));
const rocks=source.filter(p=>/RiverRock_0/.test(p.prefab));
assert(trees.length>0&&rocks.length>0);
L.props=source.filter(p=>!(p.id.startsWith('Expansion_')&&/Tree|Birch|Maple|Elm|Pine|Bush|Shrub/.test(p.prefab))&&!p.id.startsWith('Expansion_reed_'));
// 이번 작업 범위 밖인 저지대 식생 군락은 유지한다.
L.props.push(...source.filter(p=>p.id.startsWith('Expansion_reed_')));
let seed=7319;const rand=()=>((seed=Math.imul(seed,1664525)+1013904223|0)>>>0)/4294967296;
const distance=(p,q)=>{let d=Infinity;for(let i=1;i<q.points.length;i++){const a=q.points[i-1],b=q.points[i],dx=b.x-a.x,dz=b.z-a.z,t=Math.max(0,Math.min(1,((p.x-a.x)*dx+(p.z-a.z)*dz)/(dx*dx+dz*dz)));d=Math.min(d,Math.hypot(p.x-a.x-t*dx,p.z-a.z-t*dz));}return d;};
const clear=(p,padding)=>r.routes.filter(q=>!q.id.startsWith('dungeon-')).every(q=>distance(p,q)>q.halfWidth+padding)&&!((p.x>590&&p.x<710)&&(p.z>475&&p.z<615));
let serial=0;
const prop=(prefab,x,z,scale=1,yaw=0,extra={})=>{const p={id:'Explore_'+String(++serial).padStart(4,'0'),prefab,position:vec(x,0,z),scale:vec(scale,scale,scale),yaw,groundOffset:0,castShadows:true,...extra};assert(fs.existsSync(prefab),prefab);L.props.push(p);return p;};
const groves=[
 [1630,690,100,65,60],[1790,760,90,90,80],[1630,830,80,75,60],[1810,960,95,70,70],[1645,1030,90,65,50],
 [1530,560,95,55,45],[1430,470,65,45,25],[1720,800,50,38,30],
 [780,160,85,45,30],[940,270,65,35,22],[1200,195,75,35,26],[1400,110,70,48,24],[1680,-30,60,45,24],
 [450,500,65,50,16],[570,700,50,60,16],[970,1360,50,35,12],[1460,1380,50,40,10]
];
for(const [x,z,rx,rz,count]of groves){let made=0;for(let attempt=0;made<count&&attempt<count*30;attempt++){const angle=rand()*Math.PI*2,radius=Math.sqrt(rand()),p=vec(x+Math.cos(angle)*radius*rx,0,z+Math.sin(angle)*radius*rz);if(!clear(p,5))continue;if(Math.hypot(p.x-1750,p.z-870)<31||Math.hypot(p.x-1605,p.z-805)<20)continue;const t=trees[made%trees.length];const s=1.5+rand()*1.1;prop(t.prefab,p.x,p.z,s,rand()*360,{groundOffset:-.07});made++;}}
const E='Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/';
const wood=name=>E+'Props/WoodenProps/PF_EastLands_'+name+'.prefab';
const stone=E+'Stones/RiverRocks/PF_EastLands_RiverRock_01.prefab';
// 작업장은 운송로·그늘·발 디딜 공간이 있는 지점에 모은다.
for(const [x,z,yaw]of [[855,160,70],[1230,156,120],[1552,540,160],[510,745,35]]){
 prop(wood('Carriage_01'),x,z,1,yaw,{alignBounds:true});
 prop(wood('Log_02'),x+5,z-3,1,yaw+15,{alignBounds:true});
 prop(wood('WoodenFence_01'),x-8,z+6,1,yaw,{alignBounds:true});
 prop(wood('WoodenFence_01'),x-12,z+8,1,yaw+8,{alignBounds:true});
}
for(const [x,z]of [[1620,802],[1630,808],[1636,817]])prop(wood('WoodenPath_01'),x,z,1,30,{alignBounds:true});
for(const [x,z]of [[608,466],[644,479],[1718,855],[1712,869],[1090,1340],[1280,1290],[1335,1300],[525,760],[530,748]])prop(stone,x,z,1.6,rand()*360,{groundOffset:-.25,alignBounds:true});
prop(wood('AncientShrine_01'),1605,820,1,160,{alignBounds:true});
prop(wood('WoodenFence_01'),1300,1290,1,75,{alignBounds:true});
prop(wood('WoodenFence_01'),1302,1295,1,70,{alignBounds:true});
// 좁은 덮개 회랑과 넓은 천창 뜰. 네 방의 문·고저차·순환로는 별도 데이터로 남긴다.
const D='Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/prefabs/';
const floor=D+'MODULAR/01_PARTS/Floor/OneSided/P_MOD_Floor_01_O_straight_med.prefab';
const wall=D+'MODULAR/01_PARTS/Wall/OneSided/P_MOD_Wall_01_O_straight_med.prefab';
const arch=D+'MODULAR/01_PARTS/Gateway/P_MOD_Gateway_Arc_01.prefab';
const module=(prefab,x,y,z,yaw=0,extra={})=>{const v={position:vec(x,y,z),useAbsoluteHeight:true,alignBounds:true,...extra};const p=prop(prefab,x,z,1,yaw,{...v,solidBounds:prefab===wall});if(prefab===wall)prop(prefab,x,z,1,yaw+180,v);return p;};
const floorRect=(x,z,w,d,y)=>{for(let px=x-w/2+2;px<x+w/2;px+=4)for(let pz=z-d/2+2;pz<z+d/2;pz+=4)module(floor,px,y,pz);};
const rooms=[
 {id:'entry',x:650,z:500,w:12,d:16,y:100,gaps:{south:[650],north:[650],east:[500]}},
 {id:'court',x:650,z:532,w:24,d:24,y:100,gaps:{south:[650],north:[650],west:[536],east:[532]}},
 {id:'archive',x:610,z:548,w:16,d:20,y:104,gaps:{east:[548]}},
 {id:'heart',x:650,z:588,w:24,d:28,y:100,gaps:{south:[650],east:[588]}}
];
for(const room of rooms){const {x,z,w,d,y,gaps}=room;floorRect(x,z,w,d,y);
 for(const side of ['south','north'])for(let px=x-w/2+2;px<x+w/2;px+=4){if((gaps[side]??[]).some(c=>Math.abs(c-px)<4.1))continue;module(wall,px,y,z+(side==='south'?-d/2:d/2),side==='south'?180:0);}
 for(const side of ['west','east'])for(let pz=z-d/2+2;pz<z+d/2;pz+=4){if((gaps[side]??[]).some(c=>Math.abs(c-pz)<4.1))continue;module(wall,x+(side==='west'?-w/2:w/2),y,pz,side==='west'?270:90);}
}
floorRect(650,514,8,12,100);floorRect(650,560,8,32,100);
// 덮개는 시선을 좁히는 연결부에만 두고 카메라 높이를 확보한다.
for(let z=548;z<=572;z+=4){module(wall,646,100,z,270,{scale:vec(1,1.75,1)});module(wall,654,100,z,90,{scale:vec(1,1.75,1)});module(floor,650,107,z,0,{pitch:180,scale:vec(2,1,1)});}
// 상층 서고로 오르는 폭 8m·경사 1:5의 석조 경사로를 낮은 끝 기준으로 잇는다.
for(let i=0;i<5;i++)module(floor,636-i*4,100+i*.8,548,270,{pitch:-Math.atan(.2)*180/Math.PI,scale:vec(2,1,Math.sqrt(1.04))});
floorRect(632,540,12,16,100);
floorRect(674,588,24,8,100);floorRect(682,548,8,80,100);floorRect(668,504,24,8,100);
for(const [x,z,yaw]of [[650,492,0],[650,520,0],[650,574,0],[662,588,90]])module(arch,x,100,z,yaw,{scale:vec(2,2,1)});
// 높은 서고는 실제 석조 기단으로 받친다. 지형 오류를 덮는 장식 바위가 아니다.
for(let x=604;x<=616;x+=4){module(wall,x,100,538,180);module(wall,x,100,558);}
for(let z=540;z<=556;z+=4){module(wall,602,100,z,270);module(wall,618,100,z,90);}
for(const [x,y,z,yaw]of [[605,104,555,180],[614,104,555,180],[640,100,599,90]])module(D+'PROPS/comps/COMP_PROP_bookshelf_dungeon_01.prefab',x,y,z,yaw);
for(const [x,z,yaw]of [[646,504,0],[659,539,90],[658,598,0]])module(D+'PROPS/comps/COMP_PROP_barrel_shelf_dungeon.prefab',x,100,z,yaw);
// 실물 보상은 기존 DropItemActor와 SceneEntityId 계약으로 소비·재진입을 처리한다.
const chest=D+'PROPS/containers/chests/P_PROP_chest_dungeon_01.prefab';
const sack=D+'PROPS/containers/sacks/P_PROP_sack_dungeon_01.prefab';
L.pickups=[
 {id:'c42bb9f480ec4f3cacdf786ab0a59e01',item:'Assets/10.Datas/Item/Consume/rare_hp_potion.asset',model:chest,position:vec(650,100,596),count:2,useAbsoluteHeight:true},
 {id:'c42bb9f480ec4f3cacdf786ab0a59e02',item:'Assets/10.Datas/Item/Consume/companion_exp_potion.asset',model:chest,position:vec(607,104,552),count:1,useAbsoluteHeight:true},
 {id:'c42bb9f480ec4f3cacdf786ab0a59e03',item:'Assets/10.Datas/Item/Consume/common_hp_potion.asset',model:sack,position:vec(1608,0,814),count:2},
 {id:'c42bb9f480ec4f3cacdf786ab0a59e04',item:'Assets/10.Datas/Item/Consume/common_hp_potion.asset',model:sack,position:vec(855,0,164),count:1}
];
r.explorationWorkUnits=[
 {id:1,area:'성채–교역로–호수 남쪽–항구',connections:['trade-loop','trade-shelter-loop','river-return'],problem:'빈 들판과 도로 둑',implementation:'대지 우선 지형·운송 쉼터·하곡·우회 복귀',validation:'새 검사 대기',remaining:'입력 보행·카메라·미술 인수'},
 {id:2,area:'호수 남서쪽 (592..706,482..608)',connections:['corridor-approach','corridor-return'],problem:'던전 필드와 보상 부재',implementation:'입구·뜰·상층 서고·덮개 회랑·핵심 방·동쪽 귀환; 소비 보상 2곳과 기존 스켈레톤 조우',validation:'최종 근거: output/OpenWorld/Exploration/RESULT.md',remaining:'전투 감각·저장/재시작·리스폰 사용자 인수'},
 {id:3,area:'동쪽 (1500..1870,510..1100)',connections:['shadow-forest','forest-water-loop'],problem:'일정한 길과 식생 열',implementation:'굴곡 동선·수관 군락·밝은 빈터·목재 수로 샛길',validation:'새 검사 대기',remaining:'카메라 가림·팝인·투명 비용'},
 {id:4,area:'북쪽 (800..1630,1230..1500)',connections:['cloud-upland','upland-lake-loop'],problem:'고원 단일 횡단로',implementation:'넓은 산기슭·침식 돌출부·호수 전망 순환로',validation:'새 검사 대기',remaining:'실제 조망·낙하 경계 인수'},
 {id:5,area:'북서쪽 (390..800,300..1230)',connections:['stone-pass','quarry-rest-loop'],problem:'도로 높이 덧씌움·장거리 부분 경로',implementation:'산허리 굽이·넓은 안부·반대편 하강·채석 쉼터·도로 우선 NavMesh 비용',validation:'최종 근거: output/OpenWorld/Exploration/RESULT.md',remaining:'전구간 입력 보행·카메라 사용자 인수'}
];
r.dungeonRooms=rooms;
const city=r.cities.find(c=>c.mapId==='StonegateCitadel');
// 수면은 남쪽 해안선 높이를 유지하고 원경에서 사각 끝이 보이지 않도록 시각 범위만 늘린다.
r.cities.find(c=>c.mapId==='SaltwindHarbor').seaBounds={x:-10000,y:-20000,width:30000,height:20040};
city.road=[...city.road.filter((p,i)=>i<6),...[[1024,61.1,363],[1020,61.1,360],[980,60.3,360],[970,62.5,350],[970,62.8,335],[965,65.9,325],[965,65.6,315],[970,62.8,305],[970,63,285]].map(p=>vec(...p)),...city.road.filter(p=>p.x<=900)];
L.encounters=[
 {id:'c42bb9f480ec4f3cacdf786ab0a59e11',prefab:'Assets/03.Prefabs/Actor/Monster/MonsterActor_Skeleton_Common.prefab',position:vec(654,100.05,534),yaw:180},
 {id:'c42bb9f480ec4f3cacdf786ab0a59e12',prefab:'Assets/03.Prefabs/Actor/Monster/MonsterActor_Skeleton_Sword.prefab',position:vec(650,100.05,586),yaw:180}
];
assert(L.props.every(p=>fs.existsSync(p.prefab)));
authorTradeDiscovery(r);
authorCorridorDiscovery(r);
authorForestDiscovery(r);
authorUplandDiscovery(r);
authorDungeons(r);
fs.writeFileSync(path,JSON.stringify(r,null,2)+'\n');
console.log({props:L.props.length,oldTrees:trees.length,routes:r.routes.length,rooms:rooms.length,pickups:L.pickups.length});
