import fs from 'node:fs';
import crypto from 'node:crypto';

const root = 'output/RegionalSky/Followup/';
const scene = 'Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const before = fs.readFileSync(root + 'baseline/LakeVillageWorld.before-integration.unity', 'utf8');
const after = fs.readFileSync(scene, 'utf8');
const parse = source => {
    const heads = [...source.matchAll(/^--- !u!(\d+) &(-?\d+).*$/gm)];
    return new Map(heads.map((h, i) => [h[2], { type: h[1], text: source.slice(h.index, heads[i + 1]?.index ?? source.length) }]));
};
const oldDocs = parse(before), newDocs = parse(after);
const changed = [], added = [], removed = [];
for (const [id, doc] of oldDocs) {
    const current = newDocs.get(id);
    if (!current) { removed.push(id); continue; }
    if (doc.text !== current.text) {
        const normalizedSame = doc.text.replaceAll('\r\n', '\n') === current.text.replaceAll('\r\n', '\n');
        const oldLines = new Set(doc.text.split(/\r?\n/));
        const newLines = new Set(current.text.split(/\r?\n/));
        changed.push({ id, type: doc.type, normalizedSame,
            removedLines: [...oldLines].filter(l => !newLines.has(l)).slice(0, 20),
            addedLines: [...newLines].filter(l => !oldLines.has(l)).slice(0, 30) });
    }
}
for (const [id, doc] of newDocs) if (!oldDocs.has(id)) added.push({id,type:doc.type,text:doc.text});
const result = { scene, beforeSha256:crypto.createHash('sha256').update(before).digest('hex'), afterSha256:crypto.createHash('sha256').update(after).digest('hex'), beforeDocuments:oldDocs.size, afterDocuments:newDocs.size, changed, added, removed };
fs.writeFileSync(root + 'integration-scene-diff.json', JSON.stringify(result, null, 2));
console.log(JSON.stringify({ ...result, changed:changed.slice(0, 25), added:added.map(d=>({id:d.id,type:d.type})), changedCount:changed.length }, null, 2));
