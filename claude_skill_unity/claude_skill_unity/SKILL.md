---
name: unity
description: Unity Engine game development via REST API. Use for Unity projects, scene manipulation, GameObject creation, and any Unity-specific automation.
---

# UnitySkills - Direct Unity Control

Control Unity Editor directly through a REST API. No MCP overhead - just simple HTTP calls.

## Quick Start

### 1. Start Unity REST Server
In Unity: **Window > UnitySkills > Start REST Server**

### 2. Call Skills from Python
```python
import unity_skills

# Create objects
unity_skills.create_cube(x=0, y=1, z=0, name="MyCube")
unity_skills.create_sphere(x=2, y=1, z=0)

# Modify appearance
unity_skills.set_object_color("MyCube", r=1, g=0, b=0)  # Red

# Query scene
info = unity_skills.get_scene_info()
print(info["result"]["rootObjects"])

# Delete objects
unity_skills.delete_object("MyCube")
```

## Available Skills

| Skill | Description | Parameters |
|-------|-------------|------------|
| `create_cube` | Create a cube | `x`, `y`, `z`, `name` |
| `create_sphere` | Create a sphere | `x`, `y`, `z`, `name` |
| `set_object_color` | Set material color | `objectName`, `r`, `g`, `b` |
| `get_scene_info` | Get scene hierarchy | (none) |
| `find_objects_by_tag` | Find by tag | `tag` |
| `delete_object` | Delete GameObject | `objectName` |

## Raw HTTP API

```bash
# List all skills
curl http://localhost:8090/skills

# Execute skill
curl -X POST http://localhost:8090/skill/create_cube \
  -H "Content-Type: application/json" \
  -d '{"x": 1, "y": 2, "z": 3, "name": "Cube1"}'
```

## Response Format

```json
{
  "status": "success",
  "skill": "create_cube",
  "result": "Created cube 'Cube1' at (1, 2, 3)"
}
```

## Adding Custom Skills

In Unity, create a C# file:

```csharp
using MCPForUnity.Editor.Tools;

public static class MySkills
{
    [UnitySkill("my_skill", "Description for AI")]
    public static string MySkill(string param1, float param2 = 0)
    {
        // Your logic here
        return "Success message";
    }
}
```

Restart the REST server to discover new skills.

## Python Client Reference

```python
import unity_skills

# Generic skill call
unity_skills.call_skill("skill_name", param1="value", param2=123)

# Health check
if unity_skills.health():
    print("Unity is running")

# List all skills
skills = unity_skills.get_skills()
```

## CLI Usage

```bash
# List skills
python unity_skills.py --list

# Call skill
python unity_skills.py create_cube x=1 y=2 z=3 name=MyCube
```
