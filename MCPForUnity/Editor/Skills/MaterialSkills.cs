using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace UnitySkills
{
    /// <summary>
    /// Material management skills - create, modify, assign.
    /// </summary>
    public static class MaterialSkills
    {
        [UnitySkill("material_create", "Create a new material")]
        public static object MaterialCreate(string name, string shaderName = "Standard", string savePath = null)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
                return new { error = $"Shader not found: {shaderName}" };

            var material = new Material(shader) { name = name };

            if (!string.IsNullOrEmpty(savePath))
            {
                var dir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                AssetDatabase.CreateAsset(material, savePath);
                AssetDatabase.SaveAssets();
            }

            return new { success = true, name, shader = shaderName, path = savePath };
        }

        [UnitySkill("material_set_color", "Set a color property on a material or renderer")]
        public static object MaterialSetColor(string gameObjectName, float r, float g, float b, float a = 1, string propertyName = "_Color")
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return new { error = "No Renderer component found" };

            var color = new Color(r, g, b, a);
            
            // Use material instance to avoid modifying shared material
            Undo.RecordObject(renderer, "Set Material Color");
            renderer.material.SetColor(propertyName, color);

            return new { success = true, gameObject = gameObjectName, color = new { r, g, b, a } };
        }

        [UnitySkill("material_set_texture", "Set a texture on a material")]
        public static object MaterialSetTexture(string gameObjectName, string texturePath, string propertyName = "_MainTex")
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return new { error = "No Renderer component found" };

            var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (texture == null)
                return new { error = $"Texture not found: {texturePath}" };

            Undo.RecordObject(renderer, "Set Texture");
            renderer.material.SetTexture(propertyName, texture);

            return new { success = true, gameObject = gameObjectName, texture = texturePath };
        }

        [UnitySkill("material_assign", "Assign a material asset to a renderer")]
        public static object MaterialAssign(string gameObjectName, string materialPath)
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return new { error = "No Renderer component found" };

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                return new { error = $"Material not found: {materialPath}" };

            Undo.RecordObject(renderer, "Assign Material");
            renderer.material = material;

            return new { success = true, gameObject = gameObjectName, material = materialPath };
        }

        [UnitySkill("material_set_float", "Set a float property on a material")]
        public static object MaterialSetFloat(string gameObjectName, string propertyName, float value)
        {
            var go = GameObject.Find(gameObjectName);
            if (go == null)
                return new { error = $"GameObject not found: {gameObjectName}" };

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return new { error = "No Renderer component found" };

            Undo.RecordObject(renderer, "Set Material Float");
            renderer.material.SetFloat(propertyName, value);

            return new { success = true, gameObject = gameObjectName, property = propertyName, value };
        }
    }
}
