using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace Game.Input
{
    /// <summary>
    /// DualSense USB HID 입력 리포트 구조체
    /// 터치패드 데이터를 포함한 전체 HID 리포트
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct DualSenseTouchpadReport : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('H', 'I', 'D');

        // 리포트 ID
        [FieldOffset(0)] public byte reportId;
        
        // 아날로그 스틱 (1-4)
        [FieldOffset(1)] public byte leftStickX;
        [FieldOffset(2)] public byte leftStickY;
        [FieldOffset(3)] public byte rightStickX;
        [FieldOffset(4)] public byte rightStickY;
        
        // 버튼 데이터 (5-7)
        [FieldOffset(5)] public byte buttons1;
        [FieldOffset(6)] public byte buttons2;
        [FieldOffset(7)] public byte buttons3;
        
        // 트리거 (8-9)
        [FieldOffset(8)] public byte leftTrigger;
        [FieldOffset(9)] public byte rightTrigger;
        
        // 시퀀스 번호 (10)
        [FieldOffset(10)] public byte sequenceNumber;
        
        // 자이로/가속도계 데이터 (16-27)
        [FieldOffset(16)] public short gyroX;
        [FieldOffset(18)] public short gyroY;
        [FieldOffset(20)] public short gyroZ;
        [FieldOffset(22)] public short accelX;
        [FieldOffset(24)] public short accelY;
        [FieldOffset(26)] public short accelZ;
        
        // 터치패드 데이터 (33-40)
        // 첫 번째 터치 (33-36)
        [FieldOffset(33)] public byte touch1;        // 최상위 비트 = active flag (0=active, 1=inactive)
        [FieldOffset(34)] public byte touch1Data1;   // X 하위 8비트
        [FieldOffset(35)] public byte touch1Data2;   // X 상위 4비트 (하위) + Y 하위 4비트 (상위)
        [FieldOffset(36)] public byte touch1Data3;   // Y 상위 8비트
        
        // 두 번째 터치 (37-40)
        [FieldOffset(37)] public byte touch2;
        [FieldOffset(38)] public byte touch2Data1;
        [FieldOffset(39)] public byte touch2Data2;
        [FieldOffset(40)] public byte touch2Data3;
        
        /// <summary>
        /// 첫 번째 터치가 활성화되어 있는지 확인
        /// </summary>
        public bool IsTouch1Active => (touch1 & 0x80) == 0;
        
        /// <summary>
        /// 두 번째 터치가 활성화되어 있는지 확인
        /// </summary>
        public bool IsTouch2Active => (touch2 & 0x80) == 0;
        
        /// <summary>
        /// 첫 번째 터치의 X 좌표 (0-1920)
        /// </summary>
        public int Touch1X => ((touch1Data2 & 0x0F) << 8) | touch1Data1;
        
        /// <summary>
        /// 첫 번째 터치의 Y 좌표 (0-1080)
        /// </summary>
        public int Touch1Y => (touch1Data3 << 4) | ((touch1Data2 & 0xF0) >> 4);
        
        /// <summary>
        /// 두 번째 터치의 X 좌표 (0-1920)
        /// </summary>
        public int Touch2X => ((touch2Data2 & 0x0F) << 8) | touch2Data1;
        
        /// <summary>
        /// 두 번째 터치의 Y 좌표 (0-1080)
        /// </summary>
        public int Touch2Y => (touch2Data3 << 4) | ((touch2Data2 & 0xF0) >> 4);
    }
}
