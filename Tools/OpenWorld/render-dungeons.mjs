import fs from 'node:fs';
const r=JSON.parse(fs.readFileSync('Tools/OpenWorld/city-layout.json','utf8'));
const x=v=>55+v*.3,z=v=>45+(2100-v)*.3;
let s=`<svg xmlns="http://www.w3.org/2000/svg" width="1100" height="950" viewBox="0 0 1100 950"><rect width="1100" height="950" fill="#f6f3ea"/><g font-family="Malgun Gothic,sans-serif" fill="#333"><text x="55" y="28" font-size="22">신규 던전 3곳 · 월드 X/Z(m)</text><rect x="55" y="45" width="720" height="840" fill="#e3e8d5" stroke="#73816a"/>`;
for(const q of r.routes)s+=`<polyline points="${q.points.map(p=>`${x(p.x)},${z(p.z)}`).join(' ')}" fill="none" stroke="${q.id.startsWith('dungeon-')?'#9a4d35':'#8d8569'}" stroke-width="${q.id.startsWith('dungeon-')?3:1.5}"/>`;
for(const [label,px,pz] of [['생명의 호수',1100,900],['잊혀진 회랑',650,550],['그늘숲 빈터',1750,870],['구름등 고원',1150,1450],['돌등 고개',480,1020],['운송 쉼터',844,192]])s+=`<text x="${x(px)}" y="${z(pz)}" font-size="14">${label}</text>`;
r.dungeons.forEach((d,i)=>{s+=`<circle cx="${x(d.position.x)}" cy="${z(d.position.z)}" r="11" fill="#9a4d35"/><text x="${x(d.position.x)+15}" y="${z(d.position.z)-10}" font-size="15">${d.name}</text><text x="800" y="${110+i*125}" font-size="19">${d.name}</text><text x="800" y="${139+i*125}" font-size="14">${d.position.x} / ${d.position.z}</text>`;});
s+='<text x="800" y="540" font-size="14">맵 면적 2,400 × 2,800m 유지</text><text x="800" y="570" font-size="14">호수·생활권·전망 빈 공간 보존</text><text x="55" y="925" font-size="14">설계 위치도. 실제 장면은 after/atlas.png 및 journey/ 화면 참조. 런타임 미니맵 아님.</text></g></svg>';
fs.writeFileSync('output/OpenWorld/Exploration/Iteration06/layout.svg',s);
