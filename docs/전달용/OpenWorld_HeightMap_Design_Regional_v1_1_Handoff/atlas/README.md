# Open-World: HeightMap & Individual Region Concepts

Contains 8 separate image deliverables, including a new procedural 2049x2049 16-bit HeightMap and 7 regional concept illustrations.

- 01_Heightmap_Visual_Concept_Reference.png: artistic grayscale reference image; NOT numerically calibrated for terrain import.
- 01_World_Heightmap_2049_16bit.png: technical elevation image, single-channel 16-bit integer. Domain 0–2800 meters encoded as 0–65535.
- 01_World_Heightmap_2049_R16LE.raw: same 2049x2049 height data, 16-bit little-endian, row-major, no header. Unity RAW import settings must match dimensions, depth and byte order.
- 01_World_Heightmap_Preview.jpg: display preview only. Do not import it as terrain height data.
- 02_Dense_Forest.png: dense forest concept (independent generated image).
- 03_Autumn_Forest.png: autumn forest concept (independent generated image).
- 04_Arid_Badlands.png: arid badlands concept (independent generated image).
- 05_Plains_Castle.png: castle plains concept (independent generated image).
- 06_High_Alpine.png: high alpine concept (independent generated image).
- 07_Riverside_Village.png: crop of shared open-world panorama, upscaled for separate concept card.
- 08_Lakeside_Village.png: crop of shared open-world panorama, upscaled for separate concept card.

Caveats: The generated HeightMap follows the broad intended climate/topography arrangement, but the images are conceptual references rather than photogrammetrically aligned screenshots from the same heightfield. Building structures, forests, arches, water surfaces, waterfalls and settlements are not encoded in the heightmap and need separate Unity content/placement layers.
