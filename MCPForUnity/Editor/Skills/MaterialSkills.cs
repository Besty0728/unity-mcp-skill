using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace UnitySkills
{
    /// <summary>
    /// Material management skills - create, modify, assign.
    /// Now supports finding by name, instanceId, or path.
    /// Automatically detects render pipeline for correct shader selection.
    /// </summary>
    public static class MaterialSkills
    {
        [UnitySkill("material_create", "Create a new material (auto-detects render pipeline if shader not specified)")]
        public static object MaterialCreate(string name, string shaderName = null, string savePath = null)
        {
            // Auto-detect shader based on render pipeline if not specified
            if (string.IsNullOrEmpty(shaderName))
            {
                shaderName = ProjectSkills.GetDefaultShaderName();
            }
            
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                // Try fallback shaders
                var pipeline = ProjectSkills.DetectRenderPipeline();
                var fallbackShaders = pipeline switch
                {
                    ProjectSkills.RenderPipelineType.URP => new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Standard" },
                    ProjectSkills.RenderPipelineType.HDRP => new[] { "HDRP/Lit", "Standard" },
                    _ => new[] { "Standard", "Mobile/Diffuse", "Unlit/Color" }
                };
                
                foreach (var fallback in fallbackShaders)
                {
                    shader = Shader.Find(fallback);
                    if (shader != null)
                    {
                        shaderName = fallback;
                        break;
                    }
                }
                
                if (shader == null)
                {
                    var pipelineInfo = ProjectSkills.DetectRenderPipeline();
                    return new { 
                        error = $"Shader not found: {shaderName}. Detected pipeline: {pipelineInfo}. Try using project_get_render_pipeline to see available shaders.",
                        detectedPipeline = pipelineInfo.ToString(),
                        recommendedShader = ProjectSkills.GetDefaultShaderName()
                    };
                }
            }

            var material = new Material(shader) { name = name };

            if (!string.IsNullOrEmpty(savePath))
            {
                // Ensure path starts with Assets/
                if (!savePath.StartsWith("Assets/"))
                {
                    savePath = "Assets/" + savePath;
                }
                
                // Ensure path ends with .mat
                if (!savePath.EndsWith(".mat"))
                {
                    savePath = savePath + ".mat";
                }
                
                var dir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                {
                    // Create folder hierarchy
                    var folders = dir.Split('/');
                    var currentPath = folders[0];
                    for (int i = 1; i < folders.Length; i++)
                    {
                        var newPath = currentPath + "/" + folders[i];
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folders[i]);
                        }
                        currentPath = newPath;
                    }
                }

                AssetDatabase.CreateAsset(material, savePath);
                AssetDatabase.SaveAssets();
            }

            var pipelineType = ProjectSkills.DetectRenderPipeline();
            return new { 
                success = true, 
                name, 
                shader = shaderName, 
                path = savePath,
                renderPipeline = pipelineType.ToString(),
                colorProperty = ProjectSkills.GetColorPropertyName(),
                textureProperty = ProjectSkills.GetMainTexturePropertyName()
            };
        }

        [UnitySkill("material_set_color", "Set a color property on a material (auto-detects property name for render pipeline)")]
        public static object MaterialSetColor(string name = null, int instanceId = 0, string path = null, float r = 1, float g = 1, float b = 1, float a = 1, string propertyName = null)
        {
            Material material = null;
            GameObject go = null;

            // Auto-detect color property name if not specified
            if (string.IsNullOrEmpty(propertyName))
            {
                propertyName = ProjectSkills.GetColorPropertyName();
            }

            // Check if finding by Asset Path
            if (!string.IsNullOrEmpty(path) && (path.StartsWith("Assets/") || path.EndsWith(".mat")))
            {
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) return new { error = $"Material asset not found: {path}" };
            }
            else
            {
                var result = GameObjectFinder.FindOrError(name, instanceId, path);
                if (result.error != null) return result.error;
                go = result.go;

                var renderer = go.GetComponent<Renderer>();
                if (renderer == null) return new { error = "No Renderer component found" };
                if (renderer.sharedMaterial == null) return new { error = "No material assigned to renderer" };
                
                material = renderer.sharedMaterial;
            }

            var color = new Color(r, g, b, a);
            
            Undo.RecordObject(material, "Set Material Color");
            
            // Try setting color with detected property, fallback to common names
            bool colorSet = false;
            var propertiesToTry = new[] { propertyName, "_BaseColor", "_Color", "_TintColor" };
            
            foreach (var prop in propertiesToTry)
            {
                if (material.HasProperty(prop))
                {
                    material.SetColor(prop, color);
                    propertyName = prop;
                    colorSet = true;
                    break;
                }
            }
            
            if (!colorSet)
            {
                return new { 
                    error = $"Material does not have a color property. Tried: {string.Join(", ", propertiesToTry)}",
                    shaderName = material.shader.name,
                    suggestion = "Use project_get_render_pipeline to check available properties"
                };
            }
            
            if (go == null) EditorUtility.SetDirty(material);

            return new { 
                success = true, 
                target = go != null ? go.name : path, 
                color = new { r, g, b, a },
                propertyUsed = propertyName
            };
        }

        [UnitySkill("material_set_texture", "Set a texture on a material (auto-detects property name for render pipeline)")]
        public static object MaterialSetTexture(string name = null, int instanceId = 0, string path = null, string texturePath = null, string propertyName = null)
        {
            if (string.IsNullOrEmpty(texturePath))
                return new { error = "texturePath is required" };
            
            // Auto-detect texture property name if not specified
            if (string.IsNullOrEmpty(propertyName))
            {
                propertyName = ProjectSkills.GetMainTexturePropertyName();
            }

            Material material = null;
            GameObject go = null;

            // Check if finding by Asset Path
            if (!string.IsNullOrEmpty(path) && (path.StartsWith("Assets/") || path.EndsWith(".mat")))
            {
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) return new { error = $"Material asset not found: {path}" };
            }
            else
            {
                var result = GameObjectFinder.FindOrError(name, instanceId, path);
                if (result.error != null) return result.error;
                go = result.go;

                var renderer = go.GetComponent<Renderer>();
                if (renderer == null) return new { error = "No Renderer component found" };
                if (renderer.sharedMaterial == null) return new { error = "No material assigned to renderer" };
                
                material = renderer.sharedMaterial;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (texture == null)
                return new { error = $"Texture not found: {texturePath}" };

            Undo.RecordObject(material, "Set Texture");
            material.SetTexture(propertyName, texture);
            
            if (go == null) EditorUtility.SetDirty(material);

            return new { success = true, target = go != null ? go.name : path, texture = texturePath };
        }

        [UnitySkill("material_assign", "Assign a material asset to a renderer (supports name/instanceId/path)")]
        public static object MaterialAssign(string name = null, int instanceId = 0, string path = null, string materialPath = null)
        {
            if (string.IsNullOrEmpty(materialPath))
                return new { error = "materialPath is required" };

            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return new { error = "No Renderer component found" };

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                return new { error = $"Material not found: {materialPath}" };

            Undo.RecordObject(renderer, "Assign Material");
            renderer.sharedMaterial = material;

            return new { success = true, gameObject = go.name, material = materialPath };
        }

        [UnitySkill("material_set_float", "Set a float property on a material (supports name/instanceId/path)")]
        public static object MaterialSetFloat(string name = null, int instanceId = 0, string path = null, string propertyName = null, float value = 0)
        {
            if (string.IsNullOrEmpty(propertyName))
                return new { error = "propertyName is required" };

            Material material = null;
            GameObject go = null;

            // Check if finding by Asset Path
            if (!string.IsNullOrEmpty(path) && (path.StartsWith("Assets/") || path.EndsWith(".mat")))
            {
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) return new { error = $"Material asset not found: {path}" };
            }
            else
            {
                var result = GameObjectFinder.FindOrError(name, instanceId, path);
                if (result.error != null) return result.error;
                go = result.go;

                var renderer = go.GetComponent<Renderer>();
                if (renderer == null) return new { error = "No Renderer component found" };
                if (renderer.sharedMaterial == null) return new { error = "No material assigned to renderer" };
                
                material = renderer.sharedMaterial;
            }

            Undo.RecordObject(material, "Set Material Float");
            material.SetFloat(propertyName, value);
            
            if (go == null) EditorUtility.SetDirty(material);

            return new { success = true, target = go != null ? go.name : path, property = propertyName, value };
        }
    }
}
