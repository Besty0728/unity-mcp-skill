using System;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Marks a static method as a Unity Skill.
    /// Skills are automatically discovered and registered as MCP tools.
    /// Parameters are inferred from the method signature.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UnitySkillAttribute : Attribute
    {
        /// <summary>
        /// Skill name exposed to LLM. If null, derived from method name (PascalCase → snake_case).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description for the LLM. Should explain what the skill does and when to use it.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this skill returns structured output (JSON object).
        /// </summary>
        public bool StructuredOutput { get; set; } = true;

        /// <summary>
        /// Controls whether this skill is automatically registered. Defaults to true.
        /// </summary>
        public bool AutoRegister { get; set; } = true;

        /// <summary>
        /// Create a Unity Skill attribute with auto-generated name from method name.
        /// </summary>
        public UnitySkillAttribute() { }

        /// <summary>
        /// Create a Unity Skill attribute with explicit name.
        /// </summary>
        /// <param name="name">The skill name (e.g., "create_prop")</param>
        public UnitySkillAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Create a Unity Skill attribute with name and description.
        /// </summary>
        /// <param name="name">The skill name</param>
        /// <param name="description">Description for the LLM</param>
        public UnitySkillAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Describes a skill parameter. Apply to method parameters for custom descriptions.
    /// If not applied, parameter name and type are inferred automatically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class SkillParameterAttribute : Attribute
    {
        /// <summary>
        /// Parameter description for LLM.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this parameter is required. Nullable types default to false.
        /// </summary>
        public bool Required { get; set; } = true;

        /// <summary>
        /// Default value as string representation (for documentation).
        /// </summary>
        public string DefaultValue { get; set; }

        public SkillParameterAttribute(string description)
        {
            Description = description;
        }

        public SkillParameterAttribute() { }
    }
}
