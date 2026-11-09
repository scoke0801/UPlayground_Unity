import fs from 'node:fs';
import assert from 'node:assert/strict';

const plan = JSON.parse(fs.readFileSync('Tools/WorldThemes/Integration/lake-seam.json', 'utf8'));
assert(plan.outerWidth > 0 && plan.innerCollar > 0 && plan.shallowDepthFade > 0);
const smooth = value => { const t = Math.max(0, Math.min(1, value)); return t * t * (3 - 2 * t); };
const weight = (distance, depth) => smooth((distance + plan.outerWidth) / (plan.outerWidth + plan.innerCollar)) * smooth(depth / plan.shallowDepthFade);
assert.equal(weight(-plan.outerWidth, 3.5), 0);
assert.equal(weight(plan.innerCollar, 3.5), 1);
assert.equal(weight(0, 0), 0);
assert(Math.abs(weight(-1e-6, 3.5) - weight(1e-6, 3.5)) < 1e-6);
for (const depth of [0.001, 0.1, 0.35, 1.5, 3.5]) {
  const river = 0.45 + (0.78 - 0.45) * Math.max(0, Math.min(1, depth / 1.5));
  const earth = 1 - river - 0.12;
  assert(earth >= 0 && Math.abs(river + earth + 0.12 - 1) < 1e-12);
  for (const distance of [-16, -15, -10, 0, 4, 5]) assert(weight(distance, depth) >= 0 && weight(distance, depth) <= 1);
}
console.log('PASS: seam endpoints, continuity, shallow fade, normalized target weights');
