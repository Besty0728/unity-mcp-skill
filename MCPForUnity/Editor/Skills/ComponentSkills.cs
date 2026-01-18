using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;

namespace UnitySkills
{
    /// <summary>
    /// Component management skills - add, remove, get, set properties.
    /// </summary>
    public static class ComponentSkills
    {
        [UnitySkill("component_add", "Add a component to a GameObject")]
        public static object ComponentAdd(string gameObjectName, string componentType)
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var type = FindComponentType(componentType);
            if (type == null)
                return new { error = $"Component type not found: {componentType}" };

            var comp = Undo.AddComponent(go, type);
            return new { success = true, gameObject = gameObjectName, component = type.Name };
        }

        [UnitySkill("component_remove", "Remove a component from a GameObject")]
        public static object ComponentRemove(string gameObjectName, string componentType)
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var type = FindComponentType(componentType);
            if (type == null)
                return new { error = $"Component type not found: {componentType}" };

            var comp = go.GetComponent(type);
            if (comp == null)
                return new { error = $"Component not found on {gameObjectName}: {componentType}" };

            Undo.DestroyObjectImmediate(comp);
            return new { success = true, removed = componentType };
        }

        [UnitySkill("component_list", "List all components on a GameObject")]
        public static object ComponentList(string gameObjectName)
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var components = go.GetComponents<Component>()
                .Where(c => c != null)
                .Select(c => new { type = c.GetType().Name, enabled = (c as Behaviour)?.enabled ?? true })
                .ToArray();

            return new { gameObject = gameObjectName, components };
        }

        [UnitySkill("component_set_property", "Set a property on a component")]
        public static object ComponentSetProperty(string gameObjectName, string componentType, string propertyName, string value)
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var type = FindComponentType(componentType);
            var comp = go.GetComponent(type);
            if (comp == null)
                return new { error = $"Component not found: {componentType}" };

            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (prop == null && field == null)
                return new { error = $"Property/field not found: {propertyName}" };

            Undo.RecordObject(comp, "Set Property");

            try
            {
                if (prop != null)
                {
                    var converted = ConvertValue(value, prop.PropertyType);
                    prop.SetValue(comp, converted);
                }
                else
                {
                    var converted = ConvertValue(value, field.FieldType);
                    field.SetValue(comp, converted);
                }
                return new { success = true, property = propertyName, value };
            }
            catch (System.Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        [UnitySkill("component_get_properties", "Get all properties of a component")]
        public static object ComponentGetProperties(string gameObjectName, string componentType)
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var type = FindComponentType(componentType);
            var comp = go.GetComponent(type);
            if (comp == null)
                return new { error = $"Component not found: {componentType}" };

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !p.GetIndexParameters().Any())
                .Select(p =>
                {
                    try { return new { name = p.Name, type = p.PropertyType.Name, value = p.GetValue(comp)?.ToString() }; }
                    catch { return new { name = p.Name, type = p.PropertyType.Name, value = "(error)" }; }
                })
                .ToArray();

            return new { gameObject = gameObjectName, component = componentType, properties = props };
        }

        private static System.Type FindComponentType(string name)
        {
            return System.Type.GetType(name) ??
                System.AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
                    .FirstOrDefault(t => t.Name == name && typeof(Component).IsAssignableFrom(t));
        }

        private static object ConvertValue(string value, System.Type targetType)
        {
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int)) return int.Parse(value);
            if (targetType == typeof(float)) return float.Parse(value);
            if (targetType == typeof(bool)) return bool.Parse(value);
            if (targetType == typeof(Vector3))
            {
                var parts = value.Split(',');
                return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            }
            if (targetType == typeof(Color))
            {
                var parts = value.Split(',');
                return new Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), parts.Length > 3 ? float.Parse(parts[3]) : 1);
            }
            return System.Convert.ChangeType(value, targetType);
        }
    }
}
