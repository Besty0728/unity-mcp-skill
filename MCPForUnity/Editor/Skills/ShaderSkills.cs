using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace UnitySkills
{
    /// <summary>
    /// Shader management skills.
    /// </summary>
    public static class ShaderSkills
    {
        [UnitySkill("shader_create", "Create a new shader file")]
        public static object ShaderCreate(string shaderName, string savePath, string template = null)
        {
            if (File.Exists(savePath))
                return new { error = $"File already exists: {savePath}" };

            var dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var content = template ?? $@"Shader ""{shaderName}""
{{
    Properties
    {{
        _MainTex (""Texture"", 2D) = ""white"" {{}}
        _Color (""Color"", Color) = (1,1,1,1)
    }}
    SubShader
    {{
        Tags {{ ""RenderType""=""Opaque"" }}
        LOD 100

        Pass
        {{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata
            {{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            }};

            struct v2f
            {{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            }};

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert (appdata v)
            {{
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }}

            fixed4 frag (v2f i) : SV_Target
            {{
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }}
            ENDCG
        }}
    }}
}}
";
            File.WriteAllText(savePath, content);
            AssetDatabase.ImportAsset(savePath);

            return new { success = true, shaderName, path = savePath };
        }

        [UnitySkill("shader_read", "Read shader source code")]
        public static object ShaderRead(string shaderPath)
        {
            if (!File.Exists(shaderPath))
                return new { error = $"Shader not found: {shaderPath}" };

            var content = File.ReadAllText(shaderPath);
            var lines = content.Split('\n').Length;

            return new { path = shaderPath, lines, content };
        }

        [UnitySkill("shader_list", "List all shaders in project")]
        public static object ShaderList(string filter = null, int limit = 100)
        {
            var guids = AssetDatabase.FindAssets("t:Shader");
            var shaders = guids
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Where(p => string.IsNullOrEmpty(filter) || p.Contains(filter))
                .Take(limit)
                .Select(p =>
                {
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(p);
                    return new
                    {
                        path = p,
                        name = shader?.name,
                        propertyCount = shader != null ? ShaderUtil.GetPropertyCount(shader) : 0
                    };
                })
                .ToArray();

            return new { count = shaders.Length, shaders };
        }

        [UnitySkill("shader_get_properties", "Get properties of a shader")]
        public static object ShaderGetProperties(string shaderNameOrPath)
        {
            Shader shader = null;

            // Try as asset path first
            if (shaderNameOrPath.EndsWith(".shader"))
                shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderNameOrPath);

            // Try as shader name
            if (shader == null)
                shader = Shader.Find(shaderNameOrPath);

            if (shader == null)
                return new { error = $"Shader not found: {shaderNameOrPath}" };

            var propCount = ShaderUtil.GetPropertyCount(shader);
            var properties = Enumerable.Range(0, propCount)
                .Select(i => new
                {
                    name = ShaderUtil.GetPropertyName(shader, i),
                    type = ShaderUtil.GetPropertyType(shader, i).ToString(),
                    description = ShaderUtil.GetPropertyDescription(shader, i)
                })
                .ToArray();

            return new
            {
                shaderName = shader.name,
                propertyCount = propCount,
                properties
            };
        }

        [UnitySkill("shader_find", "Find shaders by name")]
        public static object ShaderFind(string searchName)
        {
            var shader = Shader.Find(searchName);
            if (shader == null)
                return new { error = $"Shader not found: {searchName}" };

            var path = AssetDatabase.GetAssetPath(shader);
            return new
            {
                found = true,
                name = shader.name,
                path = string.IsNullOrEmpty(path) ? "(built-in)" : path
            };
        }

        [UnitySkill("shader_delete", "Delete a shader file")]
        public static object ShaderDelete(string shaderPath)
        {
            if (!File.Exists(shaderPath))
                return new { error = $"Shader not found: {shaderPath}" };

            AssetDatabase.DeleteAsset(shaderPath);
            return new { success = true, deleted = shaderPath };
        }
    }
}
