import fs from 'node:fs';
const base='Assets/03.Prefabs/Environment/PoorVillage/';
const trees='Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/';
let seed=617;
const random=()=>((seed=(Math.imul(seed,1664525)+1013904223)>>>0)/4294967296);
const pick=a=>a[Math.floor(random()*a.length)];
const placements=[],patches=[];
const names=['북쪽 고원 숲과 쉼터','서쪽 바위 전망길','동북쪽 땔감 작업 숲','동쪽 숲 작업터','남동쪽 옛 고갯길','남서쪽 들녘 농가'];
const routes=[
 [[900,1320],[1000,1360],[1100,1380],[1190,1380],[1290,1360],[1400,1365]],
 [[495,990],[520,1008],[538,1025],[550,1045],[580,1060],[615,1010]],
 [[1630,1100],[1595,1060],[1540,1030],[1490,1000],[1450,995],[1510,1055],[1595,1060]],
 [[1730,700],[1800,660],[1860,610],[1895,570],[1900,520],[1840,480]],
 [[1900,400],[1980,335],[2040,270],[2090,210],[2070,145],[1990,100]],
 [[650,180],[680,60],[720,-70],[760,-190],[820,-280],[930,-330],[1020,-240],[1060,-110],[1060,30],[1100,155]]
].map((points,i)=>({group:names[i],points,width:3.2}));
function add(i,model,x,z,scale=1,yaw=0,kind='prop'){
 const path=model.startsWith('Assets/')?model:base+model+'.prefab';
 if(!fs.existsSync(path))throw Error(path);
 placements.push({group:names[i],path,x:+x.toFixed(2),z:+z.toFixed(2),scale:+scale.toFixed(2),yaw,kind,maxSlope:model==='Prop_Pyeongsang'?12:90});
}
const patch=(x,z,rx,rz,layer,strength)=>patches.push({x,z,rx,rz,layer,strength});
for(const [i,x,z,house] of [[0,1185,1400,'PoorHouse_Hut'],[2,1480,1020,'PoorHouse_Dugout'],[3,1870,585,'PoorHouse_Barn'],[5,810,-255,'PoorHouse_Chogajip'],[5,845,-260,'PoorHouse_Barn'],[5,950,-310,'PoorHouse_LHouse']]){
 add(i,house,x,z,1,180,'building');patch(x,z,12,12,5,.75);
 for(const [model,dx,dz,yaw] of [['Prop_Firewood',-7,0,90],['Prop_Jige',-5,-5,20],['Prop_Pyeongsang',5,-7,180],['Prop_JarPlatform',6,3,90]])add(i,model,x+dx,z+dz,1,yaw);
}
for(const [i,x,z] of [[0,1080,1365],[1,544,1048],[4,2080,200]]){
 add(i,'Prop_Seonangdang',x,z);add(i,'Terrain_DeadTree',x-7,z+4,1.25,34,'tree');add(i,'Prop_Pyeongsang',x+5,z-4,1,120);patch(x,z,8,8,5,.6);
}
for(const [x,z] of [[790,-254],[932,-310]])for(const [model,dx,dz] of [['Prop_Well',0,-10],['Prop_ChickenCoop',-8,3],['Prop_DryingRack',8,7],['Prop_Haystack',10,-2],['Prop_Dilbang',-3,8]])add(5,model,x+dx,z+dz);
for(const [cx,cz] of [[760,-315],[865,-330],[975,-380]]){
 for(let k=0;k<5;k++)patch(cx+k*3.4,cz,1.1,13,1,.8);
 add(5,'Prop_Scarecrow',cx+8,cz+5);add(5,'Prop_Haystack',cx-4,cz-10);
 for(let k=0;k<6;k++)add(5,'Prop_Fence_Wattle',cx-2+k*3,cz+16);
}
for(const [i,x,z] of [[2,1468,1014],[3,1880,602]]){
 for(let k=0;k<7;k++)add(i,'Terrain_Stump',x+k*4,z+random()*12-6,.8+random()*.6,random()*360);
 add(i,trees+'PF_Farm_GenTree_LogBundle_01.prefab',x+5,z+12,1,80);add(i,'Prop_Firewood',x+9,z+13,1,80);
}
function distance(p,a,b){const dx=b[0]-a[0],dz=b[1]-a[1],t=Math.max(0,Math.min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dz)/(dx*dx+dz*dz)));return Math.hypot(p[0]-a[0]-t*dx,p[1]-a[1]-t*dz);}
const clusters=[[0,780,1390,100,55,28],[0,970,1435,85,45,24],[0,1230,1470,110,55,30],[0,1470,1405,100,55,27],[0,1050,1270,75,35,18],[0,1350,1290,65,35,17],[1,510,1080,38,60,14],[1,470,1010,35,60,12],[2,1430,1045,55,40,18],[2,1510,965,55,25,16],[3,1925,640,45,55,20],[3,1940,510,45,55,18],[4,2130,265,45,50,19],[4,2050,175,35,45,15],[5,600,-260,90,65,27],[5,700,-440,85,50,25],[5,1020,-420,70,45,20],[5,890,-130,90,45,24]];
for(const [i,cx,cz,rx,rz,count] of clusters)for(let k=0;k<count;k++){
 const a=random()*Math.PI*2,r=Math.sqrt(random()),x=cx+Math.cos(a)*r*rx,z=cz+Math.sin(a)*r*rz;
 if(routes.some(r=>r.points.slice(1).some((b,j)=>distance([x,z],r.points[j],b)<8)))continue;
 if(placements.some(p=>Math.hypot(x-p.x,z-p.z)<(p.kind==='building'?16:p.kind==='tree'?7:0)))continue;
 add(i,trees+`PF_Farm_GenTree_${pick(['A','B','C','D'])}_FullTree_01.prefab`,x,z,.85+random()*.65,random()*360,'tree');
 if(k%3===0)add(i,'Terrain_Boulder_A',x+4,z-3,1.1+random()*1.7,random()*360,'rock');
 if(k%2===0)add(i,'Terrain_RockCluster',x-3,z+3,1.5,random()*360,'rock');
}
const ids=new Set(placements.map((p,i)=>p.id=`empty-space-${String(i).padStart(4,'0')}`));
if(ids.size!==placements.length||routes.length!==6)throw Error('배치 식별자·영역 검사 실패');
fs.writeFileSync('Tools/OpenWorld/empty-space-plan.json',JSON.stringify({version:1,placements,routes,patches},null,2)+'\n');
console.log(`배치 ${placements.length}, 경로 ${routes.length}, 지표 패치 ${patches.length}`);
