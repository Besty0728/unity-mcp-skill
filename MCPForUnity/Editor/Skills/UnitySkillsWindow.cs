using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace UnitySkills
{
    /// <summary>
    /// Unity Editor Window for UnitySkills REST API control.
    /// </summary>
    public class UnitySkillsWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _serverRunning;
        private string _testSkillName = "";
        private string _testSkillParams = "{}";
        private string _testResult = "";
        private Dictionary<string, List<SkillInfo>> _skillsByCategory;
        private Dictionary<string, bool> _categoryFoldouts = new Dictionary<string, bool>();

        private class SkillInfo
        {
            public string Name;
            public string Description;
            public MethodInfo Method;
        }

        [MenuItem("Window/UnitySkills")]
        public static void ShowWindow()
        {
            var window = GetWindow<UnitySkillsWindow>("UnitySkills");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            RefreshSkillsList();
            _serverRunning = SkillsHttpServer.IsRunning;
        }

        private void RefreshSkillsList()
        {
            _skillsByCategory = new Dictionary<string, List<SkillInfo>>();

            var allTypes = System.AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } });

            foreach (var type in allTypes)
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var attr = method.GetCustomAttribute<UnitySkillAttribute>();
                    if (attr != null)
                    {
                        var category = type.Name.Replace("Skills", "");
                        if (!_skillsByCategory.ContainsKey(category))
                            _skillsByCategory[category] = new List<SkillInfo>();

                        _skillsByCategory[category].Add(new SkillInfo
                        {
                            Name = attr.Name ?? method.Name,
                            Description = attr.Description ?? "",
                            Method = method
                        });
                    }
                }
            }

            foreach (var cat in _skillsByCategory.Keys)
            {
                if (!_categoryFoldouts.ContainsKey(cat))
                    _categoryFoldouts[cat] = false;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("UnitySkills", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Server Status
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            
            var statusStyle = new GUIStyle(EditorStyles.boldLabel);
            statusStyle.normal.textColor = _serverRunning ? Color.green : Color.red;
            GUILayout.Label(_serverRunning ? "● Server Running" : "● Server Stopped", statusStyle);
            
            GUILayout.FlexibleSpace();
            
            if (_serverRunning)
            {
                if (GUILayout.Button("Stop Server", GUILayout.Width(100)))
                {
                    SkillsHttpServer.Stop();
                    _serverRunning = false;
                }
            }
            else
            {
                if (GUILayout.Button("Start Server", GUILayout.Width(100)))
                {
                    SkillsHttpServer.Start();
                    _serverRunning = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_serverRunning)
            {
                EditorGUILayout.SelectableLabel(SkillsHttpServer.Url, EditorStyles.miniLabel, GUILayout.Height(18));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Test Skill Section
            EditorGUILayout.LabelField("Test Skill", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            _testSkillName = EditorGUILayout.TextField("Skill Name", _testSkillName);
            EditorGUILayout.LabelField("Parameters (JSON):");
            _testSkillParams = EditorGUILayout.TextArea(_testSkillParams, GUILayout.Height(60));
            
            if (GUILayout.Button("Execute Skill"))
            {
                _testResult = SkillRouter.Execute(_testSkillName, _testSkillParams);
            }

            if (!string.IsNullOrEmpty(_testResult))
            {
                EditorGUILayout.LabelField("Result:");
                EditorGUILayout.TextArea(_testResult, GUILayout.Height(80));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Skills List
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Available Skills", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshSkillsList();
                SkillRouter.Refresh();
            }
            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_skillsByCategory != null)
            {
                int totalSkills = _skillsByCategory.Values.Sum(l => l.Count);
                EditorGUILayout.LabelField($"Total: {totalSkills} skills in {_skillsByCategory.Count} categories", EditorStyles.miniLabel);

                foreach (var kvp in _skillsByCategory.OrderBy(k => k.Key))
                {
                    _categoryFoldouts[kvp.Key] = EditorGUILayout.Foldout(_categoryFoldouts[kvp.Key], $"{kvp.Key} ({kvp.Value.Count})", true);
                    
                    if (_categoryFoldouts[kvp.Key])
                    {
                        EditorGUI.indentLevel++;
                        foreach (var skill in kvp.Value)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(skill.Name, EditorStyles.boldLabel);
                            if (GUILayout.Button("Use", GUILayout.Width(40)))
                            {
                                _testSkillName = skill.Name;
                                _testSkillParams = BuildDefaultParams(skill.Method);
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.LabelField(skill.Description, EditorStyles.miniLabel);
                            EditorGUILayout.Space(3);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private string BuildDefaultParams(MethodInfo method)
        {
            var ps = method.GetParameters();
            if (ps.Length == 0) return "{}";

            var parts = ps.Select(p =>
            {
                var defaultVal = p.HasDefaultValue ? p.DefaultValue : GetDefaultForType(p.ParameterType);
                var valStr = defaultVal == null ? "null" :
                    p.ParameterType == typeof(string) ? $"\"{defaultVal}\"" :
                    defaultVal.ToString().ToLower();
                return $"\"{p.Name}\": {valStr}";
            });

            return "{\n  " + string.Join(",\n  ", parts) + "\n}";
        }

        private object GetDefaultForType(System.Type t)
        {
            if (t == typeof(string)) return "";
            if (t == typeof(int) || t == typeof(float)) return 0;
            if (t == typeof(bool)) return false;
            return null;
        }

        private void OnInspectorUpdate()
        {
            // Check server status periodically
            if (_serverRunning != SkillsHttpServer.IsRunning)
            {
                _serverRunning = SkillsHttpServer.IsRunning;
                Repaint();
            }
        }
    }
}
