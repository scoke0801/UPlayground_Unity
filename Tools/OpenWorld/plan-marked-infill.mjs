import fs from 'node:fs';
import assert from 'node:assert/strict';

// 첨부 지도에서 지목한 다섯 빈터만 증분 저작한다. 이전 저작 입력은 수정하지 않는다.
const poor = 'Assets/03.Prefabs/Environment/PoorVillage/';
const farm = 'Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/';
const east = 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/';
const sites = [
  { id: 'north', name: '북쪽 땔감 마당과 방풍림', center: [1420, 1500], entry: [1360, 1420], target: [1408, 1490], radius: 135 },
  { id: 'west', name: '서쪽 고목 쉼터', center: [425, 835], entry: [490, 795], target: [407, 848], radius: 100 },
  { id: 'slope', name: '호수 동쪽 참배 숲길', center: [1355, 1060], entry: [1450, 995], target: [1380, 1108], radius: 100 },
  { id: 'farm', name: '여울마을 서쪽 다랑밭', center: [425, 260], entry: [580, 245], target: [430, 266], radius: 110 },
  { id: 'river', name: '마른 물길 짐마당', center: [1390, -130], entry: [1390, -140], target: [1390, -108], radius: 100 },
];
const routes = [
  { site: 'north', points: [[1360,1420],[1370,1450],[1390,1470],[1408,1490],[1450,1500],[1490,1475],[1490,1435],[1400,1365]], width: 4.5 },
  { site: 'west', points: [[490,795],[475,820],[455,835],[430,842],[407,848],[395,880],[410,920],[455,950],[495,990]], width: 4.5 },
  { site: 'slope', points: [[1450,995],[1430,1020],[1435,1060],[1410,1095],[1380,1108],[1340,1095],[1310,1065],[1295,1030],[1270,1010]], width: 4.5 },
  { site: 'farm', points: [[580,245],[545,260],[510,285],[470,290],[430,286],[416,265],[420,240],[470,225],[525,230],[580,245]], width: 4.5 },
  { site: 'river', points: [[1390,-140],[1390,-108],[1420,-95],[1450,-118],[1440,-170],[1460,-230]], width: 5 },
];
const placements = [], patches = [];
let seed = 110209;
const random = () => ((seed = (Math.imul(seed,1664525)+1013904223)>>>0)/4294967296);
function add(site, model, x, z, kind='prop', scale=1, yaw=0) {
  const path = model.startsWith('Assets/') ? model : poor+model+'.prefab';
  assert(fs.existsSync(path), path);
  placements.push({ id: `marked-${String(placements.length).padStart(4,'0')}`, site, path, x:+x.toFixed(2), z:+z.toFixed(2), kind, scale:+scale.toFixed(3), yaw:+yaw.toFixed(2) });
}
function yard(site,x,z,rx,rz,layer=5) { patches.push({site,x,z,rx,rz,layer,strength:.68}); }
function distance(p,a,b) { const d=[b[0]-a[0],b[1]-a[1]], t=Math.max(0,Math.min(1,((p[0]-a[0])*d[0]+(p[1]-a[1])*d[1])/(d[0]*d[0]+d[1]*d[1]))); return Math.hypot(p[0]-a[0]-d[0]*t,p[1]-a[1]-d[1]*t); }
const oldRoutes = JSON.parse(fs.readFileSync('Tools/OpenWorld/city-layout.json','utf8')).routes.map(r=>({points:r.points.map(p=>[p.x,p.z]),width:8}));
const reservedRoutes = [...oldRoutes,...routes];
const onRoute = (x,z,clearance=0) => reservedRoutes.some(r=>r.points.slice(1).some((b,i)=>distance([x,z],r.points[i],b)<r.width/2+clearance));

add('north','PoorHouse_Barn',1430,1515,'building',1,175);
add('north','PoorHouse_Hut',1460,1510,'building',1,205);
for(const [m,x,z,yaw] of [['Prop_Firewood',1420,1509,90],['Prop_Firewood',1419,1515,90],['Prop_Jige',1424,1505,35],['Prop_DryingRack',1469,1508,80],['Prop_JarPlatform',1465,1518,0],['Prop_Pyeongsang',1457,1504,10]])add('north',m,x,z,'prop',1,yaw);
add('north',farm+'Foliage/Trees/PF_Farm_GenTree_LogBundle_01.prefab',1438,1510,'prop',1,85);
for(let i=0;i<9;i++)add('north','Terrain_Stump',1410+i*5,1532+(i%3)*5,'prop',.8+random()*.45,random()*360);
yard('north',1435,1510,30,17);

add('west','Terrain_DeadTree',391,857,'tree',1.5,20);
add('west','Prop_Seonangdang',397,849,'prop',1.15,90);
add('west','Prop_Pyeongsang',415,857,'prop',1.2,110);
add('west','Prop_Mat',410,853,'ground',1.2,10);
add('west','Prop_Jige',413,861,'prop',1,70);
add('west','Prop_Jangseung',483,820,'prop',1,150);
yard('west',405,851,12,10);

