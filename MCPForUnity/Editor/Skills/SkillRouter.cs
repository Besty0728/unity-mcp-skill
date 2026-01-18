using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnitySkills
{
    /// <summary>
    /// Routes REST API requests to skill methods.
    /// </summary>
    public static class SkillRouter
    {
        private static Dictionary<string, SkillInfo> _skills;
        private static bool _initialized;

        private class SkillInfo
        {
            public string Name;
            public string Description;
            public MethodInfo Method;
            public ParameterInfo[] Parameters;
        }

        public static void Initialize()
        {
            if (_initialized) return;
            _skills = new Dictionary<string, SkillInfo>(StringComparer.OrdinalIgnoreCase);

            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } });

            foreach (var type in allTypes)
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var attr = method.GetCustomAttribute<UnitySkillAttribute>();
                    if (attr != null)
                    {
                        var name = attr.Name ?? ToSnakeCase(method.Name);
                        _skills[name] = new SkillInfo
                        {
                            Name = name,
                            Description = attr.Description ?? "",
                            Method = method,
                            Parameters = method.GetParameters()
                        };
                    }
                }
            }
            _initialized = true;
            Debug.Log($"[UnitySkills] Discovered {_skills.Count} skills");
        }

        public static string GetManifest()
        {
            Initialize();
            var manifest = new
            {
                version = "1.1.0",
                totalSkills = _skills.Count,
                skills = _skills.Values.Select(s => new
                {
                    name = s.Name,
                    description = s.Description,
                    parameters = s.Parameters.Select(p => new
                    {
                        name = p.Name,
                        type = GetJsonType(p.ParameterType),
                        required = !p.HasDefaultValue,
                        defaultValue = p.HasDefaultValue ? p.DefaultValue?.ToString() : null
                    })
                })
            };
            return JsonConvert.SerializeObject(manifest, Formatting.Indented);
        }

        public static string Execute(string name, string json)
        {
            Initialize();
            if (!_skills.TryGetValue(name, out var skill))
            {
                return JsonConvert.SerializeObject(new
                {
                    status = "error",
                    error = $"Skill '{name}' not found",
                    availableSkills = _skills.Keys.Take(20).ToArray()
                });
            }

            try
            {
                var args = string.IsNullOrEmpty(json) ? new JObject() : JObject.Parse(json);
                var ps = skill.Parameters;
                var invoke = new object[ps.Length];

                for (int i = 0; i < ps.Length; i++)
                {
                    var p = ps[i];
                    if (args.TryGetValue(p.Name, StringComparison.OrdinalIgnoreCase, out var token))
                    {
                        invoke[i] = token.ToObject(p.ParameterType);
                    }
                    else if (p.HasDefaultValue)
                    {
                        invoke[i] = p.DefaultValue;
                    }
                    else if (!p.ParameterType.IsValueType || Nullable.GetUnderlyingType(p.ParameterType) != null)
                    {
                        invoke[i] = null;
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            status = "error",
                            error = $"Missing required parameter: {p.Name}"
                        });
                    }
                }

                var result = skill.Method.Invoke(null, invoke);
                return JsonConvert.SerializeObject(new { status = "success", result });
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                return JsonConvert.SerializeObject(new
                {
                    status = "error",
                    error = inner.Message
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { status = "error", error = ex.Message });
            }
        }

        public static void Refresh()
        {
            _initialized = false;
            _skills = null;
            Initialize();
        }

        private static string ToSnakeCase(string s) =>
            System.Text.RegularExpressions.Regex.Replace(s, "([a-z])([A-Z])", "$1_$2").ToLower();

        private static string GetJsonType(Type t)
        {
            var underlying = Nullable.GetUnderlyingType(t) ?? t;
            if (underlying == typeof(string)) return "string";
            if (underlying == typeof(int) || underlying == typeof(long)) return "integer";
            if (underlying == typeof(float) || underlying == typeof(double)) return "number";
            if (underlying == typeof(bool)) return "boolean";
            if (underlying.IsArray) return "array";
            return "object";
        }
    }
}
