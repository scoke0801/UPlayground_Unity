using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data
{
    public static class DialogueCameraTrackReducer
    {
        public static List<DialogueCameraRecordingSO.Sample> Reduce(
            IReadOnlyList<DialogueCameraRecordingSO.Sample> source,
            float sampleRate,
            float positionTolerance,
            float rotationTolerance,
            float fovTolerance)
        {
            var result = new List<DialogueCameraRecordingSO.Sample>();
            if (source == null || source.Count == 0)
                return result;
            if (source.Count <= 2)
            {
                for (int i = 0; i < source.Count; i++)
                    result.Add(WithTime(source[i], i / Mathf.Max(1f, sampleRate)));
                return result;
            }

            var normalized = new List<DialogueCameraRecordingSO.Sample>(source.Count);
            for (int i = 0; i < source.Count; i++)
                normalized.Add(WithTime(source[i], i / Mathf.Max(1f, sampleRate)));

            var keep = new bool[source.Count];
            keep[0] = true;
            keep[source.Count - 1] = true;
            ReduceRange(
                normalized,
                0,
                source.Count - 1,
                Mathf.Max(0.0001f, positionTolerance),
                Mathf.Max(0.01f, rotationTolerance),
                Mathf.Max(0.01f, fovTolerance),
                keep);

            for (int i = 0; i < normalized.Count; i++)
            {
                if (keep[i])
                    result.Add(normalized[i]);
            }
            return result;
        }

        private static void ReduceRange(
            IReadOnlyList<DialogueCameraRecordingSO.Sample> source,
            int start,
            int end,
            float positionTolerance,
            float rotationTolerance,
            float fovTolerance,
            bool[] keep)
        {
            if (end <= start + 1)
                return;

            DialogueCameraRecordingSO.Sample a = source[start];
            DialogueCameraRecordingSO.Sample b = source[end];
            float duration = Mathf.Max(0.0001f, b.sampleTime - a.sampleTime);
            float maxError = 0f;
            int maxIndex = -1;

            for (int i = start + 1; i < end; i++)
            {
                float t = Mathf.Clamp01((source[i].sampleTime - a.sampleTime) / duration);
                Vector3 position = Vector3.Lerp(a.localPosition, b.localPosition, t);
                Quaternion rotation = Quaternion.Slerp(
                    Quaternion.Euler(a.localEuler),
                    Quaternion.Euler(b.localEuler),
                    t);
                float fov = Mathf.Lerp(a.fieldOfView, b.fieldOfView, t);

                float positionError = Vector3.Distance(position, source[i].localPosition) / positionTolerance;
                float rotationError = Quaternion.Angle(rotation, Quaternion.Euler(source[i].localEuler)) / rotationTolerance;
                float fovError = Mathf.Abs(fov - source[i].fieldOfView) / fovTolerance;
                float error = Mathf.Max(positionError, Mathf.Max(rotationError, fovError));
                if (error > maxError)
                {
                    maxError = error;
                    maxIndex = i;
                }
            }

            if (maxError <= 1f || maxIndex < 0)
                return;

            keep[maxIndex] = true;
            ReduceRange(source, start, maxIndex, positionTolerance, rotationTolerance, fovTolerance, keep);
            ReduceRange(source, maxIndex, end, positionTolerance, rotationTolerance, fovTolerance, keep);
        }

        private static DialogueCameraRecordingSO.Sample WithTime(
            DialogueCameraRecordingSO.Sample sample,
            float fallbackTime)
        {
            if (sample.sampleTime <= 0f && fallbackTime > 0f)
                sample.sampleTime = fallbackTime;
            return sample;
        }
    }
}
