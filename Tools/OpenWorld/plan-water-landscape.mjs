import fs from 'node:fs';
import assert from 'node:assert/strict';

const farm = 'Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/';
const east = 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/';
const sites = [
  { id:'west', name:'남서쪽 숲과 초지', center:[440,-170], radius:[190,290] },
  { id:'river', name:'물길 서쪽 혼합림', center:[1180,-105], radius:[175,380] },
];
const clusters = [
  ['west',385,-70,85,100], ['west',430,-225,110,100], ['west',485,-365,90,85],
  ['river',1060,160,70,90], ['river',1130,15,90,110],
  ['river',1195,-160,85,110], ['river',1280,-330,75,90],
];
const routes = [];
for (const file of ['city-layout','empty-space-plan','landmark-sites-plan','marked-infill-plan','district-infill-plan','district-route-repair']) {
  const data=JSON.parse(fs.readFileSync(`Tools/OpenWorld/${file}.json`,'utf8'));
  for (const r of [...(data.routes??[]),...(data.cities??[]).map(c=>({points:c.road})),...(data.sites??[]).flatMap(s=>s.paths??[])]) {
    if (!r.points) continue;
    routes.push({points:r.points.map(p=>Array.isArray(p)?p:[p.x,p.z]),width:Math.max(8,r.width??0)});
  }
}
function segmentDistance(p,a,b) {
  const dx=b[0]-a[0], dz=b[1]-a[1], length=dx*dx+dz*dz;
  const t=length===0?0:Math.max(0,Math.min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dz)/length));
  return Math.hypot(p[0]-a[0]-t*dx,p[1]-a[1]-t*dz);
}
const nearRoad=(x,z,margin)=>routes.some(r=>r.points.slice(1).some((b,i)=>segmentDistance([x,z],r.points[i],b)<r.width/2+margin));
let seed=7219;
const random=()=>((seed=(Math.imul(seed,1664525)+1013904223)>>>0)/4294967296);
const placements=[];
function add(site,path,x,z,kind,scale) {
  assert(fs.existsSync(path),path);
  if(nearRoad(x,z,kind==='tree'?7:kind==='rock'?5:2))return;
  placements.push({id:`waterland-${placements.length.toString().padStart(4,'0')}`,site,path,x:+x.toFixed(2),z:+z.toFixed(2),kind,scale:+scale.toFixed(3),yaw:+(random()*360).toFixed(2)});
}
for(const [site,cx,cz,rx,rz] of clusters) {
  for(let i=0;i<42;i++) {
    const a=random()*Math.PI*2, r=Math.sqrt(random());
    const x=cx+Math.cos(a)*rx*r,z=cz+Math.sin(a)*rz*r;
    const letter=['A','B','C','D'][Math.floor(random()*4)];
    add(site,farm+`Foliage/Trees/PF_Farm_GenTree_${letter}_FullTree_01.prefab`,x,z,'tree',.75+random()*.32);
  }
  for(let i=0;i<85;i++) {
    const a=random()*Math.PI*2,r=Math.sqrt(random())*1.12;
    const x=cx+Math.cos(a)*rx*r,z=cz+Math.sin(a)*rz*r;
    const bush=i%4===0;
    add(site,bush?farm+'Foliage/Trees/Bushes/PF_Farm_GenTree_Bush_04.prefab':east+'Foliage/PF_Farm_MediumGrass_01.prefab',x,z,bush?'bush':'grass',bush?.8+random()*.5:.8+random()*.8);
  }
  for(let i=0;i<7;i++) {
    const a=random()*Math.PI*2,r=.35+random()*.6;
    add(site,east+'Stones/Boulders/PF_EastLands_MedBoulder_02.prefab',cx+Math.cos(a)*rx*r,cz+Math.sin(a)*rz*r,'rock',.7+random()*.8);
  }
}
assert(placements.length>500);
assert(new Set(placements.map(p=>p.id)).size===placements.length);
assert(placements.every(p=>Number.isFinite(p.x)&&Number.isFinite(p.z)&&p.scale>0));
const plan={sites,clusters,routes,placements};
fs.writeFileSync('Tools/OpenWorld/water-landscape-plan.json',JSON.stringify(plan,null,2)+'\n');
console.log(JSON.stringify({placements:placements.length,counts:Object.fromEntries(['tree','bush','grass','rock'].map(k=>[k,placements.filter(p=>p.kind===k).length]))}));
