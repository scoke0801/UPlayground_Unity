using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Cinematic;
using UPlayGround.Manager;

namespace UPlayGround.Data
{
    internal static class CinematicStageRuntimeUtility
    {
        public static bool TryEnter(
            CinematicStageSO stage,
            UnityEngine.Object owner,
            GameObject caster,
            GameObject target,
            out CinematicStageTicket ticket)
        {
            ticket = default;
            ICinematicStageService service = Svc.CinematicStage;
            if (service == null || stage == null || caster == null)
                return false;

            Transform casterModel = ResolveModelRoot(caster);
            Transform targetModel = ResolveModelRoot(target);
            if (casterModel == null)
                return false;

            var request = new CinematicStageRequest(
                stage,
                owner,
                caster,
                casterModel,
                target,
                targetModel);
            return service.TryEnter(request, out ticket);
        }

        /// <summary>범위 판정으로 확정된 대상 전체를 한 번에 무대로 옮긴다. 첫 항목이 주 대상이다.</summary>
        public static bool TryEnterWithTargets(
            CinematicStageSO stage,
            UnityEngine.Object owner,
            GameObject caster,
            IReadOnlyList<GameObject> targets,
            out CinematicStageTicket ticket)
        {
            ticket = default;
            ICinematicStageService service = Svc.CinematicStage;
            if (service == null || stage == null || caster == null)
                return false;

            Transform casterModel = ResolveModelRoot(caster);
            if (casterModel == null)
                return false;

            var stageTargets = new List<CinematicStageTarget>(targets?.Count ?? 0);
            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    GameObject target = targets[i];
                    if (target == null)
                        continue;
                    stageTargets.Add(
                        new CinematicStageTarget(target, ResolveModelRoot(target)));
                }
            }

            var request = new CinematicStageRequest(
                stage,
                owner,
                caster,
                casterModel,
                stageTargets);
            return service.TryEnter(request, out ticket);
        }

        public static Transform ResolveModelRoot(GameObject actor)
        {
            if (actor == null)
                return null;

            CharacterModelData characterModel =
                actor.GetComponentInChildren<CharacterModelData>(false);
            if (characterModel != null)
                return characterModel.transform;

            ActorAnimator actorAnimator = actor.GetComponentInChildren<ActorAnimator>(false);
            if (actorAnimator != null)
                return actorAnimator.transform;

            Animator animator = actor.GetComponentInChildren<Animator>(false);
            return animator != null ? animator.transform : actor.transform;
        }

        public static void SetLayerRecursively(GameObject instance, string layerName)
        {
            if (instance == null)
                return;

            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
                return;

            SetLayerRecursively(instance.transform, layer);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }
    }
}
