using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public static class MotionPreviewSubjectBinderRegistry
    {
        private static IReadOnlyList<IMotionPreviewSubjectBinder> _binders;

        public static IMotionPreviewSubject Bind(GameObject root)
        {
            if (root == null)
                return null;

            foreach (IMotionPreviewSubjectBinder binder in Binders)
            {
                try
                {
                    IMotionPreviewSubject subject = binder.TryBind(root);
                    if (subject != null)
                        return subject;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            return GenericAnimancerPreviewSubject.TryCreate(root);
        }

        private static IReadOnlyList<IMotionPreviewSubjectBinder> Binders =>
            _binders ??= TypeCache.GetTypesDerivedFrom<IMotionPreviewSubjectBinder>()
                .Where(type => !type.IsAbstract && !type.IsInterface)
                .Select(TryCreate)
                .Where(binder => binder != null)
                .OrderByDescending(binder => binder.Priority)
                .ToArray();

        private static IMotionPreviewSubjectBinder TryCreate(Type type)
        {
            try
            {
                return Activator.CreateInstance(type) as IMotionPreviewSubjectBinder;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MotionPreview] 바인더 생성 실패: {type.FullName}\n{exception}");
                return null;
            }
        }
    }
}
