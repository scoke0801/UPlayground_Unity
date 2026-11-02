import fs from 'node:fs';
import assert from 'node:assert/strict';
const base='Assets/03.Prefabs/Environment/Dungeon/';
const sites=[],placements=[];
function site(id,name,x,z,w,d,entry){const s={id,name,x,z,w,d,entry,paths:[],yards:[]};sites.push(s);return s;}
function add(s,model,x,z,scale=1,yaw=0,kind='structure'){
 const path=base+model+'.prefab';assert(fs.existsSync(path),path);
 placements.push({id:`landmark-${placements.length.toString().padStart(4,'0')}`,site:s.id,path,x:s.x+x,z:s.z+z,scale,yaw,kind});
}
function line(s,model,a,b,step=6,scale=1.5){const dx=b[0]-a[0],dz=b[1]-a[1],n=Math.ceil(Math.hypot(dx,dz)/step),yaw=Math.atan2(-dz,dx)*180/Math.PI;const wall=dx===0&&model==='Dng_Wall_Ruined'?'Dng_Wall_Plain':model;for(let i=0;i<=n;i++)add(s,wall,a[0]+dx*i/n,a[1]+dz*i/n,scale,yaw,'wall');}
function path(s,points,width=6){s.paths.push({points:points.map(p=>[s.x+p[0],s.z+p[1]]),width});}
function yard(s,x,z,rx,rz,layer=5){s.yards.push({x:s.x+x,z:s.z+z,rx,rz,layer});}
function corners(s,w,d){for(const x of [-w,w])for(const z of [-d,d])add(s,'Dng_Watchtower',x,z,1.35);}
function court(s,w,d){
 line(s,'Dng_Wall_Ruined',[-w,-d],[-12,-d]);line(s,'Dng_Wall_Ruined',[12,-d],[w,-d]);
 line(s,'Dng_Wall_Plain',[-w,-d],[-w,d]);line(s,'Dng_Wall_Ruined',[w,-d],[w,d]);
 line(s,'Dng_Wall_Ruined',[-w,d],[-18,d]);line(s,'Dng_Wall_Ruined',[18,d],[w,d]);
 path(s,[[0,-d-12],[0,12]],8);path(s,[[-w+10,-d+12],[-w+10,d-4],[w-10,d-4],[w-10,-d+12],[-w+10,-d+12]],5);
}

const n=site('north','북부 옛 성채',1140,1515,210,130,[1140,1435]);
court(n,90,52);corners(n,90,52);add(n,'Dng_Gatehouse',0,-52,1.5);add(n,'Dng_Keep',0,32,1.65);
for(const x of [-52,52]){add(n,'Dng_Barracks',x,12,1.3,90);add(n,'Dng_Guardhouse',x,-27,1.3,180);yard(n,x,8,20,30);}
for(const x of [-18,18]){add(n,'Dng_Statue_Guardian',x,-12,1.5,180);add(n,'Dng_Brazier',x,-30,1.3);}
for(const x of [-35,35])add(n,'Dng_Prison',x,37,1.25,180);
path(n,[[0,-52],[0,-80],[-40,-110],[-100,-135]],8);yard(n,0,0,83,48);yard(n,0,32,17,16,2);

const w=site('west','서부 폐채석장',542,1090,100,85,[550,1045]);
line(w,'Dng_Wall_Ruined',[-38,24],[38,24]);line(w,'Dng_Wall_Ruined',[-38,-16],[-38,24]);
add(w,'Dng_Watchtower',31,22,1.4);add(w,'Dng_Forge',-22,8,1.4);add(w,'Dng_Barracks',15,5,1.2,90);
for(const x of [-25,-12,1,14,27]){add(w,'Dng_Crates_Barrels',x,-15,1.3,30);add(w,'Dng_Buttress',x,25,1.5);}
for(const x of [-16,16])add(w,'Dng_Arch',x,-30,1.5);
path(w,[[8,-45],[0,-28],[0,0],[-20,0]],7);path(w,[[0,0],[25,-8],[18,10]],5);yard(w,0,0,38,28,2);

