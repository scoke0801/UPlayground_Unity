using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UPlayGround.Components;
using UPlayGround.Data.Actor;

namespace UPlayGround.Gameplay.World
{
    public sealed class SandFootprintWorld : MonoBehaviour
    {
        public TerrainCollider receiver;
        public TerrainCollider[] adjoiningReceivers = System.Array.Empty<TerrainCollider>();
        public Collider meshReceiver;
        public Texture2D receiverMask;
        public SandFootprintProfileSO profile;
        public ComputeShader stampShader;
        public Transform trackingAnchor;

        public static SandFootprintWorld Active { get; private set; }
        public int Accepted { get; private set; }
        public int Dropped { get; private set; }
        public int LastFrameDispatches { get; private set; }

        internal const string CacheProperty = "_UPGSandCache";
        internal const string EnabledProperty = "_UPGSandEnabled";
        internal const float TileSize = 8f;
        private const int Halo = 2;
        private const int MaxTileCount = 25;
        private const int Budget = 16;

        private struct Stamp
        {
            public Vector2 Center;
            public Vector4 Inverse;
            public Vector2 Extent;
            public Vector2 Pressure;
            public double Clock;
            public int Frame;
            public float GameTime;
        }

        private readonly Stamp[] _pending = new Stamp[Budget];
        private readonly Vector2Int[] _keys = new Vector2Int[MaxTileCount];
        private readonly bool[] _valid = new bool[MaxTileCount];
        private readonly bool[] _written = new bool[MaxTileCount];
        private readonly int[] _epoch = new int[MaxTileCount];
        private readonly Vector4[] _tiles = new Vector4[MaxTileCount];
        private readonly bool[] _retained = new bool[MaxTileCount];
        private readonly int[] _rect = new int[4];
        private RenderTexture _cache, _scratch;
        private int _update, _copy, _count, _npcCount;
        private int _lastFrame = -1;
        private double _clock;
        private float _lastGameTime;
        private Vector2Int _minTile;
        private Vector4 _receiverBounds;
        private Vector3 _receiverPosition;
        private Vector2 _logicalOffset;
        private int _grid, _interior, _resolution;
        private float _texel;
        private Vector4 _viewRange;
        private SandFootprintProfileSO.Quality _quality;

        internal RTHandle CacheHandle { get; private set; }
        internal RTHandle ScratchHandle { get; private set; }
        private Collider Surface => receiver != null ? receiver : meshReceiver;
        public long AllocatedTextureBytes => _cache == null ? 0L
            : (long)_resolution * _resolution * (_grid * _grid + 1) * 4;

        private void OnEnable()
        {
            if (Active != null || Surface == null || receiverMask == null || !receiverMask.isReadable
                || receiverMask.format != TextureFormat.R8 || profile == null || stampShader == null
                || trackingAnchor == null || Surface.transform.rotation != Quaternion.identity
                || Surface.transform.lossyScale != Vector3.one
                || Surface.bounds.size.x <= 0 || Surface.bounds.size.z <= 0
                || !SystemInfo.supportsComputeShaders || !SystemInfo.supports2DArrayTextures
                || SystemInfo.maxTextureSize < 1028 || SystemInfo.maxTextureArraySlices < MaxTileCount
                || !SystemInfo.IsFormatSupported(GraphicsFormat.R16G16_SFloat, GraphicsFormatUsage.Sample)
                || !SystemInfo.IsFormatSupported(GraphicsFormat.R16G16_SFloat, GraphicsFormatUsage.LoadStore)
                || !SystemInfo.IsFormatSupported(GraphicsFormat.R16G16_SFloat, GraphicsFormatUsage.Linear)
                || !TryGetReceiverBounds(out _receiverBounds))
            {
                Debug.LogWarning("모래 발자국을 초기화할 수 없습니다.", this);
                return;
            }
            Active = this;
            _logicalOffset = default;
            _receiverPosition = Surface.transform.position;
            ConfigureQuality();
            _lastGameTime = Time.time;
            _update = stampShader.FindKernel("UpdateTile");
            _copy = stampShader.FindKernel("CopyScratchToTile");
            ActorFootstep.SandContact += Receive;
        }

