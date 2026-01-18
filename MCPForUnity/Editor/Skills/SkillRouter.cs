using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MCPForUnity.Editor.Tools;

namespace MCPForUnity.Editor.Skills
{
    /// <summary>
    /// Routes HTTP requests to UnitySkill methods.
    /// Handles skill discovery, manifest generation, and execution.
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

        /// <summary>
        /// Initialize and discover all skills.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            DiscoverSkills();
            _initialized = true;
        }

        /// <summary>
        /// Scan all assemblies for methods with [UnitySkill] attribute.
        /// </summary>
        private static void DiscoverSkills()
        {
            _skills = new Dictionary<string, SkillInfo>(StringComparer.OrdinalIgnoreCase);

            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return new Type[0]; }
                });

            foreach (var type in allTypes)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.GetCustomAttribute<UnitySkillAttribute>() != null);

                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<UnitySkillAttribute>();
                    var name = attr.Name ?? ToSnakeCase(method.Name);

                    _skills[name] = new SkillInfo
                    {
                        Name = name,
                        Description = attr.Description ?? $"Skill: {name}",
                        Method = method,
                        Parameters = method.GetParameters()
                    };
                }
            }

            Debug.Log($"[UnitySkills] Discovered {_skills.Count} skills");
        }

        /// <summary>
        /// Get JSON manifest of all available skills.
        /// </summary>
        public static string GetSkillManifestJson()
        {
            Initialize();

            var manifest = new
            {
                version = "1.0.0",
                baseUrl = SkillsHttpServer.Url,
                skills = _skills.Values.Select(s => new
                {
                    name = s.Name,
                    description = s.Description,
                    endpoint = $"POST /skill/{s.Name}",
                    parameters = s.Parameters.Select(p => new
                    {
                        name = p.Name,
                        type = GetJsonType(p.ParameterType),
                        required = !p.HasDefaultValue && !IsNullable(p.ParameterType),
                        defaultValue = p.HasDefaultValue ? p.DefaultValue?.ToString() : null
                    })
                })
            };

            return JsonConvert.SerializeObject(manifest, Formatting.Indented);
        }

        /// <summary>
        /// Execute a skill by name with JSON arguments.
        /// </summary>
        public static string ExecuteSkill(string skillName, string jsonArgs)
        {
            Initialize();

            if (!_skills.TryGetValue(skillName, out var skill))
            {
                return JsonConvert.SerializeObject(new
                {
                    status = "error",
                    error = $"Skill '{skillName}' not found",
                    availableSkills = _skills.Keys.ToArray()
                });
            }

            try
            {
                var args = string.IsNullOrEmpty(jsonArgs) 
                    ? new JObject() 
                    : JObject.Parse(jsonArgs);

                var invokeArgs = new object[skill.Parameters.Length];

                for (int i = 0; i < skill.Parameters.Length; i++)
                {
                    var param = skill.Parameters[i];
                    var paramType = param.ParameterType;

                    if (args.TryGetValue(param.Name, StringComparison.OrdinalIgnoreCase, out var token))
                    {
                        invokeArgs[i] = token.ToObject(paramType);
                    }
                    else if (param.HasDefaultValue)
                    {
                        invokeArgs[i] = param.DefaultValue;
                    }
                    else if (IsNullable(paramType))
                    {
                        invokeArgs[i] = null;
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            status = "error",
                            error = $"Missing required parameter: {param.Name}"
                        });
                    }
                }

                var result = skill.Method.Invoke(null, invokeArgs);

                return JsonConvert.SerializeObject(new
                {
                    status = "success",
                    skill = skillName,
                    result
                });
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                return JsonConvert.SerializeObject(new
                {
                    status = "error",
                    skill = skillName,
                    error = inner.Message,
                    type = inner.GetType().Name
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    status = "error",
                    skill = skillName,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Force re-discovery of skills.
        /// </summary>
        public static void Refresh()
        {
            _initialized = false;
            Initialize();
        }

        private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return System.Text.RegularExpressions.Regex.Replace(
                name, "([a-z0-9])([A-Z])", "$1_$2"
            ).ToLower();
        }

        private static string GetJsonType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            
            if (underlying == typeof(string)) return "string";
            if (underlying == typeof(int) || underlying == typeof(long)) return "integer";
            if (underlying == typeof(float) || underlying == typeof(double)) return "number";
            if (underlying == typeof(bool)) return "boolean";
            if (underlying.IsArray) return "array";
            return "object";
        }

        private static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
        }
    }
}
