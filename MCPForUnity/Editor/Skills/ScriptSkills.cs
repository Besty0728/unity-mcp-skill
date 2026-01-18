using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnitySkills
{
    /// <summary>
    /// Script management skills - create, read, modify.
    /// </summary>
    public static class ScriptSkills
    {
        [UnitySkill("script_create", "Create a new C# script")]
        public static object ScriptCreate(string scriptName, string folder = "Assets/Scripts", string template = null)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, scriptName + ".cs");
            if (File.Exists(path))
                return new { error = $"Script already exists: {path}" };

            var content = template ?? $@"using UnityEngine;

public class {scriptName} : MonoBehaviour
{{
    void Start()
    {{
        
    }}

    void Update()
    {{
        
    }}
}}
";
            File.WriteAllText(path, content);
            AssetDatabase.ImportAsset(path);

            return new { success = true, path, className = scriptName };
        }

        [UnitySkill("script_read", "Read the contents of a script")]
        public static object ScriptRead(string scriptPath)
        {
            if (!File.Exists(scriptPath))
                return new { error = $"Script not found: {scriptPath}" };

            var content = File.ReadAllText(scriptPath);
            var lines = content.Split('\n').Length;

            return new { path = scriptPath, lines, content };
        }

        [UnitySkill("script_delete", "Delete a script file")]
        public static object ScriptDelete(string scriptPath)
        {
            if (!File.Exists(scriptPath))
                return new { error = $"Script not found: {scriptPath}" };

            AssetDatabase.DeleteAsset(scriptPath);
            return new { success = true, deleted = scriptPath };
        }

        [UnitySkill("script_find_in_file", "Search for pattern in scripts")]
        public static object ScriptFindInFile(string pattern, string folder = "Assets", bool isRegex = false, int limit = 50)
        {
            var results = new System.Collections.Generic.List<object>();
            var files = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (results.Count >= limit) break;

                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    bool match = isRegex
                        ? Regex.IsMatch(lines[i], pattern)
                        : lines[i].Contains(pattern);

                    if (match)
                    {
                        results.Add(new
                        {
                            file = file.Replace("\\", "/"),
                            line = i + 1,
                            content = lines[i].Trim()
                        });

                        if (results.Count >= limit) break;
                    }
                }
            }

            return new { pattern, matchCount = results.Count, matches = results };
        }

        [UnitySkill("script_append", "Append content to a script")]
        public static object ScriptAppend(string scriptPath, string content, int atLine = -1)
        {
            if (!File.Exists(scriptPath))
                return new { error = $"Script not found: {scriptPath}" };

            var lines = File.ReadAllLines(scriptPath).ToList();

            if (atLine < 0 || atLine >= lines.Count)
            {
                // Append before last closing brace
                var lastBrace = lines.FindLastIndex(l => l.Trim() == "}");
                if (lastBrace > 0)
                    lines.Insert(lastBrace, content);
                else
                    lines.Add(content);
            }
            else
            {
                lines.Insert(atLine, content);
            }

            File.WriteAllLines(scriptPath, lines);
            AssetDatabase.ImportAsset(scriptPath);

            return new { success = true, path = scriptPath };
        }
    }
}
