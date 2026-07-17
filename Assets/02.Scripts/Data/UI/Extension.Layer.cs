using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround
{
    public static class LayerExtensions
    {
        public static InputLayer ToInputLayer(this CanvasLayer canvasLayer)
        {
            // 정수 값이 일치하므로 바로 캐스팅
            return (InputLayer)(int)canvasLayer;
        }

        public static CanvasLayer ToCanvasLayer(this InputLayer inputLayer)
        {
            // 정수 값이 일치하므로 바로 캐스팅
            return (CanvasLayer)(int)inputLayer;
        }
    }
}
