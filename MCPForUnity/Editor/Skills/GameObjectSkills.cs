using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace UnitySkills
{
    /// <summary>
    /// GameObject management skills - create, modify, delete, find.
    /// </summary>
    public static class GameObjectSkills
    {
        [UnitySkill("gameobject_create", "Create a new GameObject")]
        public static object GameObjectCreate(string name, string primitiveType = null, float x = 0, float y = 0, float z = 0)
        {
            GameObject go;

            if (!string.IsNullOrEmpty(primitiveType))
            {
                if (System.Enum.TryParse<PrimitiveType>(primitiveType, true, out var pt))
                {
                    go = GameObject.CreatePrimitive(pt);
                    go.name = name;
                }
                else
                {
                    return new { error = $"Unknown primitive type: {primitiveType}. Use: Cube, Sphere, Capsule, Cylinder, Plane, Quad" };
                }
            }
            else
            {
                go = new GameObject(name);
            }

            go.transform.position = new Vector3(x, y, z);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);

            return new
            {
                success = true,
                name = go.name,
                instanceId = go.GetInstanceID(),
                position = new { x, y, z }
            };
        }

        [UnitySkill("gameobject_delete", "Delete a GameObject by name or instance ID")]
        public static object GameObjectDelete(string name = null, int instanceId = 0)
        {
            GameObject go = null;

            if (instanceId != 0)
                go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            else if (!string.IsNullOrEmpty(name))
                go = GameObject.Find(name);

            if (go == null)
                return new { error = "GameObject not found" };

            Undo.DestroyObjectImmediate(go);
            return new { success = true, deleted = name ?? instanceId.ToString() };
        }

        [UnitySkill("gameobject_find", "Find GameObjects by name, tag, or component")]
        public static object GameObjectFind(string name = null, string tag = null, string component = null, int limit = 50)
        {
            IEnumerable<GameObject> results = Object.FindObjectsOfType<GameObject>();

            if (!string.IsNullOrEmpty(name))
                results = results.Where(go => go.name.Contains(name));

            if (!string.IsNullOrEmpty(tag))
                results = results.Where(go => go.CompareTag(tag));

            if (!string.IsNullOrEmpty(component))
            {
                var compType = System.Type.GetType(component) ?? 
                    System.AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
                        .FirstOrDefault(t => t.Name == component);
                
                if (compType != null)
                    results = results.Where(go => go.GetComponent(compType) != null);
            }

            var list = results.Take(limit).Select(go => new
            {
                name = go.name,
                instanceId = go.GetInstanceID(),
                tag = go.tag,
                position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z }
            }).ToArray();

            return new { count = list.Length, objects = list };
        }

        [UnitySkill("gameobject_set_transform", "Set position, rotation, or scale of a GameObject")]
        public static object GameObjectSetTransform(
            string name = null,
            int instanceId = 0,
            float? posX = null, float? posY = null, float? posZ = null,
            float? rotX = null, float? rotY = null, float? rotZ = null,
            float? scaleX = null, float? scaleY = null, float? scaleZ = null)
        {
            GameObject go = instanceId != 0 
                ? EditorUtility.InstanceIDToObject(instanceId) as GameObject
                : GameObject.Find(name);

            if (go == null)
                return new { error = "GameObject not found" };

            Undo.RecordObject(go.transform, "Set Transform");

            if (posX.HasValue || posY.HasValue || posZ.HasValue)
            {
                var pos = go.transform.position;
                go.transform.position = new Vector3(
                    posX ?? pos.x,
                    posY ?? pos.y,
                    posZ ?? pos.z);
            }

            if (rotX.HasValue || rotY.HasValue || rotZ.HasValue)
            {
                var rot = go.transform.eulerAngles;
                go.transform.eulerAngles = new Vector3(
                    rotX ?? rot.x,
                    rotY ?? rot.y,
                    rotZ ?? rot.z);
            }

            if (scaleX.HasValue || scaleY.HasValue || scaleZ.HasValue)
            {
                var scale = go.transform.localScale;
                go.transform.localScale = new Vector3(
                    scaleX ?? scale.x,
                    scaleY ?? scale.y,
                    scaleZ ?? scale.z);
            }

            return new
            {
                success = true,
                name = go.name,
                position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
                rotation = new { x = go.transform.eulerAngles.x, y = go.transform.eulerAngles.y, z = go.transform.eulerAngles.z },
                scale = new { x = go.transform.localScale.x, y = go.transform.localScale.y, z = go.transform.localScale.z }
            };
        }

        [UnitySkill("gameobject_duplicate", "Duplicate a GameObject")]
        public static object GameObjectDuplicate(string name = null, int instanceId = 0)
        {
            GameObject go = instanceId != 0
                ? EditorUtility.InstanceIDToObject(instanceId) as GameObject
                : GameObject.Find(name);

            if (go == null)
                return new { error = "GameObject not found" };

            var copy = Object.Instantiate(go, go.transform.parent);
            copy.name = go.name + " (Copy)";
            Undo.RegisterCreatedObjectUndo(copy, "Duplicate " + go.name);

            return new { success = true, originalName = go.name, copyName = copy.name, copyInstanceId = copy.GetInstanceID() };
        }

        [UnitySkill("gameobject_set_parent", "Set the parent of a GameObject")]
        public static object GameObjectSetParent(string childName, string parentName = null)
        {
            var child = GameObject.Find(childName);
            if (child == null)
                return new { error = $"Child not found: {childName}" };

            Transform parent = null;
            if (!string.IsNullOrEmpty(parentName))
            {
                var parentGo = GameObject.Find(parentName);
                if (parentGo == null)
                    return new { error = $"Parent not found: {parentName}" };
                parent = parentGo.transform;
            }

            Undo.SetTransformParent(child.transform, parent, "Set Parent");
            return new { success = true, child = childName, parent = parentName ?? "(root)" };
        }
    }
}
