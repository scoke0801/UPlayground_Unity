// 사용자 표시 영역을 월드 좌표의 폴리곤으로 옮겨 숲 내부 후보를 만든다.
import fs from 'node:fs';
import assert from 'node:assert/strict';
const zones=[
 {id:'northwest',name:'북서 능선숲',polygon:[[350,1070],[440,1280],[790,1540],[920,1530],[900,1450],[570,1170]],spacing:90},
 {id:'northeast',name:'북동 깊은 숲',polygon:[[1360,1640],[1430,1710],[1590,1660],[1730,1490],[1870,1320],[1960,1140],[1770,1210],[1500,1460]],spacing:95},
 {id:'lakeslope',name:'호수 동사면 숲',polygon:[[1210,970],[1280,1210],[1360,1240],[1450,1090],[1480,800],[1510,660],[1440,620],[1310,740]],spacing:90},
 {id:'eastwoods',name:'역참 서쪽 숲',polygon:[[1390,230],[1480,400],[1660,470],[1810,440],[1900,340],[1730,210],[1530,190]],spacing:90},
 {id:'westwoods',name:'돌등성 서쪽 숲',polygon:[[285,-140],[290,220],[360,390],[455,290],[485,70],[420,-160],[340,-190]],spacing:80},
 {id:'monastery',name:'남서 수도원 숲',polygon:[[440,-480],[480,-140],[620,30],[920,10],[1100,-150],[1120,-430],[1000,-560],[700,-570]],spacing:95}
];
function inside([x,z],poly){let result=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){const [xi,zi]=poly[i],[xj,zj]=poly[j];if(((zi>z)!==(zj>z))&&x<(xj-xi)*(z-zi)/(zj-zi)+xi)result=!result;}return result;}
const candidates=[];
for(const zone of zones){const xs=zone.polygon.map(p=>p[0]),zs=zone.polygon.map(p=>p[1]);let row=0;for(let z=Math.min(...zs)+20;z<Math.max(...zs);z+=zone.spacing*.866){for(let x=Math.min(...xs)+20+(row%2)*zone.spacing/2;x<Math.max(...xs);x+=zone.spacing){if(inside([x,z],zone.polygon))candidates.push({id:'forest_'+zone.id+'_'+String(candidates.length).padStart(3,'0'),route:zone.name,zone:zone.id,x,z,kind:candidates.length%3===0?'wildlife':'humanoid'});}row++;}}
assert.equal(zones.length,6);assert(candidates.length>75);assert.equal(new Set(candidates.map(c=>c.id)).size,candidates.length);
const plan={zones,candidates,minimumSpacing:70,maxSearchRadius:30,maxSlope:27,minimumRoadDistance:9,minimumMonsterDistance:28};
fs.writeFileSync('Tools/OpenWorld/monster-infill-plan.json',JSON.stringify(plan,null,2));
console.log(zones.map(z=>z.name+': '+candidates.filter(c=>c.zone===z.id).length+'개 후보').join('\n'));

