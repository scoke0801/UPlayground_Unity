using System.Runtime.InteropServices;
using UnityEngine;

namespace UPlayGround.Gameplay.World
{
    /// <summary>셰이더·업로드·GPU 회귀 검사에서 공유하는 16바이트 정렬 레코드다.</summary>
    public static class WaterGpuRecords
    {
        public const int MaxBodies = 16;
        public const int MaxCells = 512;
        public const int MaxDryVolumes = 128;
        [StructLayout(LayoutKind.Sequential)]
        public struct Body
        {
            public Matrix4x4 WorldToLocal;
            public Vector4 VolumeMin, VolumeMax;
            public Vector4 Scattering, Extinction, Caustic, Wave0, Wave1;
            public Vector4 Shallow, Deep, Foam, Style0, Style1, Style2;
            public Vector4 Ripple, Rain, Range, Authority, Flow;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Cell { public Vector4 A, B, C; }
        [StructLayout(LayoutKind.Sequential)]
        public struct DryVolume { public Vector4 Min, Max; }
        [StructLayout(LayoutKind.Sequential)]
        public struct Stamp { public Vector4 PositionTime, Parameters; }
    }
}
