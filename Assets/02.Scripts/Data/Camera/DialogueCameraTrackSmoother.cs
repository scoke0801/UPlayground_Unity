using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data
{
    /// <summary>
    /// 사람이 몬 카메라 녹화의 손떨림(고주파 노이즈)을 제거하는 오프라인 스무딩.
    ///
    /// 설계 근거:
    /// - 전체 트랙을 이미 보유하므로 <b>비인과(centered) 필터</b>를 쓸 수 있다 → 위상 지연 0.
    ///   (One Euro 같은 인과 필터는 미래를 못 보는 실시간용이라 여기선 불필요/열등하다.)
    /// - 저장된 <b>앵커 로컬 공간 그대로</b> 필터링한다. 월드로 풀지 않으므로 앵커 이동과 손떨림이 분리된다.
    /// - 회전은 euler 성분을 평균하면 wrap/짐벌에서 깨진다 → quaternion으로 변환,
    ///   기준(중앙) 샘플과 <b>헤미스피어 정렬</b>(dot&lt;0이면 부호 반전) 후 가중 평균/정규화.
    /// - 엔드포인트는 창을 좌우 대칭으로 줄여 처리 → sample[0]/마지막 샘플은 이동하지 않음
    ///   (sample[0]은 진입 블렌드 타깃이므로 보존 필수).
    ///
    /// 키 리덕션/압축은 별개 문제(데이터 크기)라 여기서 다루지 않는다.
    /// </summary>
    public static class DialogueCameraTrackSmoother
    {
        public const int MaxRadius = 6; // strength=1일 때 ±6 샘플(30Hz 기준 ±0.2s) 창

        /// <summary>
        /// raw 샘플을 zero-phase Gaussian으로 스무딩한 새 리스트를 반환한다(비파괴).
        /// strength 0 또는 샘플 3개 미만이면 원본을 그대로 복사한다.
        /// </summary>
        public static List<DialogueCameraRecordingSO.Sample> Smooth(
            IReadOnlyList<DialogueCameraRecordingSO.Sample> raw, float strength)
        {
            return Smooth(raw, strength, strength, strength);
        }

        public static List<DialogueCameraRecordingSO.Sample> Smooth(
            IReadOnlyList<DialogueCameraRecordingSO.Sample> raw,
            float positionStrength,
            float rotationStrength,
            float fovStrength)
        {
            var result = new List<DialogueCameraRecordingSO.Sample>();
            if (raw == null || raw.Count == 0)
                return result;

            int positionRadius = Mathf.RoundToInt(Mathf.Clamp01(positionStrength) * MaxRadius);
            int rotationRadius = Mathf.RoundToInt(Mathf.Clamp01(rotationStrength) * MaxRadius);
            int fovRadius = Mathf.RoundToInt(Mathf.Clamp01(fovStrength) * MaxRadius);
            int maxRadius = Mathf.Max(positionRadius, Mathf.Max(rotationRadius, fovRadius));
            if (maxRadius <= 0 || raw.Count < 3)
            {
                result.AddRange(raw);
                return result;
            }

            for (int i = 0; i < raw.Count; i++)
            {
                int edgeRadius = Mathf.Min(i, raw.Count - 1 - i);
                if (edgeRadius <= 0)
                {
                    result.Add(raw[i]);
                    continue;
                }

                Quaternion refQ = Quaternion.Euler(raw[i].localEuler);
                Vector3 position = positionRadius > 0
                    ? SmoothPosition(raw, i, Mathf.Min(positionRadius, edgeRadius))
                    : raw[i].localPosition;
                Quaternion rotation = rotationRadius > 0
                    ? SmoothRotation(raw, i, Mathf.Min(rotationRadius, edgeRadius), refQ)
                    : refQ;
                float fov = fovRadius > 0
                    ? SmoothFov(raw, i, Mathf.Min(fovRadius, edgeRadius))
                    : raw[i].fieldOfView;

                result.Add(new DialogueCameraRecordingSO.Sample
                {
                    sampleTime = raw[i].sampleTime,
                    localPosition = position,
                    localEuler = rotation.eulerAngles,
                    fieldOfView = fov
                });
            }

            return result;
        }

        private static Vector3 SmoothPosition(IReadOnlyList<DialogueCameraRecordingSO.Sample> raw, int index, int radius)
        {
            float sigma = Mathf.Max(0.0001f, radius / 2f);
            Vector3 sum = Vector3.zero;
            float weightSum = 0f;
            for (int k = -radius; k <= radius; k++)
            {
                float weight = Mathf.Exp(-(k * k) / (2f * sigma * sigma));
                sum += raw[index + k].localPosition * weight;
                weightSum += weight;
            }
            return sum / weightSum;
        }

        private static Quaternion SmoothRotation(
            IReadOnlyList<DialogueCameraRecordingSO.Sample> raw,
            int index,
            int radius,
            Quaternion reference)
        {
            float sigma = Mathf.Max(0.0001f, radius / 2f);
            Vector4 sum = Vector4.zero;
            for (int k = -radius; k <= radius; k++)
            {
                float weight = Mathf.Exp(-(k * k) / (2f * sigma * sigma));
                Quaternion q = Quaternion.Euler(raw[index + k].localEuler);
                if (Quaternion.Dot(q, reference) < 0f)
                    q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                sum += new Vector4(q.x, q.y, q.z, q.w) * weight;
            }
            Vector4 normalized = sum.normalized;
            return new Quaternion(normalized.x, normalized.y, normalized.z, normalized.w);
        }

        private static float SmoothFov(IReadOnlyList<DialogueCameraRecordingSO.Sample> raw, int index, int radius)
        {
            float sigma = Mathf.Max(0.0001f, radius / 2f);
            float sum = 0f;
            float weightSum = 0f;
            for (int k = -radius; k <= radius; k++)
            {
                float weight = Mathf.Exp(-(k * k) / (2f * sigma * sigma));
                sum += raw[index + k].fieldOfView * weight;
                weightSum += weight;
            }
            return sum / weightSum;
        }
    }
}
