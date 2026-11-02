import fs from 'node:fs';
import assert from 'node:assert/strict';

const poor='Assets/03.Prefabs/Environment/PoorVillage/';
const farm='Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/';
const shade='Assets/08.Models/WorldCities/ShadeCastle/Prefabs/';
const east='Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/';
const previous=JSON.parse(fs.readFileSync('Tools/OpenWorld/marked-infill-plan.json','utf8'));
const reserved=JSON.parse(fs.readFileSync('output/OpenWorld/DistrictInfill/reserved.json','utf8'));
const regions=[
 {id:'north',name:'북부 벌목 취락과 혼합림',center:[1430,1515],radius:240,polygon:[[1240,1450],[1280,1615],[1420,1680],[1600,1580],[1640,1420],[1520,1360],[1370,1380]],spacing:12,ground:[.12,.23,.02,.6,0,.03,0]},
 {id:'west',name:'서부 암릉 숲과 채석 생활권',center:[400,810],radius:230,polygon:[[260,620],[285,910],[370,1030],[495,1025],[565,925],[555,720],[470,605]],spacing:12,ground:[.15,.12,.35,.35,0,.03,0]},
 {id:'slope',name:'호수 동쪽 암반 계곡림',center:[1350,1100],radius:190,polygon:[[1230,980],[1220,1150],[1300,1245],[1435,1230],[1480,1110],[1450,970],[1340,950]],spacing:11,ground:[.1,.1,.32,.45,0,.03,0]},
 {id:'farm',name:'여울마을 서부 농경 생활권',center:[405,280],radius:260,polygon:[[275,70],[280,340],[330,465],[405,505],[485,460],[490,325],[480,80],[375,65]],spacing:13,ground:[.25,.3,.02,.28,0,.15,0]},
 {id:'river',name:'남부 물길 창고촌과 둔치 숲',center:[1430,-130],radius:260,polygon:[[1275,10],[1450,30],[1610,-10],[1660,-245],[1510,-360],[1370,-310],[1310,-205],[1320,-100]],spacing:12,ground:[.12,.42,.08,.2,0,.18,0]},
];
const routes=[
 {site:'north',width:6,points:[[1408,1490],[1360,1515],[1350,1550],[1400,1578],[1460,1575],[1500,1535],[1540,1495],[1530,1450],[1490,1435]]},
 {site:'west',width:5,points:[[475,820],[505,822],[535,835],[542,870],[520,900],[495,950],[495,990]]},
 {site:'farm',width:6,points:[[430,286],[435,325],[470,352],[455,390],[410,410],[365,400],[345,355],[350,310],[370,280],[390,286]]},
 {site:'farm',width:5,points:[[420,240],[430,195],[450,150],[435,110],[390,100],[360,145],[350,210],[370,280]]},
 {site:'river',width:6,points:[[1420,-95],[1455,-76],[1500,-84],[1535,-132],[1520,-195],[1485,-250],[1455,-280],[1430,-250],[1440,-170]]},
];
const routeSources=['city-layout','empty-space-plan','landmark-sites-plan','marked-infill-plan'];
const reservedRoutes=routeSources.flatMap(name=>{
 const p=JSON.parse(fs.readFileSync(`Tools/OpenWorld/${name}.json`,'utf8'));
 return name==='city-layout'?p.routes.map(r=>({width:8,points:r.points.map(v=>[v.x,v.z])})):name==='landmark-sites-plan'?p.sites.flatMap(s=>s.paths):p.routes;
}).concat(routes);
const distance=(p,a,b)=>{const dx=b[0]-a[0],dz=b[1]-a[1],t=Math.max(0,Math.min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dz)/(dx*dx+dz*dz)));return Math.hypot(p[0]-a[0]-dx*t,p[1]-a[1]-dz*t);};
const road=(x,z,extra=0)=>reservedRoutes.some(r=>r.points.slice(1).some((b,i)=>distance([x,z],r.points[i],b)<(r.width??5)/2+extra));
function inside(x,z,poly){let hit=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){const a=poly[i],b=poly[j];if((a[1]>z)!==(b[1]>z)&&x<(b[0]-a[0])*(z-a[1])/(b[1]-a[1])+a[0])hit=!hit;}return hit;}
const placements=[],patches=[],fields=[];
let seed=11022026;const rand=()=>((seed=(Math.imul(seed,1664525)+1013904223)>>>0)/4294967296);
function add(site,model,x,z,kind='prop',scale=1,yaw=0){
 const path=model.startsWith('Assets/')?model:poor+model+'.prefab';assert(fs.existsSync(path),path);
 placements.push({id:`district-${String(placements.length).padStart(5,'0')}`,site,path,x:+x.toFixed(2),z:+z.toFixed(2),kind,scale:+scale.toFixed(3),yaw:+yaw.toFixed(2)});
}
function patch(site,x,z,rx,rz,layer=5,strength=.75,shape='ellipse'){patches.push({site,x,z,rx,rz,layer,strength,shape});}
function home(site,model,x,z,yaw=180,scale=1.1){
 const candidates=[];
 for(const radius of [0,12,24,36])for(let i=0;i<(radius===0?1:12);i++) {
  const angle=i*Math.PI/6,px=x+Math.cos(angle)*radius,pz=z+Math.sin(angle)*radius;
  if(road(px,pz,8)||reserved.objects.some(o=>o.tree&&Math.hypot(px-o.x,pz-o.z)<14)||reserved.objects.some(o=>o.building&&px>o.minX-12&&px<o.maxX+12&&pz>o.minZ-12&&pz<o.maxZ+12)||placements.some(p=>p.kind==='building'&&Math.hypot(px-p.x,pz-p.z)<19))continue;
  candidates.push({x:px,z:pz,radius});
 }
 assert(candidates.length,`건물 자리 부족: ${site} ${x}/${z}`);
 ({x,z}=candidates[0]);
 add(site,model,x,z,'building',scale,yaw);patch(site,x,z,12,10);
 for(const [m,dx,dz] of [['Prop_JarPlatform',6,3],['Prop_Firewood',-7,2],['Prop_Jige',-5,-5]])if(!road(x+dx,z+dz,2))add(site,m,x+dx,z+dz,'prop',1,yaw);
}
for(const [m,x,z,yaw] of [['PoorHouse_Hut',1350,1532,120],['PoorHouse_Barn',1372,1555,140],['PoorHouse_Granary',1400,1558,180],['PoorHouse_LHouse',1430,1555,190],['PoorHouse_Jumak',1462,1550,220],['PoorHouse_Dugout',1495,1515,230],['PoorHouse_Chogajip',1520,1478,230],['PoorHouse_Barn',1508,1450,310]])home('north',m,x,z,yaw);
for(const [m,x,z,yaw] of [['PoorHouse_Barn',520,810,10],['PoorHouse_Granary',553,836,260],['PoorHouse_Jumak',526,870,90]])home('west',m,x,z,yaw);
for(const [m,x,z,yaw] of [['PoorHouse_Chogajip',375,340,90],['PoorHouse_LHouse',400,350,180],['PoorHouse_Jumak',435,360,180],['PoorHouse_Barn',465,372,220],['PoorHouse_Granary',455,335,0],['PoorHouse_Hut',325,355,90],['PoorHouse_Barn',330,310,90],['PoorHouse_Granary',350,287,30],['PoorHouse_Chogajip',380,115,0],['PoorHouse_Barn',410,115,0],['PoorHouse_Hut',440,105,250]])home('farm',m,x,z,yaw);
for(const [m,x,z,yaw] of [['PoorHouse_Granary',1435,-54,180],['PoorHouse_Barn',1470,-55,180],['PoorHouse_Chogajip',1503,-65,200],['PoorHouse_Barn',1550,-109,250],['PoorHouse_Granary',1540,-165,270],['PoorHouse_LHouse',1500,-211,0],['PoorHouse_Barn',1450,-245,90],['PoorHouse_Hut',1510,-250,0]])home('river',m,x,z,yaw,1.2);
home('river','Assets/03.Prefabs/Environment/LakeVillage/LakeHouse_Boathouse.prefab',1480,-120,90,1.15);
for(const [site,x,z] of [['north',1430,1540],['west',536,853],['farm',420,340],['river',1480,-94],['river',1515,-170]]) {
 patch(site,x,z,23,18,5,.82);
 for(const [model,dx,dz] of [['Prop_Cart',-10,2],['Prop_LogPile',7,7],['Prop_Barrel_Crates',9,-7]])if(!road(x+dx,z+dz,3))add(site,shade+model+'.prefab',x+dx,z+dz,'prop',1.3,rand()*360);
}
// 큰 경작 구획은 마을의 생산 규모가 지도에서도 보이도록 나누고 사이에 농로를 둔다.
for(const [x,z,w,d] of [[360,155,38,45],[410,160,42,42],[355,240,32,48],[365,395,40,44],[430,410,52,46],[405,470,62,38]]) {
 fields.push({id:`field-${fields.length}`,site:'farm',x,z,w,d,spacing:2.8});patch('farm',x,z,w/2,d/2,1,.92,'rectangle');
 for(let i=0;i<Math.floor(w/3.1);i++) {const px=x-w/2+i*3.1,pz=z+d/2+2;if(!road(px,pz,3))add('farm','Prop_Fence_Wattle',px,pz,'ground',1,0);}
 for(const [model,dx,dz] of [['Prop_Haystack',-w/2-3,-8],['Prop_DryingRack',-w/2-3,-13],['Prop_Scarecrow',w/2+3,0]])if(!road(x+dx,z+dz,3))add('farm',model,x+dx,z+dz,'prop',1.1,0);
}
const excluded=(x,z,r=0)=>reserved.protected.some(p=>Math.hypot(x-p.x,z-p.z)<14+r)||reserved.objects.some(o=>o.building&&x>o.minX-8-r&&x<o.maxX+8+r&&z>o.minZ-8-r&&z<o.maxZ+8+r);
const oldFields=previous.patches.filter(p=>p.layer===1);
const yards=(x,z,r=0)=>fields.some(f=>Math.abs(x-f.x)<f.w/2+8+r&&Math.abs(z-f.z)<f.d/2+8+r)||oldFields.some(f=>Math.abs(x-f.x)<f.rx+6+r&&Math.abs(z-f.z)<f.rz+6+r)||patches.some(p=>p.layer===5&&Math.abs(x-p.x)<p.rx+4+r&&Math.abs(z-p.z)<p.rz+4+r)||previous.placements.some(p=>p.kind==='prop'&&Math.hypot(x-p.x,z-p.z)<6+r);
// 암반은 길을 막는 담이 되지 않도록 나뉜 능선으로 묶는다.
for(const [site,a,b,count] of [['west',[305,680],[330,810],12],['west',[300,890],[380,1010],12],['west',[520,620],[535,755],10],['slope',[1245,1110],[1280,1190],11],['slope',[1310,970],[1375,1035],10],['slope',[1400,1170],[1455,1220],9],['river',[1310,-35],[1350,-110],8]]) {
 for(let i=0;i<count;i++) {
  const t=i/(count-1),x=a[0]+(b[0]-a[0])*t+(rand()-.5)*9,z=a[1]+(b[1]-a[1])*t+(rand()-.5)*8,scale=2+rand()*1.6;
  if(road(x,z,scale*4+5)||excluded(x,z,scale*3)||yards(x,z,8))continue;
  add(site,shade+(i%3===0?'Terrain_Rock_Slab':'Terrain_Rock_Large')+'.prefab',x,z,'outcrop',scale,Math.atan2(b[0]-a[0],b[1]-a[1])*180/Math.PI+(rand()-.5)*25);
  patch(site,x,z,scale*5,scale*3.5,2,.7);
 }
}
for(const region of regions) {
 const xs=region.polygon.map(p=>p[0]),zs=region.polygon.map(p=>p[1]);
 for(let x=Math.min(...xs);x<Math.max(...xs);x+=region.spacing)for(let z=Math.min(...zs);z<Math.max(...zs);z+=region.spacing) {
  const px=x+(rand()-.5)*region.spacing*.7,pz=z+(rand()-.5)*region.spacing*.7;
  if(!inside(px,pz,region.polygon)||road(px,pz,7)||excluded(px,pz,2)||yards(px,pz,2))continue;
  if(reserved.objects.some(o=>o.tree&&Math.hypot(px-o.x,pz-o.z)<8))continue;
  if(placements.some(p=>(p.kind==='tree'&&Math.hypot(px-p.x,pz-p.z)<10)||(p.kind==='outcrop'&&Math.hypot(px-p.x,pz-p.z)<p.scale*3)))continue;
  const wave=Math.sin(px*.025)+Math.cos(pz*.023),keep=region.id==='farm'?.68:.9;
  if(rand()>keep-(wave>1.2?.18:0))continue;
  const red=region.id==='river'&&rand()<.33;
  const tree=['A','B','C','D'][Math.floor(rand()*4)];
  add(region.id,farm+`Foliage/Trees/PF_Farm_GenTree_${red?'C':tree}_FullTree_01${red?'_Red':''}.prefab`,px,pz,'tree',region.id==='farm'?.62+rand()*.23:.85+rand()*.4,rand()*360);
  if(rand()<.6) {
   const bx=px+5,bz=pz+4;
   if(!road(bx,bz,4)&&!excluded(bx,bz)&&!yards(bx,bz))add(region.id,farm+'Foliage/Trees/Bushes/PF_Farm_GenTree_Bush_04.prefab',bx,bz,'bush',1+rand()*.6,rand()*360);
  }
  if(rand()<.25&&!road(px-5,pz+3,7))add(region.id,'Terrain_RockCluster',px-5,pz+3,'rock',1.2+rand()*.8,rand()*360);
 }
}
// 물길 양쪽에는 낮은 군락을 묶어 건널목과 지형 단차를 드러낸다.
for(let k=0;k<230;k++) {
 const z=-30-rand()*285,x=1295+(Math.abs(z)*.38)+(rand()-.5)*55;
 if(road(x,z,5)||excluded(x,z)||yards(x,z))continue;
 add('river',east+'Foliage/Plants/PF_EastLands_AmurSilvergrass_02.prefab',x,z,'bush',.5+rand()*.35,rand()*360);
}
for(const [site,x,z] of [['north',1360,1510],['west',510,823],['farm',439,325],['river',1444,-88]])if(!road(x,z,1))add(site,'Prop_Jangseung',x,z,'prop',1.2,110);
assert.equal(new Set(placements.map(p=>p.id)).size,placements.length);
assert(placements.every(p=>p.x>0&&p.x<2400&&p.z>-700&&p.z<2100));
assert(placements.filter(p=>p.kind==='tree').every(p=>!road(p.x,p.z,6.9)));
assert(regions.every(r=>placements.filter(p=>p.site===r.id&&p.kind==='tree').length>20));
// 배치의 난수 순서를 유지하면서 실제 씬 장애물을 피해 최종 동선을 확정한다.
const repairedRoutes=JSON.parse(fs.readFileSync('Tools/OpenWorld/district-route-repair.json','utf8')).routes;
assert.equal(repairedRoutes.length,routes.length);
for(let i=0;i<routes.length;i++) {
 assert.equal(repairedRoutes[i].site,routes[i].site);
 assert(repairedRoutes[i].points.length>1&&repairedRoutes[i].points.every(p=>p.length===2&&p.every(Number.isFinite)));
 routes[i].points=repairedRoutes[i].points;
}
const plan={version:1,regions,routes,patches,fields,placements,reservedRoutes,cropPath:farm+'Foliage/Crops/GenericCrops/PF_Farm_Wheat_05.prefab'};
fs.writeFileSync(process.argv[2]??'Tools/OpenWorld/district-infill-plan.json',JSON.stringify(plan,null,2)+'\n');
console.log(JSON.stringify({placements:placements.length,byKind:Object.fromEntries([...new Set(placements.map(p=>p.kind))].map(k=>[k,placements.filter(p=>p.kind===k).length])),regions:regions.map(r=>({id:r.id,trees:placements.filter(p=>p.site===r.id&&p.kind==='tree').length}))}));

