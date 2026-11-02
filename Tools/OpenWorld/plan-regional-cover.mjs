import fs from 'node:fs';
import assert from 'node:assert/strict';
const farm='Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/';
const poor='Assets/03.Prefabs/Environment/PoorVillage/';
let seed=1102;const rand=()=>((seed=(Math.imul(seed,1664525)+1013904223)>>>0)/4294967296);
const regions=[
 {id:'upland',name:'북서 고원 혼합림',polygon:[[565,1190],[650,1390],[990,1430],[1335,1340],[1340,1200],[1100,1170],[850,1175]],spacing:21},
 {id:'ridge',name:'북동 바위숲',polygon:[[1400,1195],[1415,1350],[1660,1420],[1880,1380],[1860,1160],[1650,1130]],spacing:21},
 {id:'east',name:'동남 교역림과 들판',polygon:[[1450,40],[1440,280],[1610,340],[1815,310],[2050,260],[2040,85],[1830,30],[1630,25]],spacing:22},
 {id:'south',name:'남서 농경지와 과수림',polygon:[[600,340],[875,355],[1000,220],[1060,-40],[1150,-390],[1180,-575],[1020,-605],[910,-540],[850,-400],[630,-405],[485,-285],[520,-160],[665,-125]],spacing:22}
];
const layout=JSON.parse(fs.readFileSync('Tools/OpenWorld/city-layout.json','utf8'));
const empty=JSON.parse(fs.readFileSync('Tools/OpenWorld/empty-space-plan.json','utf8'));
const landmarks=JSON.parse(fs.readFileSync('Tools/OpenWorld/landmark-sites-plan.json','utf8'));
const routes=[...layout.routes.map(r=>({points:r.points.map(p=>[p.x,p.z])})),...empty.routes,...landmarks.sites.flatMap(s=>s.paths)];
const pathDist=(p,a,b)=>{const dx=b[0]-a[0],dz=b[1]-a[1],t=Math.max(0,Math.min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dz)/(dx*dx+dz*dz)));return Math.hypot(p[0]-a[0]-dx*t,p[1]-a[1]-dz*t);};
function nearRoad(x,z,r=10){return routes.some(q=>q.points.slice(1).some((b,i)=>pathDist([x,z],q.points[i],b)<r));}
function inside(x,z,poly){let result=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){const a=poly[i],b=poly[j];if((a[1]>z)!==(b[1]>z)&&x<(b[0]-a[0])*(z-a[1])/(b[1]-a[1])+a[0])result=!result;}return result;}
const fields=[];
for(const [region,x,z,w,d] of [['south',700,80,62,40],['south',810,100,62,42],['south',940,60,60,44],['south',725,-70,58,42],['south',905,-100,65,42],['south',1030,-175,58,42],['south',1040,-325,58,40],['south',1045,-500,62,42],['east',1520,180,62,42],['east',1650,140,68,44],['east',1790,210,62,42]]){
 fields.push({region,x,z,w,d,spacing:2.8});
}
const placements=[];
function add(region,path,x,z,scale,kind,yaw=rand()*360){assert(fs.existsSync(path),path);placements.push({region,path,x:+x.toFixed(2),z:+z.toFixed(2),scale:+scale.toFixed(2),kind,yaw});}
for(const r of regions){const xs=r.polygon.map(p=>p[0]),zs=r.polygon.map(p=>p[1]);
 for(let x=Math.min(...xs);x<Math.max(...xs);x+=r.spacing)for(let z=Math.min(...zs);z<Math.max(...zs);z+=r.spacing){
  const px=x+(rand()-.5)*r.spacing*.75,pz=z+(rand()-.5)*r.spacing*.75;
  if(!inside(px,pz,r.polygon)||nearRoad(px,pz)||landmarks.sites.some(s=>Math.abs(px-s.x)<s.w/2+16&&Math.abs(pz-s.z)<s.d/2+16)||fields.some(f=>Math.abs(px-f.x)<f.w/2+12&&Math.abs(pz-f.z)<f.d/2+12))continue;
  // 넓은 띠 단위로 밀도를 바꿔 숲 내부와 열린 가장자리를 만든다。
  const wave=(Math.sin(px*.014)+Math.cos(pz*.019)+Math.sin((px+pz)*.009))/3;
  if(rand()<(r.id==='south'?.12:.06)+(wave>.4?.15:0))continue;
  const red=(r.id==='east'&&rand()<.72)||(r.id==='ridge'&&rand()<.22);
  const model=red?'PF_Farm_GenTree_C_FullTree_01_Red.prefab':`PF_Farm_GenTree_${['A','B','C','D'][Math.floor(rand()*4)]}_FullTree_01.prefab`;
  add(r.id,farm+'Foliage/Trees/'+model,px,pz,r.id==='south'?.65+rand()*.25:.88+rand()*.4,'tree');
  if(rand()<.23)add(r.id,farm+'Foliage/Trees/Bushes/PF_Farm_GenTree_Bush_04.prefab',px+7,pz-5,1+rand(),'bush');
  if(rand()<(r.id==='ridge'?.45:.15))add(r.id,poor+'Terrain_Boulder_A.prefab',px-5,pz+6,1.5+rand()*2.5,'rock');
 }
}
// 울타리와 수확 도구는 작물 구획에 붙인다.
for(const f of fields){
 for(let i=0;i<Math.floor(f.w/3.1);i++)add(f.region,poor+'Prop_Fence_Wattle.prefab',f.x-f.w/2+i*3.1,f.z+f.d/2+3,1,'prop',0);
 add(f.region,poor+'Prop_Scarecrow.prefab',f.x+f.w/2+3,f.z,1,'prop',0);
 add(f.region,poor+'Prop_Haystack.prefab',f.x-f.w/2-4,f.z-8,1.3,'prop');
 add(f.region,poor+'Prop_DryingRack.prefab',f.x-f.w/2-4,f.z-13,1,'prop');
}
const sites=[{id:'upland',name:'북서 고원',x:950,z:1290,w:730,entry:[900,1320]},{id:'ridge',name:'북동 바위숲',x:1640,z:1260,w:540,entry:[1630,1220]},{id:'east',name:'교역 들판',x:1740,z:180,w:640,entry:[1700,300]},{id:'south',name:'남서 농경지',x:850,z:-120,w:760,entry:[800,180]}];
const plan={regions,sites,fields,placements,routes,cropPath:farm+'Foliage/Crops/GenericCrops/PF_Farm_Wheat_05.prefab'};
fs.writeFileSync('Tools/OpenWorld/regional-cover-plan.json',JSON.stringify(plan,null,2)+'\n');
console.log(JSON.stringify({placements:placements.length,trees:placements.filter(p=>p.kind==='tree').length,fields:fields.length}));
