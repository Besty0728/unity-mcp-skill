using UnityEngine;
using UnityEditor;
using MCPForUnity.Editor.Tools;

namespace MCPForUnity.Editor.Skills
{
    /// <summary>
    /// Sample skills demonstrating the new UnitySkill attribute system.
    /// These skills are automatically discovered and exposed to AI assistants.
    /// </summary>
    public static class SampleSkills
    {
        [UnitySkill("create_cube", "Create a cube GameObject at the specified position")]
        public static string CreateCube(float x = 0, float y = 0, float z = 0, string name = "Cube")
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = new Vector3(x, y, z);
            Undo.RegisterCreatedObjectUndo(cube, $"Create {name}");
            return $"Created cube '{name}' at ({x}, {y}, {z})";
        }

        [UnitySkill("create_sphere", "Create a sphere GameObject at the specified position")]
        public static string CreateSphere(float x = 0, float y = 0, float z = 0, string name = "Sphere")
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.position = new Vector3(x, y, z);
            Undo.RegisterCreatedObjectUndo(sphere, $"Create {name}");
            return $"Created sphere '{name}' at ({x}, {y}, {z})";
        }

        [UnitySkill("set_object_color", "Set the color of a GameObject's material")]
        public static string SetObjectColor(
            [SkillParameter("Name of the GameObject to modify")] string objectName,
            [SkillParameter("Red component (0-1)")] float r = 1,
            [SkillParameter("Green component (0-1)")] float g = 1,
            [SkillParameter("Blue component (0-1)")] float b = 1)
        {
            var obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return $"Error: GameObject '{objectName}' not found";
            }

            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
            {
                return $"Error: GameObject '{objectName}' has no Renderer component";
            }

            Undo.RecordObject(renderer.sharedMaterial, "Set Color");
            renderer.sharedMaterial.color = new Color(r, g, b);
            return $"Set color of '{objectName}' to ({r}, {g}, {b})";
        }

        [UnitySkill("get_scene_info", "Get information about the current scene")]
        public static object GetSceneInfo()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            return new
            {
                sceneName = scene.name,
                scenePath = scene.path,
                rootObjectCount = rootObjects.Length,
                rootObjects = System.Array.ConvertAll(rootObjects, go => go.name)
            };
        }

        [UnitySkill("find_objects_by_tag", "Find all GameObjects with a specific tag")]
        public static object FindObjectsByTag([SkillParameter("Tag to search for")] string tag)
        {
            try
            {
                var objects = GameObject.FindGameObjectsWithTag(tag);
                return new
                {
                    tag,
                    count = objects.Length,
                    objects = System.Array.ConvertAll(objects, go => new
                    {
                        name = go.name,
                        position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z }
                    })
                };
            }
            catch (UnityException)
            {
                return new { error = $"Tag '{tag}' is not defined" };
            }
        }

        [UnitySkill("delete_object", "Delete a GameObject by name")]
        public static string DeleteObject([SkillParameter("Name of the GameObject to delete")] string objectName)
        {
            var obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return $"Error: GameObject '{objectName}' not found";
            }

            Undo.DestroyObjectImmediate(obj);
            return $"Deleted GameObject '{objectName}'";
        }
    }
}
