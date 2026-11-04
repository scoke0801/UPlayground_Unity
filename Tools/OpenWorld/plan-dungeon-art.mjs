import fs from 'node:fs';
import assert from 'node:assert/strict';

// 현재 월드에 적용하는 저작 데이터다. 기존 전체 월드 생성기를 호출하지 않는다.
const kit = 'Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/prefabs';
function files(dir) { return fs.readdirSync(dir, {withFileTypes:true}).flatMap(e => e.isDirectory() ? files(`${dir}/${e.name}`) : [`${dir}/${e.name}`]); }
const catalogue = files(kit).filter(p => p.endsWith('.prefab'));
function asset(name) { const matches = catalogue.filter(p => p.endsWith(`/${name}.prefab`)); assert.equal(matches.length, 1, name); return matches[0]; }
const keys = {
  wall: 'COMP_Wall_01_M_straight_med', tall: 'COMP_Wall_01_M_straight_large', low: 'COMP_Wall_01_M_straight_small',
  column: 'COMP_Column_01_med', stump: 'COMP_Column_01_small', arch: 'COMP_Door_01_large',
  cart: 'COMP_PROP_cart_dungeon_02', altar: 'COMP_PROP_altar_dungeon_02', coffin: 'P_PROP_coffin_dungeon',
  grave: 'COMP_PROP_gravestone_dungeon_01', barrel: 'P_PROP_barrel_dungeon_02', crate: 'P_PROP_crate_dungeon_01',
  fire: 'COMP_PROP_campfire_dungeon', shelf: 'COMP_PROP_barrel_shelf_dungeon',
  rubble1: 'P_PROP_debris_stones_dungeon_01', rubble2: 'P_PROP_debris_stones_dungeon_02',
  rubble3: 'P_PROP_debris_stones_dungeon_03', green: 'P_PROP_vegetation_dungeon_03',
  ore: 'P_PROP_orevein_iron_dungeon', planks: 'COMP_PROP_planks_dungeon', flag: 'P_PROP_flag_dungeon_03',
  brazier: 'COMP_Column_01_base_brazier',
};
const assets = Object.fromEntries(Object.entries(keys).map(([k,v]) => [k, asset(v)]));
const oldSites = JSON.parse(fs.readFileSync('Tools/OpenWorld/landmark-sites-plan.json','utf8')).sites;
const placements = [], patches = [], lights = [];
const sites = [
  {id:'quarry',name:'폐채석장 갱구와 작업 회랑',x:542,z:1090,old:'AuthoredEnvironment/랜드마크 유적 구역/서부 폐채석장',source:'west',radius:65},
  {id:'hall',name:'묻힌 등불전',x:330,z:550,old:'AuthoredEnvironment/월드 구간/던전/묻힌 등불전',radius:48,prefab:'Assets/03.Prefabs/Environment/OpenWorld/Dungeons/SunkenHall.prefab',position:[330,105,565]},
  {id:'chain',name:'사슬 제단',x:590,z:860,old:'AuthoredEnvironment/월드 구간/던전/사슬 제단',radius:50,prefab:'Assets/03.Prefabs/Environment/OpenWorld/Dungeons/ChainTemple.prefab',position:[600,140,860]},
  {id:'crypt',name:'석관 묘역과 무너진 묘실',x:1525,z:1075,old:'AuthoredEnvironment/랜드마크 유적 구역/북동부 석관 묘역',source:'northeast',radius:68},
  {id:'fort',name:'숲 보루의 전실과 병영 폐허',x:1845,z:615,old:'AuthoredEnvironment/랜드마크 유적 구역/동부 숲 보루',source:'east',radius:68},
  {id:'waystation',name:'버려진 역참의 숙영 회랑',x:2025,z:175,old:'AuthoredEnvironment/랜드마크 유적 구역/남동부 버려진 역참',source:'southeast',radius:70},
];
const dist = (p, points) => { let d=Infinity; for(let i=1;i<points.length;i++) { const a=points[i-1],b=points[i],dx=b[0]-a[0],dz=b[1]-a[1],t=Math.max(0,Math.min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dz)/(dx*dx+dz*dz))); d=Math.min(d,Math.hypot(p[0]-a[0]-t*dx,p[1]-a[1]-t*dz)); } return d; };
for(const s of sites) s.routes = s.source ? oldSites.find(v=>v.id===s.source).paths : [];
function blocked(s,x,z,r=3.5) { return s.routes.some(route => dist([s.x+x,s.z+z],route.points)<r); }
function add(s,key,x,z,scale=1,yaw=0,kind='structure',offset=0,rotation=null) {
  placements.push({site:s.id,path:assets[key],name:key,x:s.x+x,z:s.z+z,scale:Array.isArray(scale)?scale:[scale,scale,scale],yaw,kind,offset,rotation});
}
function wall(s,a,b,profile=['wall'],gaps=[],offset=0) {
  const length=Math.hypot(b[0]-a[0],b[1]-a[1]),count=Math.round(length/4),yaw=-Math.atan2(b[1]-a[1],b[0]-a[0])*180/Math.PI;
  for(let i=0;i<count;i++) { const t=(i+.5)/count,x=a[0]+(b[0]-a[0])*t,z=a[1]+(b[1]-a[1])*t;
    if(gaps.includes(i)||blocked(s,x,z,4))continue;
    add(s,profile[Math.floor(i/3)%profile.length],x,z,[length/count/4,1,1],yaw,'structure',offset);
  }
}
function arch(s,x,z,yaw=0,scale=1.8) { add(s,'arch',x,z,scale*.6,yaw,'passage'); }
function columns(s,points) { for(const [x,z,key='column',yaw=0] of points) if(!blocked(s,x,z,2.2))add(s,key,x,z,1.1,yaw); }
function paving(s,x,z,rx,rz,seed) {
  const polygon=[]; for(let i=0;i<16;i++) { const angle=i*Math.PI/8, f=.76+.21*Math.sin(i*5.21+seed)+.09*Math.cos(i*2.7-seed); polygon.push([s.x+x+Math.cos(angle)*rx*f,s.z+z+Math.sin(angle)*rz*f]); }
  patches.push({site:s.id,polygon});
}
function scatter(s,points) { for(let i=0;i<points.length;i++) { const [x,z,scale=1]=points[i]; if(blocked(s,x,z,3.6))continue;
  add(s,`rubble${i%3+1}`,x,z,scale,i*47,'decoration');
  add(s,'green',x+1.5,z-.8,.7+i%3*.2,i*31,'decoration');
} }
function lamp(s,x,z,height=2.1) { add(s,'brazier',x,z,1,0,'decoration'); lights.push({site:s.id,x:s.x+x,z:s.z+z,height,range:8,intensity:1.8}); }

