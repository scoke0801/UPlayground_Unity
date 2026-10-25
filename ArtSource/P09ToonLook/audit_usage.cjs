// 씬의 직렬화 GUID를 추적한다. 적용 전 Unity 로드 검사와 함께 사용한다.
const fs = require('fs');
const path = require('path');
function walk(dir) {
  return fs.readdirSync(dir, {withFileTypes:true}).flatMap(e => {
    const p = `${dir}/${e.name}`;
    return e.isDirectory() ? walk(p) : [p];
  });
}
const files = ['Assets/01.Scenes','Assets/03.Prefabs','Assets/10.Datas'].flatMap(walk);
const guidPaths = new Map();
for (const file of files.filter(p=>p.endsWith('.meta'))) {
  const guid = fs.readFileSync(file,'utf8').match(/^guid: ([0-9a-f]{32})/m)?.[1];
  if (guid) guidPaths.set(guid, file.slice(0,-5));
}
const references = new Map();
function refs(p) {
  if (!references.has(p)) {
    const text = fs.readFileSync(p,'utf8');
    references.set(p,[...text.matchAll(/guid: ([0-9a-f]{32})/g)].map(m=>guidPaths.get(m[1])).filter(x=>x && /\.(prefab|asset|unity)$/.test(x)));
  }
  return references.get(p);
}
const usage = new Map();
for (const scene of files.filter(p=>p.includes('/Scenario/') && p.endsWith('.unity'))) {
  const visited = new Set(), queue = [scene];
  while (queue.length) {
    const p = queue.pop();
    if (visited.has(p)) continue;
    visited.add(p);
    if (p.endsWith('.prefab')) {
      if (!usage.has(p)) usage.set(p,[]);
      usage.get(p).push(scene);
    }
    queue.push(...refs(p));
  }
}
const candidates = [...usage].filter(([p]) => /Characters\/(?:ScenarioResidents\/Npc\/|HarborResidents\/|WorldCities\/|EquipmentReview\/NpcVariants\/)|Environment\/Farming\/FarmWorker/.test(p) && !/_Model\.prefab$|HandHerbs\.prefab$/.test(p));
fs.mkdirSync('output/P09ToonLook',{recursive:true});
fs.writeFileSync('output/P09ToonLook/scene-usage.json',JSON.stringify(Object.fromEntries(candidates),null,2));
fs.writeFileSync('output/P09ToonLook/used-npcs.txt',candidates.map(([p])=>p).sort().join('\n'));
console.log(candidates.map(([p,s])=>`${p} | ${s.map(x=>path.basename(x)).join(', ')}`).join('\n'));
