import fs from 'node:fs';
import assert from 'node:assert/strict';
const folder='Tools/RiverBasinWorld';
const p=JSON.parse(fs.readFileSync(`${folder}/world-plan.json`));
p.bridges.find(b=>b.id==='B2').yaw=50;
for(const r of p.routes)r.points=r.points.map(q=>Math.hypot(q[0]+30.72,q[2]-476.16)<1?[-50,119,466]:q);
p.nodes.N={name:'호수 북안',x:-50,z:466};
p.branches.find(r=>r.id==='Coast').points.splice(1,0,[140.5,17,-461.4]);
p.routes.find(r=>r.id==='C').points=p.routes.find(r=>r.id==='C').points.map(q=>Math.hypot(q[0]-399.36,q[2]+30.72)<1?[415,85,30]:q);
p.nodes.S={name:'옛 사당',x:415,z:30};
p.branches.find(r=>r.id==='Shrine').points=[[368.64,77,0],[368,79,40],[415,85,30],[470,76,-10],[500,65,-107],[506.88,61,-199.68],[430.08,54,-245.76]];
// 다리 끝을 향한 짧은 직선 접속부를 저작하여 꺾인 평면 경로가 난간으로 향하지 않게 한다.
for(const bridge of p.bridges){
 const a=bridge.yaw*Math.PI/180,dir=[Math.sin(a),Math.cos(a)];
 for(const r of p.routes){let i=r.points.findIndex(q=>Math.hypot(q[0]-bridge.position[0],q[2]-bridge.position[2])<.1);if(i<1)continue;
 const before=r.points[i-1],sign=Math.sign((before[0]-bridge.position[0])*dir[0]+(before[2]-bridge.position[2])*dir[1]);
 const end=s=>[bridge.position[0]+dir[0]*23*s,bridge.position[1],bridge.position[2]+dir[1]*23*s];
 r.points.splice(i,1,end(sign),bridge.position,end(-sign));
 }
}
p.branches.push({id:'ForestChoice',points:[[184.32,84,15.36],[207,87,48],[241,82,42],[261.12,77,0]]});
p.branches.push({id:'Island',points:[[-307.2,123,337.92],[-230,108,290],[-155,100,255],[-15.36,100,245.76]]});
p.detail.clearing=[260,77,0];p.detail.clearingSize=[28,32];
p.detail.points=[[130,87.53,22],[184.32,84,15.36],[261.12,77,0],[284.2,77,-15.36],[307.2,77,-15.36],[330.2,77,-15.36],[368.64,77,0],[438,68,-81]];
// 대표 숲 교량 주변은 하나의 평탄 수면. 상·하류의 낙차는 이 구간 밖에 둔다.
p.river.splice(6,2,[307.2,72,65],[307.2,72,-15.36],[307.2,72,-85]);
const lines=[...p.routes,...p.branches,p.detail];
const segments=ls=>ls.flatMap(r=>r.points.slice(1).map((b,i)=>({a:r.points[i],b})));
const road=segments(lines),water=segments([{points:p.river}]);
function nearest(x,z,segs){let best=1e12,h=0;for(const {a,b} of segs){const dx=b[0]-a[0],dz=b[2]-a[2];const t=Math.max(0,Math.min(1,((x-a[0])*dx+(z-a[2])*dz)/(dx*dx+dz*dz)));const d=(x-a[0]-dx*t)**2+(z-a[2]-dz*t)**2;if(d<best){best=d;h=a[1]+(b[1]-a[1])*t;}}return [Math.sqrt(best),h];}
const smooth=(a,b,v)=>{const t=Math.max(0,Math.min(1,(v-a)/(b-a)));return t*t*(3-2*t);};
const gauss=(x,z,cx,cz,rx,rz)=>Math.exp(-(((x-cx)/rx)**2+((z-cz)/rz)**2));
const pads=[{x:260,z:0,y:77,rx:18,rz:20},{x:142,z:42,y:88,rx:25,rz:14},{x:355,z:1,y:77,rx:23,rz:15},{x:-199.68,z:92.16,y:104,rx:62,rz:44},{x:353.28,z:583.68,y:188,rx:34,rz:30},{x:-491.52,z:230.4,y:116,rx:35,rz:35},{x:415,z:30,y:85,rx:24,rz:24},{x:-368.64,z:-384,y:52,rx:25,rz:25},{x:476.16,z:-537.6,y:8,rx:30,rz:22},{x:-15.36,z:245.76,y:100,rx:34,rz:22}];
const N=1537,heights=new Float32Array(N*N),roadDistance=new Float32Array(N*N);
function terrain(x,z){const [rd,rh]=nearest(x,z,road),[wd,wh]=nearest(x,z,water);let h=42+z*.078+12*Math.sin(x*.009)*Math.cos(z*.007)+4*Math.sin(x*.037+z*.021);
 h+=115*gauss(x,z,330,650,270,210)+54*gauss(x,z,-500,400,230,280)+36*gauss(x,z,540,140,200,310);
 const shoulder=1-smooth(18,120,rd);h=h*(1-shoulder)+(rh+1.4*Math.sin(x*.023+z*.014))*shoulder;
 const valley=1-smooth(40,170,wd);h=Math.max(h,(wh+7+3*Math.sin(x*.02))*valley+h*(1-valley));
 const lake=Math.hypot((x-p.lake.x)/p.lake.rx,(z-p.lake.z)/p.lake.rz);
 h=h*(smooth(.78,1.35,lake))+(89+Math.min(8,Math.max(0,(lake-.6)*18)))*(1-smooth(.78,1.35,lake));
 const rw=1-smooth(6,50,rd);h=h*(1-rw)+rh*rw;
 for(const pad of pads){const d=Math.hypot((x-pad.x)/pad.rx,(z-pad.z)/pad.rz);const t=1-smooth(.85,1.6,d);h=h*(1-t)+pad.y*t;}
 const core=1-smooth(4,12,rd);h=h*(1-core)+rh*core;
 for(const b of p.bridges){const a=b.yaw*Math.PI/180,dx=x-b.position[0],dz=z-b.position[2],along=dx*Math.sin(a)+dz*Math.cos(a),across=dx*Math.cos(a)-dz*Math.sin(a);const t=(1-smooth(18,23,Math.abs(along)))*(1-smooth(5,12,Math.abs(across)));h=h*(1-t)+b.position[1]*t;}
 // 마른 호안에서 얕은 하상까지 연속 경사를 남긴다.
 const riverMask=(1-smooth(6,20,wd))*smooth(.75,.95,lake);h=h*(1-riverMask)+Math.min(h,wh-2+Math.max(0,wd-5)*.50)*riverMask;
 if(z<-620){const t=smooth(-620,-760,z);h=h*(1-t)-3*t;}
 return [h,rd,wd,wh,lake];}
