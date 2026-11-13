using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Data.Save;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>개체와 Terrain 셀의 표현을 기존 단일 반응 작성자에 연결한다.</summary>
    public interface IEnvironmentReactionTarget
    {
        Transform transform { get; }
        Transform OcclusionRoot { get; }
        bool IsAvailable { get; }
        string StableId { get; }
        string PersistenceSignature { get; }
        ReactiveEnvironmentDefinitionSO Definition { get; }
        Vector3 ReactionPosition { get; }
        Vector2 Wind { get; }
        bool IsAuthoringValid();
        void SampleRain(out float rain, out float exposure);
        void ApplySnapshot(EnvironmentReactionState state, EnvironmentDestructionSaveState destruction = null);
        void FixedTick(float deltaTime);
    }
}
