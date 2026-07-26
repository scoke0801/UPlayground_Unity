using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Projectile;

namespace UPlayGround.Data.Editor.Authoring
{
    /// <summary>
    /// Definition의 이동 전략을 정규화된 2D 궤적으로 표현한다.
    /// 물리 시뮬레이션이 아니라 저작 중 형태·방향·범위를 빠르게 읽기 위한 프리뷰다.
    /// </summary>
    internal sealed class ProjectileTrajectoryPreviewElement : VisualElement
    {
        private const int SegmentCount = 48;
        private readonly List<Vector2> _points = new(SegmentCount + 1);
        private ProjectileDefinitionSO _definition;

        public ProjectileTrajectoryPreviewElement()
        {
            AddToClassList("up-projectile-preview");
            generateVisualContent += DrawPreview;
        }

        public void SetDefinition(ProjectileDefinitionSO definition)
        {
            _definition = definition;
            MarkDirtyRepaint();
        }

        private void DrawPreview(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width < 40f || rect.height < 40f)
                return;

            Painter2D painter = context.painter2D;
            float left = rect.xMin + 24f;
            float right = rect.xMax - 24f;
            float top = rect.yMin + 18f;
            float bottom = rect.yMax - 22f;
            float centerY = Mathf.Lerp(top, bottom, 0.68f);

            DrawGrid(painter, left, right, top, bottom);
            if (_definition?.motion == null)
                return;

            _points.Clear();
            switch (_definition.motion)
            {
                case ArcProjectileMotion arc:
                    BuildArc(left, right, centerY, top, arc);
                    break;
                case OrbitProjectileMotion:
                    BuildCircle(
                        new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f),
                        Mathf.Min(right - left, bottom - top) * 0.28f);
                    break;
                case StationaryProjectileMotion:
                    BuildCircle(
                        new Vector2((left + right) * 0.5f, centerY),
                        Mathf.Min(right - left, bottom - top) * 0.12f);
                    break;
                case HomingProjectileMotion homing:
                    BuildHoming(left, right, centerY, top, homing);
                    break;
                case HitscanProjectileMotion:
                    _points.Add(new Vector2(left, centerY));
                    _points.Add(new Vector2(right, centerY));
                    break;
                default:
                    BuildLinear(left, right, centerY);
                    break;
            }

            if (_points.Count < 2)
                return;

            painter.lineWidth = _definition.motion is HitscanProjectileMotion ? 3f : 2f;
            painter.strokeColor = _definition.motion switch
            {
                ArcProjectileMotion => new Color(0.32f, 0.82f, 1f),
                HomingProjectileMotion => new Color(0.65f, 0.45f, 1f),
                OrbitProjectileMotion => new Color(0.35f, 0.9f, 0.68f),
                StationaryProjectileMotion => new Color(1f, 0.62f, 0.24f),
                HitscanProjectileMotion => new Color(1f, 0.36f, 0.3f),
                _ => new Color(0.25f, 0.68f, 1f),
            };
            painter.BeginPath();
            painter.MoveTo(_points[0]);
            for (int i = 1; i < _points.Count; i++)
                painter.LineTo(_points[i]);
            painter.Stroke();

            DrawMarker(painter, _points[0], new Color(0.35f, 0.95f, 0.6f));
            DrawMarker(painter, _points[^1], new Color(1f, 0.4f, 0.32f));
        }

        private static void DrawGrid(
            Painter2D painter,
            float left,
            float right,
            float top,
            float bottom)
        {
            painter.lineWidth = 1f;
            painter.strokeColor = new Color(0.32f, 0.4f, 0.48f, 0.26f);
            for (int i = 0; i <= 4; i++)
            {
                float x = Mathf.Lerp(left, right, i / 4f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, top));
                painter.LineTo(new Vector2(x, bottom));
                painter.Stroke();
            }
            for (int i = 0; i <= 3; i++)
            {
                float y = Mathf.Lerp(top, bottom, i / 3f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(left, y));
                painter.LineTo(new Vector2(right, y));
                painter.Stroke();
            }
        }

        private void BuildLinear(float left, float right, float centerY)
        {
            _points.Add(new Vector2(left, centerY));
            _points.Add(new Vector2(right, centerY));
        }

        private void BuildArc(
            float left,
            float right,
            float centerY,
            float top,
            ArcProjectileMotion arc)
        {
            float normalizedHeight = Mathf.Clamp01(arc.arcHeight / 12f);
            float height = Mathf.Lerp(24f, centerY - top, normalizedHeight);
            for (int i = 0; i <= SegmentCount; i++)
            {
                float t = i / (float)SegmentCount;
                float curved = arc.progressCurve != null
                    ? Mathf.Clamp01(arc.progressCurve.Evaluate(t))
                    : t;
                _points.Add(new Vector2(
                    Mathf.Lerp(left, right, curved),
                    centerY - 4f * curved * (1f - curved) * height));
            }
        }

        private void BuildHoming(
            float left,
            float right,
            float centerY,
            float top,
            HomingProjectileMotion homing)
        {
            float bend = Mathf.Lerp(
                20f,
                centerY - top,
                Mathf.Clamp01(homing.turnRate / 360f));
            float delayRatio = Mathf.Clamp01(
                homing.activationDelay / Mathf.Max(0.01f, _definition.lifetime));
            for (int i = 0; i <= SegmentCount; i++)
            {
                float t = i / (float)SegmentCount;
                float active = Mathf.InverseLerp(delayRatio, 1f, t);
                float strength = homing.strengthCurve != null
                    ? homing.strengthCurve.Evaluate(active)
                    : active;
                float y = centerY - Mathf.Sin(active * Mathf.PI) * bend
                    * Mathf.Clamp01(strength);
                _points.Add(new Vector2(Mathf.Lerp(left, right, t), y));
            }
        }

        private void BuildCircle(Vector2 center, float radius)
        {
            for (int i = 0; i <= SegmentCount; i++)
            {
                float angle = i / (float)SegmentCount * Mathf.PI * 2f;
                _points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        private static void DrawMarker(Painter2D painter, Vector2 center, Color color)
        {
            const float radius = 4f;
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(center + Vector2.up * radius);
            for (int i = 1; i <= 12; i++)
            {
                float angle = i / 12f * Mathf.PI * 2f + Mathf.PI * 0.5f;
                painter.LineTo(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            painter.Fill();
        }
    }
}