const ne=site('northeast','북동부 석관 묘역',1525,1075,110,90,[1595,1060]);
for(const x of [-35,35])for(const z of [-22,5,30])add(ne,'Dng_Crypt',x,z,1.25,x<0?90:270);
for(const x of [-16,16])for(const z of [-22,-8,6,20]){add(ne,'Dng_Sarcophagus',x,z,1.3);add(ne,'Dng_Pillar',x+(x<0?-7:7),z,1.4);}
add(ne,'Dng_Chapel',0,30,1.5,180);add(ne,'Dng_Altar',0,5,1.5);
for(const x of [-10,10])add(ne,'Dng_Statue_Guardian',x,-34,1.5,180);
line(ne,'Dng_Wall_Ruined',[-48,40],[-48,-30]);line(ne,'Dng_Wall_Ruined',[48,-30],[48,-23]);line(ne,'Dng_Wall_Ruined',[48,-7],[48,40]);
path(ne,[[70,-15],[48,-15],[0,-35],[0,20]],6);path(ne,[[-8,-30],[-8,10],[8,10],[8,-30],[-8,-30]],5);yard(ne,0,0,45,38,2);

const e=site('east','동부 숲 보루',1845,615,105,95,[1800,660]);
court(e,42,34);corners(e,42,34);add(e,'Dng_Gatehouse',0,-34,1.5);add(e,'Dng_Keep',0,22,1.25);
for(const x of [-23,23]){add(e,'Dng_Barracks',x,4,1.15);add(e,'Dng_Weapon_Rack',x,-12,1.3,180);add(e,'Dng_Crates_Barrels',x,-20,1.2);}
path(e,[[-45,45],[-58,45],[-58,0],[0,-48],[0,-34]],6);yard(e,0,0,38,30);

const se=site('southeast','남동부 버려진 역참',2025,175,110,90,[1990,100]);
line(se,'Dng_Wall_Ruined',[-44,-30],[-12,-30]);line(se,'Dng_Wall_Ruined',[12,-30],[44,-30]);
for(const x of [-30,30])for(const z of [-12,13])add(se,'Dng_Barracks',x,z,1.25,x<0?90:270);
add(se,'Dng_Guardhouse',-35,34,1.4);add(se,'Dng_Watchtower',35,34,1.5);add(se,'Dng_Gatehouse',0,-30,1.5);
add(se,'Dng_Forge',0,30,1.35);for(const x of [-15,15])add(se,'Dng_Crates_Barrels',x,12,1.5);
path(se,[[-35,-75],[-15,-52],[0,-40],[0,22]],7);path(se,[[-20,-20],[-20,22],[20,22],[20,-20],[-20,-20]],5);yard(se,0,0,38,32);

const sw=site('southwest','남서부 무너진 수도원',770,-405,210,150,[820,-280]);
court(sw,88,60);add(sw,'Dng_Chapel',0,42,1.6,180);add(sw,'Dng_Keep',-62,40,1.5);add(sw,'Dng_Watchtower',75,-45,1.4);
for(const x of [-45,45]){add(sw,'Dng_Barracks',x,8,1.35);add(sw,'Dng_Crypt',x,-35,1.4,180);}
for(const x of [-20,20])for(const z of [-25,-10,5,20])add(sw,'Dng_Pillar',x,z,1.7);
for(const x of [-9,9])add(sw,'Dng_Statue_Guardian',x,30,1.5,180);
add(sw,'Dng_Altar',0,18,1.6);add(sw,'Dng_Arch',0,-60,2);
for(const x of [-65,65])for(const z of [-24,-10,4])add(sw,'Dng_Sarcophagus',x,z,1.3,90);
path(sw,[[50,125],[50,95],[15,80],[0,65],[0,57],[-16,55],[-16,25],[0,12],[0,-60]],8);yard(sw,0,0,82,56,2);

assert.equal(new Set(placements.map(p=>p.id)).size,placements.length);
fs.writeFileSync('Tools/OpenWorld/landmark-sites-plan.json',JSON.stringify({sites,placements},null,2)+'\n');
console.log(`장소 ${sites.length}, 구조물 ${placements.length}`);
