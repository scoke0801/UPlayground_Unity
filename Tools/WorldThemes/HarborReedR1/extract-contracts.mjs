import fs from 'node:fs';
import crypto from 'node:crypto';
const source='output/WorldThemesSharedBaseline/Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const rows=[];
for(const b of fs.readFileSync(source,'utf8').split('\n--- !u!')){
 const id=b.match(/^114 &(-?\d+)/)?.[1],guid=b.match(/^  _guid: (.+)$/m)?.[1]?.trim();
 if(id&&guid)rows.push([id,guid,b.match(/^  _ownerKey: (.+)$/m)?.[1]?.trim()]);
 if(b.startsWith('1001 '))for(const m of b.matchAll(/- target: \{fileID: (\d+),[^\n]+\}\r?\n\s+propertyPath: _guid\r?\n\s+value: (\w+)/g))rows.push([m[1],m[2],'prefab override']);
}
fs.writeFileSync('output/HarborReedR1/content-ids.tsv',rows.map(r=>r.slice(0,2).join('\t')).join('\n'));
const p='Assets/01.Scenes/Scenario/OpenWorld/Regions/SaltwindHarborRegion.unity';
const text=fs.readFileSync(p,'utf8');
const contracts=text.split('\n--- !u!').filter(b=>/(_guid:|_mapId:|_arrivalId:|_destinationMap|_ownerKey:)/.test(b));
fs.writeFileSync('output/HarborReedR1/content-contracts.json',JSON.stringify({host:rows,region:p,regionSha256:crypto.createHash('sha256').update(text).digest('hex'),serializedRegionContracts:contracts,notes:'디스크 계약. 실제 연결 및 물리 검사는 별도.'},null,2));
console.log({hostIds:rows.length,regionBlocks:contracts.length});