        private bool TryGetReceiverBounds(out Vector4 maskBounds)
        {
            maskBounds = default;
            if (receiver == null)
            {
                Bounds mesh = meshReceiver.bounds;
                maskBounds = new Vector4(mesh.min.x, mesh.min.z, 1f / mesh.size.x, 1f / mesh.size.z);
                return adjoiningReceivers.Length == 0;
            }
            TerrainData data = receiver.terrainData;
            if (data == null) return false;
            Vector3 min = receiver.transform.position;
            Vector3 max = min + data.size;
            foreach (var tile in adjoiningReceivers)
            {
                if (tile == null || tile.terrainData == null || tile.terrainData.size != data.size
                    || tile.terrainData.alphamapWidth != data.alphamapWidth
                    || tile.terrainData.alphamapHeight != data.alphamapHeight
                    || tile.transform.rotation != Quaternion.identity || tile.transform.lossyScale != Vector3.one)
                    return false;
                Vector3 origin = tile.transform.position;
                min = Vector3.Min(min, origin);
                max = Vector3.Max(max, origin + data.size);
            }
            Vector3 size = max - min;
            int tilesX = Mathf.RoundToInt(size.x / data.size.x);
            int tilesZ = Mathf.RoundToInt(size.z / data.size.z);
            maskBounds = new Vector4(min.x, min.z, 1f / size.x, 1f / size.z);
            return receiverMask.width == tilesX * (data.alphamapWidth - 1) + 1
                && receiverMask.height == tilesZ * (data.alphamapHeight - 1) + 1;
        }

        private void SynchronizeReceiverOrigin()
        {
            Vector3 shift = Surface.transform.position - _receiverPosition;
            if (shift == Vector3.zero) return;
            _logicalOffset -= new Vector2(shift.x, shift.z);
            _receiverPosition = Surface.transform.position;
            _receiverBounds.x += shift.x;
            _receiverBounds.y += shift.z;
        }

        private void OnDisable()
        {
            if (Active != this) return;
            ActorFootstep.SandContact -= Receive;
            Active = null;
            ReleaseBuffers();
            Clear();
        }

        private void ConfigureQuality()
        {
            _quality = profile.quality;
            _grid = _quality == SandFootprintProfileSO.Quality.Extended ? 5 : 3;
            _interior = _quality == SandFootprintProfileSO.Quality.Low ? 512 : 1024;
            _resolution = _interior + Halo * 2;
            _texel = TileSize / _interior;
        }

        private void ReleaseBuffers()
        {
            Shader.SetGlobalFloat(EnabledProperty, 0);
            CacheHandle?.Release();
            ScratchHandle?.Release();
            CacheHandle = ScratchHandle = null;
            Release(ref _cache);
            Release(ref _scratch);
        }

        private static void Release(ref RenderTexture texture)
        {
            if (texture == null) return;
            texture.Release();
            Destroy(texture);
            texture = null;
        }

