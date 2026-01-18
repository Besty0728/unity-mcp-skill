using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;

namespace UnitySkills
{
    /// <summary>
    /// Scene management skills - load, save, create, get info.
    /// </summary>
    public static class SceneSkills
    {
        [UnitySkill("scene_create", "Create a new empty scene")]
        public static object SceneCreate(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return new { error = "scenePath is required" };

            var dir = Path.GetDirectoryName(scenePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            return new { success = true, scenePath, sceneName = scene.name };
        }

        [UnitySkill("scene_load", "Load an existing scene")]
        public static object SceneLoad(string scenePath, bool additive = false)
        {
            if (!File.Exists(scenePath))
                return new { error = $"Scene not found: {scenePath}" };

            var mode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
            var scene = EditorSceneManager.OpenScene(scenePath, mode);

            return new { success = true, sceneName = scene.name, scenePath = scene.path };
        }

        [UnitySkill("scene_save", "Save the current scene")]
        public static object SceneSave(string scenePath = null)
        {
            var scene = SceneManager.GetActiveScene();
            var path = scenePath ?? scene.path;

            if (string.IsNullOrEmpty(path))
                return new { error = "Scene has no path. Provide scenePath parameter." };

            EditorSceneManager.SaveScene(scene, path);
            return new { success = true, scenePath = path };
        }

        [UnitySkill("scene_get_info", "Get current scene information")]
        public static object SceneGetInfo()
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            return new
            {
                sceneName = scene.name,
                scenePath = scene.path,
                isDirty = scene.isDirty,
                rootObjectCount = roots.Length,
                rootObjects = roots.Select(go => new
                {
                    name = go.name,
                    instanceId = go.GetInstanceID(),
                    childCount = go.transform.childCount
                }).ToArray()
            };
        }

        [UnitySkill("scene_get_hierarchy", "Get scene hierarchy tree")]
        public static object SceneGetHierarchy(int maxDepth = 3)
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            return new
            {
                sceneName = scene.name,
                hierarchy = roots.Select(go => GetHierarchyNode(go, 0, maxDepth)).ToArray()
            };
        }

        private static object GetHierarchyNode(GameObject go, int depth, int maxDepth)
        {
            var node = new
            {
                name = go.name,
                instanceId = go.GetInstanceID(),
                components = go.GetComponents<Component>().Select(c => c.GetType().Name).ToArray(),
                children = depth < maxDepth
                    ? Enumerable.Range(0, go.transform.childCount)
                        .Select(i => GetHierarchyNode(go.transform.GetChild(i).gameObject, depth + 1, maxDepth))
                        .ToArray()
                    : null
            };
            return node;
        }

        [UnitySkill("scene_screenshot", "Capture a screenshot of the scene view")]
        public static object SceneScreenshot(string filename = "screenshot.png", int width = 1920, int height = 1080)
        {
            var path = Path.Combine(Application.dataPath, "Screenshots", filename);
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            ScreenCapture.CaptureScreenshot(path, 1);
            AssetDatabase.Refresh();

            return new { success = true, path };
        }
    }
}
