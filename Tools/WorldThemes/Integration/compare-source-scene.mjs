import fs from 'node:fs';
import crypto from 'node:crypto';

const previous = 'output/WorldThemeProduction/A00/backup/LakeVillageWorld.unity';
const current = 'Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const split = path => {
  const text = fs.readFileSync(path, 'utf8');
  const headers = [...text.matchAll(/^--- !u!(\d+) &(-?\d+).*$/gm)];
  return new Map(headers.map((match, index) => [match[2], {
    type: match[1], text: text.slice(match.index, headers[index + 1]?.index ?? text.length)
  }]));
};
const before = split(previous);
const after = split(current);
const changes = [];
for (const [id, doc] of before) {
  const next = after.get(id);
  if (!next) { changes.push({ id, type: doc.type, kind: 'removed' }); continue; }
  if (doc.text === next.text) continue;
  const oldLines = doc.text.split(/\r?\n/);
  const newLines = next.text.split(/\r?\n/);
  changes.push({ id, type: doc.type, kind: 'changed',
    removedLines: oldLines.filter(line => !newLines.includes(line)),
    addedLines: newLines.filter(line => !oldLines.includes(line)) });
}
for (const [id, doc] of after) if (!before.has(id)) changes.push({ id, type: doc.type, kind: 'added' });
const result = {
  before: previous, after: current,
  beforeSha256: crypto.createHash('sha256').update(fs.readFileSync(previous)).digest('hex'),
  afterSha256: crypto.createHash('sha256').update(fs.readFileSync(current)).digest('hex'),
  beforeDocuments: before.size, afterDocuments: after.size, changes
};
fs.writeFileSync('output/WorldThemeProduction/A00/source-scene-drift.json', JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