        [ContextMenu("발자국 지우기")]
        public void Clear()
        {
            System.Array.Clear(_valid, 0, MaxTileCount);
            System.Array.Clear(_written, 0, MaxTileCount);
            _lastFrame = -1;
            _count = _npcCount = 0;
        }

#if UNITY_EDITOR
        [ContextMenu("플레이어 앞에 양발 테스트 자국")]
        private void StampAtAnchor()
        {
            if (!Application.isPlaying || Active != this) return;
            Vector3 forward = trackingAnchor.forward;
            for (int i = 0; i < 2; i++)
            {
                Vector3 origin = trackingAnchor.position + Vector3.up * 1.5f + forward * (1.2f + i * 0.32f)
                    + trackingAnchor.right * (i == 0 ? -0.13f : 0.13f);
                if (Physics.Raycast(origin, Vector3.down, out var hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    Receive(hit, forward, new Vector2(0.13f, 0.29f), FootstepKind.Landing, i == 0, true);
            }
        }
#endif

        private void AdvanceClock()
        {
            float now = Time.time;
            _clock += Mathf.Max(0, now - _lastGameTime) * profile.windDecay / Mathf.Max(0.1f, profile.halfLife);
            _lastGameTime = now;
        }

        private void Receive(RaycastHit hit, Vector3 forward, Vector2 size, FootstepKind kind, bool left, bool player)
        {
            var collider = hit.collider;
            if (collider == null || hit.normal.y < 0.643f ||
                (collider != Surface && (!(collider is TerrainCollider tile) || System.Array.IndexOf(adjoiningReceivers, tile) < 0)))
                return;
            if (!player && (_npcCount >= profile.npcStampsPerFrame || trackingAnchor == null
                || (trackingAnchor.position - hit.point).sqrMagnitude > profile.npcRadius * profile.npcRadius))
                return;
            SynchronizeReceiverOrigin();
            if (Weight(hit.point) < 0.5f) return;
            if (!TryProject(hit.normal, forward, size, left, out Vector4 inverse, out Vector2 extent)) return;
            if (_count >= Budget) { Dropped++; return; }
            AdvanceClock();
            _pending[_count++] = new Stamp
            {
                Center = new Vector2(hit.point.x, hit.point.z) + _logicalOffset,
                Inverse = inverse,
                Extent = extent,
                Pressure = profile.Pressure(kind),
                Clock = _clock,
                Frame = Time.frameCount,
                GameTime = Time.time
            };
            if (!player) _npcCount++;
            Accepted++;
        }

        internal static bool TryProject(Vector3 normal, Vector3 forward, Vector2 size, bool left,
            out Vector4 inverse, out Vector2 extent)
        {
            Vector3 f = Vector3.ProjectOnPlane(forward, normal).normalized;
            Vector3 r = Vector3.Cross(normal, f) * (left ? -size.x : size.x);
            f *= size.y;
            float determinant = r.x * f.z - f.x * r.z;
            inverse = default;
            extent = default;
            if (!float.IsFinite(determinant) || Mathf.Abs(determinant) < 0.001f) return false;
            inverse = new Vector4(f.z, -f.x, -r.z, r.x) / determinant;
            extent = new Vector2(Mathf.Abs(r.x) + Mathf.Abs(f.x), Mathf.Abs(r.z) + Mathf.Abs(f.z)) * 0.65f;
            return true;
        }

        public float Weight(Vector3 point)
        {
            float u = (point.x - _receiverBounds.x) * _receiverBounds.z;
            float v = (point.z - _receiverBounds.y) * _receiverBounds.w;
            if (u < 0 || u > 1 || v < 0 || v > 1 || receiverMask == null) return 0;
            int w = receiverMask.width, h = receiverMask.height;
            float x = u * (w - 1), z = v * (h - 1);
            int ix = Mathf.FloorToInt(x), iz = Mathf.FloorToInt(z);
            int nx = Mathf.Min(ix + 1, w - 1), nz = Mathf.Min(iz + 1, h - 1);
            var pixels = receiverMask.GetRawTextureData<byte>();
            return Mathf.Lerp(Mathf.Lerp(pixels[iz * w + ix], pixels[iz * w + nx], x - ix),
                Mathf.Lerp(pixels[nz * w + ix], pixels[nz * w + nx], x - ix), z - iz) / 255f;
        }

        internal static int TileIndex(int coordinate, int grid) => (coordinate % grid + grid) % grid;
        internal static int TileCoordinate(float world) => Mathf.FloorToInt(world / TileSize);
        internal static float Decay(double clockDelta) => Mathf.Pow(2, (float)-clockDelta);

        internal bool Prepare()
        {
            if (Surface == null || trackingAnchor == null || profile == null) return false;
            if (_quality != profile.quality)
            {
                ReleaseBuffers();
                Clear();
                ConfigureQuality();
            }
            if (_cache != null && !_cache.IsCreated())
            {
                OnDisable();
                OnEnable();
                return false;
            }
            if (_cache == null && _count > 0)
            {
                _cache = MakeTexture("모래 발자국 캐시", _grid * _grid);
                _scratch = MakeTexture("모래 발자국 작업 버퍼", 1);
                if (!_cache.IsCreated() || !_scratch.IsCreated())
                {
                    OnDisable();
                    return false;
                }
                CacheHandle = RTHandles.Alloc(_cache);
                ScratchHandle = RTHandles.Alloc(_scratch);
            }
            return _cache != null;
        }

        private RenderTexture MakeTexture(string name, int slices)
        {
            var descriptor = new RenderTextureDescriptor(_resolution, _resolution, GraphicsFormat.R16G16_SFloat, 0)
            {
                dimension = slices > 1 ? TextureDimension.Tex2DArray : TextureDimension.Tex2D,
                volumeDepth = slices, enableRandomWrite = true, msaaSamples = 1, useMipMap = false
            };
            var texture = new RenderTexture(descriptor)
            {
                name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            texture.Create();
            return texture;
        }

        internal void Record(CommandBuffer cmd, bool owner)
        {
            if (!owner || _lastFrame == Time.frameCount)
            {
                Publish(cmd);
                return;
            }
            LastFrameDispatches = 0;
            SynchronizeReceiverOrigin();
            _lastFrame = Time.frameCount;
            AdvanceClock();
            Vector3 worldAnchor = trackingAnchor.position;
            Vector2 anchor = new Vector2(worldAnchor.x, worldAnchor.z) + _logicalOffset;
            int center = _grid / 2;
            _minTile = new Vector2Int(TileCoordinate(anchor.x) - center, TileCoordinate(anchor.y) - center);
            _viewRange = new Vector4(anchor.x, anchor.y, _grid == 3 ? 6f : 12f, _grid == 3 ? 7f : 14f);
            int epoch = (int)System.Math.Floor(_clock);

            for (int z = _minTile.y; z < _minTile.y + _grid; z++)
            for (int x = _minTile.x; x < _minTile.x + _grid; x++)
            {
                int slot = Slot(x, z);
                _retained[slot] = _valid[slot] && _keys[slot] == new Vector2Int(x, z);
            }
            for (int z = _minTile.y; z < _minTile.y + _grid; z++)
            for (int x = _minTile.x; x < _minTile.x + _grid; x++)
            {
                var key = new Vector2Int(x, z);
                int slot = Slot(x, z);
                _valid[slot] = true;
                _keys[slot] = key;
                if (!_retained[slot])
                {
                    _written[slot] = RebuildHalo(cmd, key, slot, epoch);
                    _epoch[slot] = epoch;
                }
            }
            for (int z = _minTile.y; z < _minTile.y + _grid; z++)
            for (int x = _minTile.x; x < _minTile.x + _grid; x++)
            {
                int slot = Slot(x, z);
                Vector2 start = new Vector2(x, z) * TileSize;
                for (int i = 0; i < _count; i++)
                {
                    Stamp stamp = _pending[i];
                    if (_lastFrame - stamp.Frame > 2 || Time.time - stamp.GameTime > 0.1f) continue;
                    Vector2 low = (stamp.Center - stamp.Extent - start) / _texel + Vector2.one * (Halo - 0.5f);
                    Vector2 high = (stamp.Center + stamp.Extent - start) / _texel + Vector2.one * (Halo - 0.5f);
                    int x0 = Mathf.Max(0, Mathf.FloorToInt(low.x)), z0 = Mathf.Max(0, Mathf.FloorToInt(low.y));
                    int x1 = Mathf.Min(_resolution, Mathf.CeilToInt(high.x) + 1), z1 = Mathf.Min(_resolution, Mathf.CeilToInt(high.y) + 1);
                    if (x0 >= x1 || z0 >= z1) continue;
                    if (!_written[slot])
                        Dispatch(cmd, slot, 0, 0, _resolution, _resolution, 0, 1);
                    else if (_epoch[slot] != epoch)
                        Dispatch(cmd, slot, 0, 0, _resolution, _resolution, 1, Decay(epoch - _epoch[slot]));
                    _written[slot] = true;
                    _epoch[slot] = epoch;
                    cmd.SetComputeVectorParam(stampShader, "_TileOrigin", start);
                    cmd.SetComputeVectorParam(stampShader, "_Center", stamp.Center);
                    cmd.SetComputeVectorParam(stampShader, "_InverseBasis", stamp.Inverse);
                    cmd.SetComputeVectorParam(stampShader, "_Strength", stamp.Pressure * Decay(stamp.Clock - epoch));
                    Dispatch(cmd, slot, x0, z0, x1 - x0, z1 - z0, 2, 1);
                }
                _tiles[slot] = new Vector4(start.x, start.y, Decay(_clock - _epoch[slot]), _written[slot] ? 1 : 0);
            }
            _count = _npcCount = 0;
            Publish(cmd);
        }

        private int Slot(int x, int z) => TileIndex(z, _grid) * _grid + TileIndex(x, _grid);

        private void Publish(CommandBuffer cmd)
        {
            cmd.SetGlobalFloat(EnabledProperty, _lastFrame < 0 ? 0 : 1);
            if (_lastFrame < 0) return;
            int w = receiverMask.width, h = receiverMask.height;
            cmd.SetGlobalTexture(CacheProperty, _cache);
            cmd.SetGlobalTexture("_UPGSandMask", receiverMask);
            cmd.SetGlobalVectorArray("_UPGSandTiles", _tiles);
            cmd.SetGlobalVector("_UPGSandMinTile", new Vector4(_minTile.x, _minTile.y, _grid, 0));
            cmd.SetGlobalVector("_UPGSandReceiver", _receiverBounds);
            cmd.SetGlobalVector("_UPGSandLogicalOffset", _logicalOffset);
            cmd.SetGlobalVector("_UPGSandGeometry", new Vector4(TileSize, _texel, _interior, 1f / _resolution));
            cmd.SetGlobalVector("_UPGSandView", _viewRange);
            cmd.SetGlobalVector("_UPGSandRelief", new Vector4(profile.depression, profile.rim, profile.colorContrast, profile.smoothness));
            cmd.SetGlobalVector("_UPGSandMaskSize", new Vector4(w, h, 1f / w, 1f / h));
        }

        private bool RebuildHalo(CommandBuffer cmd, Vector2Int key, int slot, int epoch)
        {
            bool hasNeighbor = false;
            Vector4 slots0 = default, slots1 = default, decays0 = default, decays1 = default;
            int index = 0;
            for (int z = -1; z <= 1; z++)
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && z == 0) continue;
                int nx = key.x + x, nz = key.y + z;
                int neighbor = Slot(nx, nz);
                bool valid = _retained[neighbor] && _written[neighbor] && _keys[neighbor] == new Vector2Int(nx, nz);
                int source = valid ? neighbor : -1;
                float decay = valid ? Decay(epoch - _epoch[neighbor]) : 0;
                if (index < 4) { slots0[index] = source; decays0[index] = decay; }
                else { slots1[index - 4] = source; decays1[index - 4] = decay; }
                hasNeighbor |= valid;
                index++;
            }
            if (!hasNeighbor) return false;
            cmd.SetComputeVectorParam(stampShader, "_RebuildSlots0", slots0);
            cmd.SetComputeVectorParam(stampShader, "_RebuildSlots1", slots1);
            cmd.SetComputeVectorParam(stampShader, "_RebuildDecay0", decays0);
            cmd.SetComputeVectorParam(stampShader, "_RebuildDecay1", decays1);
            cmd.SetComputeIntParam(stampShader, "_TileResolution", _resolution);
            cmd.SetComputeIntParam(stampShader, "_Interior", _interior);
            Dispatch(cmd, slot, 0, 0, _resolution, _resolution, 3, 1);
            return true;
        }

        private void Dispatch(CommandBuffer cmd, int slot, int x, int z, int width, int height, int mode, float decay)
        {
            _rect[0] = x;
            _rect[1] = z;
            _rect[2] = width;
            _rect[3] = height;
            cmd.SetComputeIntParams(stampShader, "_Rect", _rect);
            cmd.SetComputeIntParam(stampShader, "_Slot", slot);
            cmd.SetComputeIntParam(stampShader, "_Mode", mode);
            cmd.SetComputeFloatParam(stampShader, "_Decay", decay);
            cmd.SetComputeFloatParam(stampShader, "_PixelSize", _texel);
            cmd.SetComputeTextureParam(stampShader, _update, "_CacheRead", _cache);
            cmd.SetComputeTextureParam(stampShader, _update, "_ScratchWrite", _scratch);
            cmd.DispatchCompute(stampShader, _update, (width + 7) / 8, (height + 7) / 8, 1);
            cmd.SetComputeTextureParam(stampShader, _copy, "_ScratchRead", _scratch);
            cmd.SetComputeTextureParam(stampShader, _copy, "_CacheWrite", _cache);
            cmd.DispatchCompute(stampShader, _copy, (width + 7) / 8, (height + 7) / 8, 1);
            LastFrameDispatches += 2;
        }
    }
}
