import {authorUplandDiscovery} from './author-upland-discovery.mjs';
﻿import fs from 'node:fs';
import assert from 'node:assert/strict';
import { authorCorridorDiscovery } from './author-corridor-discovery.mjs';
import { authorForestDiscovery } from './author-forest-discovery.mjs';
const folder='output/OpenWorld/Exploration/Iteration03';
const before=JSON.parse(fs.readFileSync(folder+'/input-before.json','utf8'));
const current=JSON.parse(fs.readFileSync('Tools/OpenWorld/city-layout.json','utf8'));
assert.deepEqual(authorUplandDiscovery(authorForestDiscovery(authorCorridorDiscovery(structuredClone(before)))),current);
assert.deepEqual(authorCorridorDiscovery(structuredClone(current)),current);
assert.deepEqual(before.landscape.pickups,current.landscape.pickups);
assert.deepEqual(before.landscape.encounters,current.landscape.encounters);
assert.deepEqual(authorUplandDiscovery(authorForestDiscovery(structuredClone(before))).routes,current.routes);
assert.deepEqual(before.cities,current.cities);
assert.deepEqual(before.landscape.landforms,current.landscape.landforms);
assert.deepEqual(before.landscape.worldSize,current.landscape.worldSize);
const changed=current.landscape.props.filter(p=>['Explore_1037','Explore_1038'].includes(p.id)&&JSON.stringify(p)!==JSON.stringify(before.landscape.props.find(q=>q.id===p.id)));
assert.deepEqual(changed.map(p=>p.id),['Explore_1037','Explore_1038']);
// 하단 받침 바닥과 상층 서고 내부에만 두며 경사로 중심(548)은 비운다.
for(const p of changed){
 const lower=p.position.y===100;
 assert(p.position.x>(lower?626:602)+1&&p.position.x<(lower?638:618)-1);
 assert(p.position.z>(lower?532:538)+1&&p.position.z<(lower?548:558)-1);
 assert(Math.abs(p.position.z-548)>=4);
}
const result={utc:new Date().toISOString(),result:'PASS',changedIds:changed.map(p=>p.id),checks:['회랑의 두 책장 변경 유지·숲·고원 저작과 합성 일치','반복 저작 불변','기존 보상·적·ID·면적·도시 보존','기존 받침 내부·경사로 중심 여유']};
const output=process.argv[2]||'output/OpenWorld/Exploration/Iteration05';
fs.mkdirSync(output,{recursive:true});
fs.writeFileSync(output+'/corridor-regression.json',JSON.stringify(result,null,2)+'\n');
console.log(result);

