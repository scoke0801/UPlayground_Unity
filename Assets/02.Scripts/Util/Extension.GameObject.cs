using UnityEngine;

namespace UPlayGround
{
    public static class GameObjectExtensions
    {
        /// <summary>
        /// 컴포넌트가 존재하면 가져오고, 없으면 새로 추가하여 반환합니다.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
    }
}