const q=sites[0];
// 南측 작업마당에서 꺾어 들어가는 갱구. 북쪽 높은 벽과 낮은 잔해가 절개면을 지지한다.
wall(q,[-40,28],[32,28],['tall','tall','wall','tall','low','wall']);
wall(q,[-40,-20],[-40,28],['low','wall','wall','tall']);
wall(q,[-28,12],[-8,12],['tall'],[2]); arch(q,-18,12,0,1.8);
wall(q,[-30,12],[-30,28],['tall']); wall(q,[-6,12],[-6,28],['tall']);
wall(q,[12,4],[32,4],['low','wall'],[2]); arch(q,22,4,0,1.6);
wall(q,[32,4],[32,28],['wall','tall','wall']);
wall(q,[12,4],[12,24],['wall','low']);
wall(q,[-34,-20],[-18,-20],['low','wall'],[1]);
columns(q,[[-40,28],[-30,12],[-6,12],[12,4],[32,4],[32,28],[-34,-20,'stump']]);
add(q,'cart',-23,-8,1.35,-35,'decoration'); add(q,'shelf',25,19,1.4,90,'decoration');
for(const [x,z] of [[-35,23],[-33,18],[28,24],[16,25]])add(q,'ore',x,z,2,30,'decoration');
for(const [x,z] of [[-27,-7],[-29,-9],[27,13],[29,15],[18,21]])add(q,'crate',x,z,1.1,x*7,'decoration');
paving(q,-15,17,16,11,1); paving(q,20,13,13,13,3); paving(q,-9,-9,15,13,5);
scatter(q,[[-38,-10,2],[-35,24,2.2],[-11,27,1.5],[29,1,1.6],[9,26,2],[-29,-21,2],[-35,4,1.7],[24,-22,1.6]]);
lamp(q,-25,15); lamp(q,-11,15); lamp(q,25,20);

const c=sites[3];
// 중앙 참배로 양쪽에 매장실을 두고 동쪽의 기존 진입 틈을 유지한다.
wall(c,[-48,-30],[-48,42],['wall','tall','tall','wall','low','wall']);
wall(c,[-48,42],[44,42],['tall','wall','low','wall','tall'],[10,11]);
wall(c,[48,-30],[48,-23],['low']); wall(c,[48,-7],[48,38],['low','wall','wall','low']);
arch(c,48,-15,90,2); arch(c,0,-35,0,2.1);
for(const side of [-1,1]) {
  for(const z of [-22,5,30]) {
    const x=side*35; wall(c,[x-10,z-9],[x+10,z-9],['wall'],[2]); arch(c,x,z-9,0,1.35);
    wall(c,[x-10,z-9],[x-10,z+9],['wall','tall']); wall(c,[x+10,z-9],[x+10,z+9],['wall','low']);
    wall(c,[x-10,z+9],[x+10,z+9],['tall','wall']);
    add(c,'coffin',x,z,1.35,side<0?90:270); add(c,'grave',x+side*4,z+3,1.3,180);
    paving(c,x,z,9,8,z); scatter(c,[[x-8,z+6,1.3],[x+8,z-6,1.4]]);
  }
}
for(const z of [-22,-8,6,20])for(const side of [-1,1]) {
  add(c,'coffin',side*16,z,1.2,0); add(c,'grave',side*19,z+3,1.2,180);
}
columns(c,[[-11,-35],[11,-35],[-11,29],[11,29],[-24,38],[24,38],[-48,42],[48,38]]);
add(c,'altar',0,28,2.1,180); paving(c,0,26,12,11,8);
scatter(c,[[-44,-29,1.7],[43,34,2],[5,41,2],[-4,-39,1.6],[29,-32,1.8]]);
lamp(c,-11,-32); lamp(c,11,-32); lamp(c,0,32);

