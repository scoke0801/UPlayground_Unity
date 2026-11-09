import fs from 'node:fs';
import assert from 'node:assert/strict';
const folder='Tools/RiverBasinWorld/VisualR1';
const world=JSON.parse(fs.readFileSync('Tools/RiverBasinWorld/production-plan.json'));
const smooth=(a,b,v)=>{const t=Math.max(0,Math.min(1,(v-a)/(b-a)));return t*t*(3-2*t);};
const gauss=(x,z,cx,cz,rx,rz)=>Math.exp(-(((x-cx)/rx)**2+((z-cz)/rz)**2));
const riverX=z=>307.2+3.8*Math.sin((z+15.36)*.048)*Math.sin(Math.PI*Math.max(0,Math.min(1,(z+85)/150)));
const riverRadius=z=>7+1.1*Math.sin(z*.061)**2;
const segments=[...world.routes,...world.branches,{points:world.detail.points}].flatMap(r=>r.points.slice(1).map((b,i)=>[r.points[i],b]));
function nearest(x,z){let d2=1e12,h=0;for(const [a,b] of segments){const dx=b[0]-a[0],dz=b[2]-a[2];const t=Math.max(0,Math.min(1,((x-a[0])*dx+(z-a[2])*dz)/(dx*dx+dz*dz)));const d=(x-a[0]-dx*t)**2+(z-a[2]-dz*t)**2;if(d<d2){d2=d;h=a[1]+(b[1]-a[1])*t;}}return [Math.sqrt(d2),h];}
const baseline={};for(const tx of [1,2]){const bytes=fs.readFileSync(`${folder}/Baseline/Terrain_${tx}_1.f32`);baseline[tx]=new Float32Array(bytes.buffer.slice(bytes.byteOffset,bytes.byteOffset+bytes.byteLength));}
const base=(x,z)=>{const tx=x<256?1:2,ix=Math.max(0,Math.min(512,Math.round(x-(-768+512*tx)))),iz=Math.max(0,Math.min(512,Math.round(z+256)));return baseline[tx][iz*513+ix]*288-32;};
function terrain(x,z){const old=base(x,z);const extent=(1-smooth(63,90,Math.abs(z+15)))*(1-smooth(57,82,Math.abs(x-307)));if(extent===0)return old;
 const signed=x-riverX(z),d=Math.abs(signed),r=riverRadius(z);
 let bed=70.3+.16*Math.sin(z*.19+x*.11);
 const shore=smooth(r-1,r+15,d);let target=bed+(77.2-bed)*shore;
 if(signed>0){const bar=gauss(x,z,320,-47,13,25);target=target*(1-bar*.7)+(72.7+Math.max(0,signed-11)*.11)*bar*.7;}
 const natural=old+.28*Math.sin(x*.13+z*.09)+.18*Math.cos(z*.21-x*.08);
 const riverInfluence=1-smooth(20,35,d);target=target*riverInfluence+natural*(1-riverInfluence);
 target+=3.2*gauss(x,z,249,34,17,18)+2.5*gauss(x,z,345,37,20,23)+1.4*gauss(x,z,340,-61,18,16);
 const clearing=1-smooth(.8,1.25,Math.hypot((x-260)/17,z/19));target=target*(1-clearing)+77*clearing;
 const [rd,rh]=nearest(x,z);let protect=1-smooth(3.3,7.5,rd);
 const bridgeVoid=(1-smooth(8,14,Math.abs(z+15.36)))*(1-smooth(12.5,17.5,Math.abs(x-307.2)));
 protect*=1-bridgeVoid;
 target=target*(1-protect)+rh*protect;
 return old*(1-extent)+target*extent;
}
fs.mkdirSync(`${folder}/Generated`,{recursive:true});
for(const tx of [1,2]){
 const heights=new Float32Array(513*513);for(let z=0;z<513;z++)for(let x=0;x<513;x++)heights[z*513+x]=(terrain(-768+tx*512+x,-256+z)+32)/288;
 fs.writeFileSync(`${folder}/Generated/Terrain_${tx}_1.f32`,Buffer.from(heights.buffer));
 const bytes=fs.readFileSync(`${folder}/Baseline/Terrain_${tx}_1.alpha.f32`);const alpha=new Float32Array(bytes.buffer.slice(bytes.byteOffset,bytes.byteOffset+bytes.byteLength));
 for(let z=0;z<256;z++)for(let x=0;x<256;x++){
  const wx=-768+tx*512+x*512/255,wz=-256+z*512/255;
  const envelope=(1-smooth(63,90,Math.abs(wz+15)))*(1-smooth(57,82,Math.abs(wx-307)));if(envelope===0)continue;
  const h=terrain(wx,wz),wd=Math.abs(wx-riverX(wz)),rd=nearest(wx,wz)[0];const clearing=1-smooth(.8,1.25,Math.hypot((wx-260)/17,wz/19));
  const footpath=Math.max(1-smooth(2.5,4.4,rd),clearing*.7);
  const gravel=(1-smooth(13,22,wd))*(1-smooth(74.2,76.6,h));
  const patch=.5+.5*Math.sin(wx*.085+Math.sin(wz*.092)*2);
  let weights=[(.23+.2*patch)*(1-footpath)*(1-gravel),(.77-.2*patch)*(1-footpath)*(1-gravel),footpath*(1-gravel),0,0,0,gravel];
  const sum=weights.reduce((s,v)=>s+v,0);for(let i=0;i<7;i++)alpha[(z*256+x)*7+i]=alpha[(z*256+x)*7+i]*(1-envelope)+weights[i]/sum*envelope;
 }
 fs.writeFileSync(`${folder}/Generated/Terrain_${tx}_1.alpha.f32`,Buffer.from(alpha.buffer));
}
const east='Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/';
const idyllic='Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/';
const objects=[];const put=(prefab,x,z,scale,yaw,role,palette='Gold',tilt=0)=>objects.push({prefab,x,z,scale,yaw,role,palette,tilt});
let seed=94137;const rand=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};
const isClear=(x,z,margin=0)=>nearest(x,z)[0]<3.8+margin||Math.hypot((x-260)/(18+margin),z/(20+margin))<1;
// 숲의 가장자리와 수관 틈을 먼저 고정하고, 각 군락 안에서만 변형을 준다.
const trees=[[240,21,3,1.0,20,'Copper'],[249,36,1,1.12,90,'Gold'],[263,37,2,1.0,190,'Gold'],[279,40,3,1.05,55,'Copper'],[291,31,1,1.0,230,'Gold'],[301,49,2,1.0,70,'Gold'],[326,38,3,1.04,120,'Gold'],[342,29,1,1.1,240,'Copper'],[355,16,3,.92,35,'Gold'],[370,29,2,1.12,90,'Green'],[243,-24,1,.95,160,'Gold'],[253,-36,3,1,55,'Copper'],[276,-37,2,1.05,35,'Gold'],[286,-53,3,1.05,230,'Gold'],[294,-71,1,1.1,100,'Copper'],[332,-43,3,.9,170,'Gold'],[347,-54,2,1,270,'Copper'],[358,-32,1,.96,120,'Gold'],[372,-54,3,1.1,270,'Green']];
for(const [x,z,v,s,yaw,palette] of trees)put(`${east}Foliage/Trees/Osmanthus/PF_EastLands_Osmanthus_0${v}.prefab`,x,z,s,yaw,'수관 프레이밍',palette);
for(const [cx,cz,rx,rz,count] of [[232,46,24,23,15],[271,61,38,16,22],[340,58,28,22,17],[371,8,19,26,11],[259,-65,26,19,17],[351,-77,24,18,16]])for(let i=0;i<count;i++){
 const a=rand()*Math.PI*2,r=Math.sqrt(rand()),x=cx+Math.cos(a)*rx*r,z=cz+Math.sin(a)*rz*r;if(isClear(x,z,3)||Math.abs(x-riverX(z))<15)continue;
 put(`${east}Foliage/Trees/Osmanthus/PF_EastLands_Osmanthus_0${1+i%3}.prefab`,x,z,.72+rand()*.37,rand()*360,'배경 군락',i%5===0?'Green':i%3===0?'Copper':'Gold');
}
// 큰 암반은 교량 입구와 조우 중심에서 물리고, 호안의 방향을 바꾼다.
for(const [x,z,s,yaw] of [[244,15,2.1,15],[247,19,1.35,90],[250,18,.8,180],[277,-28,2.0,70],[272,-30,1.2,180],[281,25,1.9,25],[284,30,1.25,200],[333,12,1.85,50],[337,17,1.4,150],[327,-41,1.5,100],[330,-48,1.1,190],[290,5,1.6,20],[294,15,1.3,100],[294,29,1.9,10],[296,35,1.2,80],[321,33,1.5,160],[323,42,1.7,250],[295,-49,1.2,90],[292,-58,1.7,30],[320,-67,1.3,150]])put(`${east}Stones/RiverRocks/PF_EastLands_RiverRock_0${1+Math.floor(rand()*4)}.prefab`,x,z,s,yaw,'주요 암반');
for(const [cx,cz,count] of [[296,43,8],[321,31,9],[295,-56,9],[322,-63,8],[325,-40,8]])for(let i=0;i<count;i++){
 const x=cx+(rand()-.5)*8,z=cz+(rand()-.5)*13;if(isClear(x,z,1))continue;put(`${east}Stones/RiverRocks/PF_EastLands_RiverRock_0${1+i%4}.prefab`,x,z,.35+rand()*.55,rand()*360,'자갈과 작은 하상석');
}
put(`${east}Props/Logs/PF_EastLands_Log_Medium.prefab`,257,-25,1,75,'마당 외곽 쓰러진 나무');
for(const [cx,cz,count] of [[240,24,8],[260,35,8],[280,30,8],[276,-35,7],[337,25,9],[353,-31,8],[344,-58,7]])for(let i=0;i<count;i++){
 const x=cx+(rand()-.5)*15,z=cz+(rand()-.5)*12;if(isClear(x,z,2))continue;put(`${east}Foliage/Trees/Osmanthus/PF_EastLands_Osmanthus_Bush_0${1+i%3}.prefab`,x,z,.18+rand()*.2,rand()*360,'낮은 관목',i%3===0?'Copper':'Green');
}
// 노출 지표와 식생이 맞물리는 가장자리만 채운다. 조우 중심에는 낮은 자갈만 남긴다.
for(let i=0;i<4500;i++){
 const x=228+rand()*150,z=-88+rand()*158,rd=nearest(x,z)[0],wd=Math.abs(x-riverX(z));
 if(isClear(x,z,.3)||rd>19&&rand()<.65||wd<10.5||terrain(x,z)<73.5)continue;
 const large=i%7===0;put(`${east}Foliage/${large?'PF_Farm_ForestSmallGrass':'PF_Farm_SmallGrass_01'}.prefab`,x,z,(large?.7:1.0)+rand()*.7,rand()*360,'길가 낮은 풀','Green');
}
for(const [cx,cz,count] of [[322,-38,12],[321,-53,14],[295,37,10],[324,30,9]])for(let i=0;i<count;i++){
 const x=cx+(rand()-.5)*7,z=cz+(rand()-.5)*12;if(isClear(x,z,1)||terrain(x,z)>75)continue;put(`${idyllic}Reeds_0${1+i%3}.prefab`,x,z,.5+rand()*.4,rand()*360,'물가 갈대','Green');
}
for(const [cx,cz,count] of [[242,22,10],[274,-28,8],[282,25,7],[336,19,8],[350,-32,8]])for(let i=0;i<count;i++){
 const x=cx+(rand()-.5)*10,z=cz+(rand()-.5)*9;if(isClear(x,z,.3))continue;put(`${east}Foliage/PF_EastLands_GroundLeaves_02.prefab`,x,z,.25+rand()*.35,rand()*360,'군락 아래 낙엽','Copper');
}
const river=[];for(let z=65;z>=-85;z-=5)river.push([riverX(z),72,z]);river.push([riverX(-85),72,-85]);
const plan={version:1,stage:'B1 VisualR1',core:{minX:250,maxX:350,minZ:-50,maxZ:40},context:{minX:225,maxX:390,minZ:-105,maxZ:85},terrainInputs:[1,2].map(tx=>`Terrain_${tx}_1`),objects,river,bridge:{position:[307.2,77,-15.36],yaw:90,deckLength:36,deckWidth:5.6},keepClear:{encounter:[260,77,0],radii:[18,20],routeHalfWidth:3.8},views:[{id:'01_fork',eye:[239,0,4],target:[284,77,-13]},{id:'02_encounter',eye:[267,0,-2],target:[325,77,-14]},{id:'03_crossing',eye:[309,79,-15.36],target:[326,77,3]},{id:'04_return',eye:[350,0,-4],target:[291,77,-15]},{id:'05_river',eye:[332,0,-49],target:[303,75,-13]}]};
assert(plan.objects.every(p=>fs.existsSync(p.prefab)), '누락 자산');assert(plan.river.every(p=>p[1]===72));
fs.writeFileSync(`${folder}/art-plan.json`,JSON.stringify(plan,null,2));
console.log({objects:objects.length,trees:objects.filter(p=>p.role.includes('군락')||p.role.includes('프레이밍')).length,core:plan.core});
