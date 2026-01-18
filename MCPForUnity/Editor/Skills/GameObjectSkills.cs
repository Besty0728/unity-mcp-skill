using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace UnitySkills
{
    /// <summary>
    /// GameObject management skills - create, modify, delete, find.
    /// Now supports finding by name, instanceId, or path.
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
                path = GameObjectFinder.GetPath(go),
                position = new { x, y, z }
            };
        }

        [UnitySkill("gameobject_delete", "Delete a GameObject (supports name/instanceId/path)")]
        public static object GameObjectDelete(string name = null, int instanceId = 0, string path = null)
        {
            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

            var deletedName = go.name;
            Undo.DestroyObjectImmediate(go);
            return new { success = true, deleted = deletedName };
        }

        [UnitySkill("gameobject_find", "Find GameObjects by name/regex, tag, layer, or component")]
        public static object GameObjectFind(string name = null, bool useRegex = false, string tag = null, string layer = null, string component = null, int limit = 50)
        {
            // Efficiency: If tag is provided, use FindGameObjectsWithTag (faster).
            // But we need to filter further anyway.
            IEnumerable<GameObject> results;
            if (!string.IsNullOrEmpty(tag))
                results = GameObject.FindGameObjectsWithTag(tag);
            else
                results = Object.FindObjectsOfType<GameObject>();

            // Filter by Name (Regex or Contains)
            if (!string.IsNullOrEmpty(name))
            {
                if (useRegex)
                {
                    var regex = new System.Text.RegularExpressions.Regex(name);
                    results = results.Where(go => regex.IsMatch(go.name));
                }
                else
                {
                    results = results.Where(go => go.name.Contains(name));
                }
            }
            
            // Filter by Tag (if not already fetched by tag - double check in case we fell back)
            if (!string.IsNullOrEmpty(tag))
                results = results.Where(go => go.CompareTag(tag));
                
            // Filter by Layer
            if (!string.IsNullOrEmpty(layer))
            {
                int layerId = LayerMask.NameToLayer(layer);
                if (layerId != -1)
                    results = results.Where(go => go.layer == layerId);
            }

            // Filter by Component
            if (!string.IsNullOrEmpty(component))
            {
                var compType = System.Type.GetType(component) ?? 
                    System.AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
                        .FirstOrDefault(t => t.Name == component || t.FullName == component);
                
                if (compType != null)
                    results = results.Where(go => go.GetComponent(compType) != null);
            }

            var list = results.Take(limit).Select(go => new
            {
                name = go.name,
                instanceId = go.GetInstanceID(),
                path = GameObjectFinder.GetPath(go),
                tag = go.tag,
                layer = LayerMask.LayerToName(go.layer),
                position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z }
            }).ToArray();

            return new { count = list.Length, objects = list };
        }

        [UnitySkill("gameobject_set_transform", "Set position, rotation, or scale (supports name/instanceId/path)")]
        public static object GameObjectSetTransform(
            string name = null, int instanceId = 0, string path = null,
            float? posX = null, float? posY = null, float? posZ = null,
            float? rotX = null, float? rotY = null, float? rotZ = null,
            float? scaleX = null, float? scaleY = null, float? scaleZ = null)
        {
            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

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
                instanceId = go.GetInstanceID(),
                position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
                rotation = new { x = go.transform.eulerAngles.x, y = go.transform.eulerAngles.y, z = go.transform.eulerAngles.z },
                scale = new { x = go.transform.localScale.x, y = go.transform.localScale.y, z = go.transform.localScale.z }
            };
        }

        [UnitySkill("gameobject_duplicate", "Duplicate a GameObject (supports name/instanceId/path)")]
        public static object GameObjectDuplicate(string name = null, int instanceId = 0, string path = null)
        {
            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

            var copy = Object.Instantiate(go, go.transform.parent);
            copy.name = go.name + "_Copy";
            Undo.RegisterCreatedObjectUndo(copy, "Duplicate " + go.name);

            return new { 
                success = true, 
                originalName = go.name, 
                copyName = copy.name, 
                copyInstanceId = copy.GetInstanceID(),
                copyPath = GameObjectFinder.GetPath(copy)
            };
        }

        [UnitySkill("gameobject_set_parent", "Set the parent of a GameObject (supports name/instanceId/path)")]
        public static object GameObjectSetParent(string childName = null, int childInstanceId = 0, string childPath = null, 
            string parentName = null, int parentInstanceId = 0, string parentPath = null)
        {
            var (child, childError) = GameObjectFinder.FindOrError(childName, childInstanceId, childPath);
            if (childError != null) return childError;

            Transform parent = null;
            if (!string.IsNullOrEmpty(parentName) || parentInstanceId != 0 || !string.IsNullOrEmpty(parentPath))
            {
                var (parentGo, parentError) = GameObjectFinder.FindOrError(parentName, parentInstanceId, parentPath);
                if (parentError != null) return parentError;
                parent = parentGo.transform;
            }

            Undo.SetTransformParent(child.transform, parent, "Set Parent");
            return new { 
                success = true, 
                child = child.name, 
                parent = parent?.name ?? "(root)",
                newPath = GameObjectFinder.GetPath(child)
            };
        }

        [UnitySkill("gameobject_get_info", "Get detailed info about a GameObject (supports name/instanceId/path)")]
        public static object GameObjectGetInfo(string name = null, int instanceId = 0, string path = null)
        {
            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

            var components = go.GetComponents<Component>()
                .Where(c => c != null)
                .Select(c => c.GetType().Name)
                .ToArray();

            var children = new List<object>();
            foreach (Transform child in go.transform)
            {
                children.Add(new { name = child.name, instanceId = child.gameObject.GetInstanceID() });
            }

            return new
            {
                name = go.name,
                instanceId = go.GetInstanceID(),
                path = GameObjectFinder.GetPath(go),
                tag = go.tag,
                layer = LayerMask.LayerToName(go.layer),
                isActive = go.activeSelf,
                position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
                rotation = new { x = go.transform.eulerAngles.x, y = go.transform.eulerAngles.y, z = go.transform.eulerAngles.z },
                scale = new { x = go.transform.localScale.x, y = go.transform.localScale.y, z = go.transform.localScale.z },
                parent = go.transform.parent?.name,
                childCount = go.transform.childCount,
                children,
                components
            };
        }

        [UnitySkill("gameobject_set_active", "Enable or disable a GameObject (supports name/instanceId/path)")]
        public static object GameObjectSetActive(string name = null, int instanceId = 0, string path = null, bool active = true)
        {
            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

            Undo.RecordObject(go, "Set Active");
            go.SetActive(active);

            return new { success = true, name = go.name, active };
        }
    }
}
