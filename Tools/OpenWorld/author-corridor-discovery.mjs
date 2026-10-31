// 서고의 기존 책장으로 뜰 → 경사로 → 상층의 발견 관계를 만든다.
import fs from 'node:fs';
import assert from 'node:assert/strict';

export function authorCorridorDiscovery(r) {
  for (const [id,x,y,z,yaw] of [
    ['Explore_1038',628,100,536,90],
    ['Explore_1037',614,104,544,90],
  ]) {
    const prop=r.landscape.props.find(p=>p.id===id);
    assert(prop?.prefab.endsWith('COMP_PROP_bookshelf_dungeon_01.prefab'), `기존 책장 확인 필요: ${id}`);
    prop.position={x,y,z};
    prop.yaw=yaw;
  }
  return r;
}

if (process.argv[1]?.endsWith('author-corridor-discovery.mjs')) {
  const path='Tools/OpenWorld/city-layout.json';
  const r=authorCorridorDiscovery(JSON.parse(fs.readFileSync(path,'utf8')));
  fs.writeFileSync(path,JSON.stringify(r,null,2)+'\n');
}
