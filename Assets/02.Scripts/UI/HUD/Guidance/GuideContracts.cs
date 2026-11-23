using UnityEngine;
using UPlayGround.Data.UI;

namespace UPlayGround.UI.Guidance
{
    /// <summary>두 지점 사이의 안내 경로를 계산한다. 길을 확인하지 못하면 false를 돌려주고 직선으로 대신하지 않는다.</summary>
    public interface IGuidePathProvider
    {
        bool TryBuild(Vector3 from, Vector3 to, GuidePath result);
    }

    /// <summary>연출이 한 번 재생되는 동안 필요한 입력을 묶는다. 연출은 퀘스트·NavMesh·입력을 알지 못한다.</summary>
    public readonly struct GuideContext
    {
        public GuideContext(Transform player, Transform effectRoot, GuidePath path, GuideProfileSO profile)
        {
            Player = player;
            EffectRoot = effectRoot;
            Path = path;
            Profile = profile;
        }

        /// <summary>안내를 받는 플레이어.</summary>
        public Transform Player { get; }

        /// <summary>연출 개체를 풀링해 둘 부모. 연출이 소유하지 않고 컨트롤러가 정리한다.</summary>
        public Transform EffectRoot { get; }

        /// <summary>따라갈 길. 항상 완성된 경로다(길을 확인하지 못하면 연출을 시작하지 않는다).</summary>
        public GuidePath Path { get; }

        public GuideProfileSO Profile { get; }
    }

    /// <summary>길안내 연출 한 종류(나비·바람 등)의 계약. 컨트롤러는 이 인터페이스만 호출한다.</summary>
    public interface IGuideEffect
    {
        bool IsPlaying { get; }

        /// <summary>경로를 따라 안내하는 연출을 시작한다.</summary>
        void Begin(in GuideContext context);

        /// <summary>재생 중 경로가 다시 계산됐을 때 새 경로로 갈아 끼운다. 개체는 끊김 없이 이어서 난다.</summary>
        void ReplacePath(GuidePath path);

        void Tick(float deltaTime);

        /// <summary>immediate가 false면 페이드아웃 후 끝나고, true면 즉시 정리한다.</summary>
        void Stop(bool immediate);

        /// <summary>풀링한 개체와 리소스를 모두 해제한다.</summary>
        void Dispose();
    }
}
