using UnityEngine;
using UnityEditor;
using System.IO;

namespace UnitySkills
{
    /// <summary>
    /// Prefab management skills - create, edit, save.
    /// </summary>
    public static class PrefabSkills
    {
        [UnitySkill("prefab_create", "Create a prefab from a GameObject")]
        public static object PrefabCreate(string gameObjectName, string savePath)
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, savePath);
            return new { success = true, prefabPath = savePath, name = prefab.name };
        }

        [UnitySkill("prefab_instantiate", "Instantiate a prefab in the scene")]
        public static object PrefabInstantiate(string prefabPath, float x = 0, float y = 0, float z = 0, string name = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return new { error = $"Prefab not found: {prefabPath}" };

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.transform.position = new Vector3(x, y, z);
            
            if (!string.IsNullOrEmpty(name))
                instance.name = name;

            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");

            return new { success = true, name = instance.name, instanceId = instance.GetInstanceID() };
        }

        [UnitySkill("prefab_apply", "Apply changes from instance to prefab")]
        public static object PrefabApply(string gameObjectName)
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            if (prefabRoot == null)
                return new { error = "GameObject is not a prefab instance" };

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);

            return new { success = true, appliedTo = prefabPath };
        }

        [UnitySkill("prefab_unpack", "Unpack a prefab instance")]
        public static object PrefabUnpack(string gameObjectName, bool completely = false)
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var mode = completely ? PrefabUnpackMode.Completely : PrefabUnpackMode.OutermostRoot;
            PrefabUtility.UnpackPrefabInstance(go, mode, InteractionMode.UserAction);

            return new { success = true, unpacked = gameObjectName };
        }
    }
}