const f=sites[4];
// 정문 전실과 방어 마당 뒤로 병영의 남은 벽이 이어진다. 사각 바위 울타리를 만들지 않는다.
wall(f,[-42,-34],[-10,-34],['tall','wall','wall','low']);
wall(f,[10,-34],[42,-34],['tall','tall','wall','low']); arch(f,0,-34,0,2.3);
wall(f,[-42,-34],[-42,30],['wall','tall','low','wall','low'],[6,7]);
wall(f,[42,-34],[42,34],['low','wall','tall','wall'],[7,8]);
wall(f,[-42,34],[-8,34],['tall','wall','wall']); wall(f,[8,34],[42,34],['wall','low','wall']);
arch(f,0,34,0,1.8);
for(const side of [-1,1]) {
  const x=side*22; wall(f,[x-8,-8],[x+8,-8],['wall'],[1,2]); arch(f,x,-8,0,1.6);
  wall(f,[x-8,-8],[x-8,18],['wall','tall','low']); wall(f,[x+8,-8],[x+8,18],['wall','wall','low']);
  wall(f,[x-8,18],[x+8,18],['wall','low','wall']);
  paving(f,x,5,11,16,side+3); add(f,'shelf',x+side*4,13,1.3,side*90,'decoration');
  add(f,'barrel',x+side*4,7,1.4,0,'decoration'); add(f,'planks',x-side*3,13,1.4,side*24,'decoration');
}
columns(f,[[-10,-34],[10,-34],[-42,-34],[42,-34],[-42,34],[42,34],[0,23]]);
add(f,'altar',0,23,1.4,180); paving(f,0,-20,12,12,4);
scatter(f,[[-37,-29,2],[38,-28,2.1],[-40,7,1.7],[39,30,2],[18,31,1.8],[-9,-9,1.4],[10,14,1.6]]);
lamp(f,-8,-31); lamp(f,8,-31); lamp(f,0,22);

const w=sites[5];
// 길가의 관문과 숙영동 두 채를 남기고 북쪽은 무너진 경계로 열어 둔다.
wall(w,[-44,-30],[-10,-30],['low','wall','tall']);wall(w,[10,-30],[44,-30],['tall','wall','low']); arch(w,0,-30,0,2.2);
wall(w,[-44,-30],[-44,32],['low','wall','wall','low'],[4,5]);
wall(w,[44,-30],[44,32],['wall','tall','low','wall'],[7,8]);
wall(w,[-42,34],[-12,34],['wall','low','wall']); wall(w,[12,34],[42,34],['low','wall','tall'],[2]);
for(const side of [-1,1]) {
  const x=side*32; wall(w,[x-8,-19],[x+8,-19],['wall','low']);
  wall(w,[x-8,21],[x+8,21],['wall','low','wall']);
  wall(w,[x+side*8,-19],[x+side*8,21],['tall','wall','low','wall']);
  wall(w,[x-side*8,-19],[x-side*8,21],['wall','low'],[3,4,7]);
  arch(w,x-side*8,-4,90,1.5); paving(w,x,0,9,23,side+4);
  for(const z of [-12,13]) { add(w,'barrel',x+side*4,z,1.3,0,'decoration');add(w,'crate',x+side*3,z+3,1.2,27,'decoration'); }
  add(w,'planks',x-side*2,14,1.5,side*13,'decoration');
}
wall(w,[-10,22],[-10,38],['wall','tall']); wall(w,[10,22],[10,38],['tall','wall']);wall(w,[-10,38],[10,38],['tall']);
arch(w,0,22,0,1.8); add(w,'fire',0,30,1.3,0,'decoration');
columns(w,[[-10,-30],[10,-30],[-44,-30],[44,-30],[-44,32],[44,32]]);
paving(w,0,29,12,12,3); paving(w,0,-20,13,9,9);
scatter(w,[[-39,-25,1.8],[39,-23,2],[-42,26,2],[40,29,2.1],[-8,38,2],[29,32,1.8],[-27,27,2],[12,36,2]]);
lamp(w,-8,-27);lamp(w,8,-27);lamp(w,0,32);

for(const s of sites) if(s.prefab) assert(fs.existsSync(s.prefab));
for(const p of placements) { assert(fs.existsSync(p.path));assert(p.scale.every(n=>n>0));assert(Number.isFinite(p.x+p.z+p.yaw)); }
assert.equal(sites.length,6);
const plan={version:2,sites,placements,patches,lights,floorMaterialSource:asset('P_MOD_Floor_01_O_straight_med')};
fs.writeFileSync('Tools/OpenWorld/dungeon-art-plan.json',JSON.stringify(plan,null,2)+'\n');
console.log(JSON.stringify({sites:sites.length,placements:placements.length,paving:patches.length,lights:lights.length,assets:Object.keys(assets).length}));