for(let iz=0;iz<N;iz++)for(let ix=0;ix<N;ix++){const k=iz*N+ix;const v=terrain(ix-768,iz-768);heights[k]=v[0];roadDistance[k]=v[1];}
fs.mkdirSync(`${folder}/Generated`,{recursive:true});
for(let tz=0;tz<3;tz++)for(let tx=0;tx<3;tx++){
 const out=new Float32Array(513*513);for(let z=0;z<513;z++)for(let x=0;x<513;x++)out[z*513+x]=(heights[(tz*512+z)*N+tx*512+x]+32)/288;
 fs.writeFileSync(`${folder}/Generated/terrain-${tx}-${tz}.f32`,Buffer.from(out.buffer));
 const alpha=new Float32Array(256*256*7);for(let z=0;z<256;z++)for(let x=0;x<256;x++){
  const wx=tx*512+x*2-767,wz=tz*512+z*2-767;const h=heights[Math.round(wz+768)*N+Math.round(wx+768)],rd=roadDistance[Math.round(wz+768)*N+Math.round(wx+768)];
  let snow=smooth(145,190,h)*smooth(380,530,wz),dry=(1-smooth(-300,-100,wx))*smooth(30,150,wz)*(1-snow),forest=smooth(110,190,wx)*(1-smooth(150,240,wz))*smooth(-300,-190,wz);
  const pathMask=1-smooth(3,6,rd);
  let weights=[(1-forest*.6)*(1-dry)*(1-snow)*(1-pathMask),forest*.6*(1-snow)*(1-pathMask),pathMask,0,dry*(1-pathMask),snow*(1-pathMask),0];
  if(h<2)weights=[0,0,0,0,0,0,1];
  if(h>130&&snow<.2)weights[3]=.35;
  const sum=weights.reduce((s,n)=>s+n,0);weights.forEach((v,i)=>alpha[(z*256+x)*7+i]=v/sum);
 }fs.writeFileSync(`${folder}/Generated/alpha-${tx}-${tz}.f32`,Buffer.from(alpha.buffer));
}
const placements=[];const put=(prefab,x,z,scale=1,yaw=0,role='')=>placements.push({prefab,x,z,scale,yaw,role});
const poor='Assets/03.Prefabs/Environment/PoorVillage/',lake='Assets/03.Prefabs/Environment/LakeVillage/';
put(poor+'PoorHouse_Barn.prefab',162,34,1,180,'하역 작업장');
put('Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Props/FarmTools/PF_Farm_Cart_01.prefab',153,32,1,25,'주도로 밖 하역 수레');
put('Assets/03.Prefabs/Environment/LakeVillage/Shrine/Shrine_Main.prefab',415,43,1,180,'분기 목적지 사당');
for(const [n,x,z,yaw] of [['Prop_Firewood',148,39,0],['Prop_Jige',146,37,25],['Prop_Haystack',128,44,0],['Prop_DryingRack',132,42,90],['Prop_Scarecrow',116,43,10],['Prop_StoneWall',195,27,50],['Terrain_DeadTree',254,-23,85]])put(poor+n+'.prefab',x,z,1,yaw,'생활 또는 공간 경계');
let houseCount=0;for(let i=0;i<30&&houseCount<12;i++){const x=-265+(i%6)*24,z=46+Math.floor(i/6)*26;if(nearest(x,z,road)[0]<12)continue;put(lake+['LakeHouse_Fisher','LakeHouse_Stilt','LakeHouse_Boathouse'][i%3]+'.prefab',x,z,1,i%2?160:0,'마을 블록아웃');houseCount++;}
let seed=p.seed;const rand=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};
const tree='Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/';
// 고른 간격 산포 대신 서로 크기가 다른 군락과 비어 있는 시선 회랑을 만든다.
for(const [cx,cz,rx,rz,count] of [[175,70,30,32,35],[215,-26,35,22,40],[250,66,52,29,50],[290,49,28,23,35],[334,48,26,38,40],[363,-65,25,40,40],[409,15,40,30,40],[431,-46,35,30,35],[384,-147,70,38,40],[505,-164,43,65,36],[189,38,12,10,12],[224,29,14,12,16],[236,-19,16,11,16],[274,24,10,15,12],[276,-34,12,9,14],[333,22,9,14,15],[337,-40,10,12,14]]){
 for(let i=0;i<count;i++){const a=rand()*Math.PI*2,r=Math.sqrt(rand()),x=cx+Math.cos(a)*rx*r,z=cz+Math.sin(a)*rz*r;const [h,rd,wd,wh]=terrain(x,z);if(rd<8||wd<11||h<wh+1||Math.hypot((x-260)/20,z/21)<1)continue;put(tree+`BroadleafTree_0${1+(i%2)}_Red.prefab`,x,z,1.35+rand()*.85,rand()*360,'비대칭 단풍 군락');}
}
const rock='Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Stones/RiverRocks/';
for(const [cx,cz,count] of [[204,31,3],[241,22,3],[269,-24,3],[281,15,3],[336,12,3],[352,-40,5]])for(let i=0;i<count;i++)put(rock+'PF_EastLands_RiverRock_0'+(1+i%3)+'.prefab',cx+rand()*7,cz+rand()*5,1.1+rand()*1.2,rand()*360,'분기 가림 또는 마당 외곽');
const grass='Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Foliage/PF_Farm_SmallGrass_01.prefab';
for(let i=0;i<2300;i++){const x=120+rand()*340,z=-120+rand()*215;const [h,rd,wd,wh]=terrain(x,z);if(rd<5||wd<11||h<wh+1||Math.hypot((x-260)/22,z/24)<1)continue;put(grass,x,z,.9+rand()*.9,rand()*360,'길 가장자리 낮은 풀');}
p.placements=placements;p.pads=pads;
p.routeLengths=p.routes.map(r=>({id:r.id,meters:r.points.slice(1).reduce((s,b,i)=>s+Math.hypot(...b.map((v,j)=>v-r.points[i][j])),0)}));
p.detail.planarLength=p.detail.points.slice(1).reduce((s,b,i)=>s+Math.hypot(b[0]-p.detail.points[i][0],b[2]-p.detail.points[i][2]),0);
assert(p.detail.planarLength>=300&&p.detail.planarLength<=400);
fs.writeFileSync(`${folder}/production-plan.json`,JSON.stringify(p,null,2));
const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="1100" height="1100" viewBox="-800 -800 1600 1600"><rect x="-800" y="-800" width="1600" height="1600" fill="#e1e4d6"/><g transform="scale(1,-1)"><ellipse cx="${p.lake.x}" cy="${p.lake.z}" rx="190" ry="150" fill="#6dabb7"/>${[...p.routes,...p.branches,{id:'river',points:p.river},{id:'detail',points:p.detail.points}].map(r=>`<polyline fill="none" stroke="${({A:'#ba5826',B:'#835c81',C:'#387c86',river:'#5294ad',detail:'#faac20'})[r.id]||'#888'}" stroke-width="${r.id==='detail'?10:r.id==='river'?14:4}" points="${r.points.map(q=>q[0]+','+q[2]).join(' ')}"/>`).join('')}</g>${p.bridges.map(b=>`<text x="${b.position[0]}" y="${-b.position[2]-15}" font-size="22">${b.id} Y${b.position[1]}</text>`).join('')}<text x="-750" y="-720" font-size="30">RiverBasinWorld P1 / B1 P2  ${p.detail.planarLength.toFixed(1)}m / N ↑</text></svg>`;
fs.writeFileSync(`${folder}/production-routes.svg`,svg);
console.log({tiles:9,placements:placements.length,detail:p.detail.planarLength,routes:p.routeLengths});
