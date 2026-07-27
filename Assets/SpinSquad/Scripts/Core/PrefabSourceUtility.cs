using System;
using System.Reflection;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// Unity 6 có thể serialize prefab asset thành <c>UnityEngine.Prefab</c>; cần lấy root <see cref="GameObject"/>
    /// và/hoặc xử lý kết quả <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/> không cast trực tiếp được.
    /// </summary>
    static class PrefabSourceUtility
    {
        const BindingFlags InstanceLookup = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Clone prefab asset và trả về root <see cref="GameObject"/> trong scene.</summary>
        public static GameObject InstantiatePrefabRoot(UnityEngine.Object source)
        {
            if (source == null)
                return null;

            var toInstantiate = TryExtractGameObjectFromUnityWrapper(source) ?? source;
            var clone = UnityEngine.Object.Instantiate(toInstantiate);
            return TryExtractGameObjectFromUnityWrapper(clone) ?? (clone as GameObject);
        }

        /// <summary>
        /// Từ asset Prefab hoặc instance sau Instantiate: tìm root GameObject qua property/method.
        /// </summary>
        static GameObject TryExtractGameObjectFromUnityWrapper(UnityEngine.Object o)
        {
            if (o == null)
                return null;
            if (o is GameObject go)
                return go;
            if (o is Component c)
                return c.gameObject;

            var t = o.GetType();
            if (!IsUnityEngineLikeType(t))
                return null;

            foreach (var prop in t.GetProperties(InstanceLookup))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length != 0)
                    continue;
                if (prop.PropertyType != typeof(GameObject)
                    && prop.PropertyType != typeof(Transform)
                    && !typeof(Component).IsAssignableFrom(prop.PropertyType))
                    continue;

                object val;
                try
                {
                    val = prop.GetValue(o);
                }
                catch (TargetInvocationException)
                {
                    continue;
                }

                switch (val)
                {
                    case GameObject g:
                        return g;
                    case Transform tr:
                        return tr.gameObject;
                    case Component comp:
                        return comp.gameObject;
                }
            }

            foreach (var m in t.GetMethods(InstanceLookup))
            {
                if (m.IsStatic || m.GetParameters().Length != 0)
                    continue;
                if (!typeof(GameObject).IsAssignableFrom(m.ReturnType))
                    continue;
                try
                {
                    if (m.Invoke(o, null) is GameObject rg)
                        return rg;
                }
                catch (TargetInvocationException)
                {
                    // ignore
                }
            }

            // Fallback: property khai báo kiểu object/interface — vẫn có thể trả GameObject/Transform lúc runtime.
            foreach (var prop in t.GetProperties(InstanceLookup))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                    continue;
                object val;
                try
                {
                    val = prop.GetValue(o);
                }
                catch (TargetInvocationException)
                {
                    continue;
                }

                switch (val)
                {
                    case GameObject g2:
                        return g2;
                    case Transform tr2:
                        return tr2.gameObject;
                    case Component comp2:
                        return comp2.gameObject;
                }
            }

            return null;
        }

        static bool IsUnityEngineLikeType(Type t)
        {
            if (t == null)
                return false;
            var ns = t.Namespace ?? string.Empty;
            return ns.StartsWith("Unity", StringComparison.Ordinal);
        }
    }
}