add('slope','Terrain_DeadTree',1376,1122,'tree',1.35,30);
add('slope','Prop_Seonangdang',1382,1118,'prop',1.1,180);
add('slope','Prop_Jangseung',1438,1008,'prop',1,130);
for(const [x,z] of [[1335,1080],[1322,1050],[1292,1016],[1360,1115]])add('slope','Terrain_Boulder_B',x,z,'rock',1.8,random()*360);
yard('slope',1382,1118,8,7);

add('farm','PoorHouse_Chogajip',431,310,'building',1,180);
add('farm','PoorHouse_Granary',407,306,'building',1,130);
add('farm','PoorHouse_Barn',456,312,'building',1,190);
for(const [m,x,z,yaw] of [['Prop_Well',444,298,0],['Prop_JarPlatform',436,316,0],['Prop_ChickenCoop',416,316,0],['Prop_Pyeongsang',428,302,0],['Prop_Jige',455,305,75],['Prop_DryingRack',453,292,90],['Prop_Haystack',461,297,0],['Prop_Dilbang',405,298,15]])add('farm',m,x,z,'prop',1,yaw);
yard('farm',433,303,36,20);
// 밭 사이 6m 농로와 남북 접근로를 열어 둔다.
for(const [cx,cz] of [[400,255],[440,255],[400,214]]) {
  yard('farm',cx,cz,14,16,1);
  for(let ix=-4;ix<=4;ix++)for(let iz=-5;iz<=5;iz++) {
    const x=cx+ix*2.5,z=cz+iz*2.5;
    if(!onRoute(x,z,2))add('farm',farm+'Foliage/Crops/GenericCrops/PF_Farm_Wheat_05.prefab',x,z,'crop',.8+random()*.15,random()*360);
  }
  for(let i=0;i<8;i++)add('farm','Prop_Fence_Wattle',cx-11+i*3.1,cz+16,'ground',1,0);
  add('farm','Prop_Scarecrow',cx+13,cz,'prop',1,0);
  add('farm','Prop_Haystack',cx-13,cz-12,'prop',1,random()*360);
}

add('river','PoorHouse_Barn',1410,-76,'building',1,195);
for(const [m,x,z,yaw] of [['Prop_Jige',1406,-84,100],['Prop_DryingRack',1418,-83,10],['Prop_Mat',1402,-88,20],['Prop_Firewood',1418,-73,0],['Prop_JarPlatform',1403,-73,90],['Prop_Pyeongsang',1397,-85,80]])add('river',m,x,z,'prop',1,yaw);
yard('river',1408,-83,21,16);

// 숲을 섬처럼 묶고 길 주변에는 낮은 식생을 배치한다.
const groves = [
 ['north',1380,1560,80,35,24],['north',1490,1550,75,45,28],['north',1520,1475,35,65,18],
 ['west',380,930,30,65,14],['west',390,780,35,40,12],['west',459,897,30,24,9],
 ['slope',1345,1065,46,65,19],['slope',1390,1150,38,30,13],['slope',1300,1000,30,48,13],
 ['farm',358,290,25,60,13],['farm',370,190,50,24,13],['farm',488,320,28,34,9],
 ['river',1450,-60,45,24,12],['river',1458,-175,20,40,8],
];
for(const [site,cx,cz,rx,rz,count] of groves)for(let k=0;k<count;k++) {
  const angle=random()*Math.PI*2,r=Math.sqrt(random()),x=cx+Math.cos(angle)*r*rx,z=cz+Math.sin(angle)*r*rz;
  if(onRoute(x,z,7)||placements.some(p=>p.kind!=='crop'&&Math.hypot(x-p.x,z-p.z)<(p.kind==='building'?18:p.kind==='tree'?11:5)))continue;
  const tree=['A','B','C','D'][Math.floor(random()*4)];
  add(site,farm+`Foliage/Trees/PF_Farm_GenTree_${tree}_FullTree_01.prefab`,x,z,'tree',site==='farm'?.55+random()*.2:.75+random()*.4,random()*360);
  if(k%2===0)add(site,farm+'Foliage/Trees/Bushes/PF_Farm_GenTree_Bush_04.prefab',x+5,z-4,'bush',.8+random()*.6,random()*360);
  if(k%3===0)add(site,'Terrain_RockCluster',x-5,z+3,'rock',1.2+random()*.8,random()*360);
}
for(let k=0;k<40;k++) {
  const x=1320+random()*92,z=-100-random()*125;
  if(onRoute(x,z,4))continue;
  add('river',east+'Foliage/Plants/PF_EastLands_AmurSilvergrass_02.prefab',x,z,'bush',.45+random()*.3,random()*360);
}
assert.equal(sites.length,5);
assert.equal(new Set(placements.map(p=>p.id)).size,placements.length);
assert(placements.every(p=>p.x>0&&p.x<2400&&p.z>-700&&p.z<2100));
assert(placements.filter(p=>p.kind==='tree').every(p=>!onRoute(p.x,p.z,5)));
fs.writeFileSync('Tools/OpenWorld/marked-infill-plan.json',JSON.stringify({version:1,sites,routes,patches,placements},null,2)+'\n');
console.log(JSON.stringify({sites:sites.length,placements:placements.length,trees:placements.filter(p=>p.kind==='tree').length,crops:placements.filter(p=>p.kind==='crop').length}));

