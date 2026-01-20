---
name: unity-material
description: Create and configure materials in Unity Editor via REST API
---

# Unity Material Skills

Control materials - define how objects look with colors, textures, and shaders.

## Capabilities

- Create new materials with any shader
- Set material colors (albedo, emission, etc.)
- Assign textures to materials
- Set float/vector properties
- Assign materials to renderers

## Skills Reference

| Skill | Description |
|-------|-------------|
| `material_create` | Create a new material |
| `material_set_color` | Set material color |
| `material_set_texture` | Set material texture |
| `material_assign` | Assign material to renderer |
| `material_set_float` | Set float property |

## Parameters

### material_create

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | Yes | - | Material name |
| `shaderName` | string | No | "Standard" | Shader to use |
| `savePath` | string | No | null | Asset save path |

### material_set_color

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | No* | - | GameObject name |
| `path` | string | No* | - | Material asset path |
| `r` | float | No | 1 | Red (0-1) |
| `g` | float | No | 1 | Green (0-1) |
| `b` | float | No | 1 | Blue (0-1) |
| `a` | float | No | 1 | Alpha (0-1) |
| `propertyName` | string | No | "_Color" | Color property |

*Either name or path required

### material_set_texture

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | GameObject name |
| `path` | string | No* | Material asset path |
| `texturePath` | string | Yes | Texture asset path |
| `propertyName` | string | No | "_MainTex" |

### material_assign

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | GameObject name |
| `materialPath` | string | Yes | Material asset path |
| `slot` | int | No | 0 (material index) |

### material_set_float

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | GameObject name |
| `path` | string | No* | Material asset path |
| `propertyName` | string | Yes | Property name |
| `value` | float | Yes | Property value |

## Common Shader Properties

**Standard Shader:**
- `_Color` - Albedo color
- `_MainTex` - Albedo texture
- `_Metallic` - Metallic value (0-1)
- `_Glossiness` - Smoothness (0-1)
- `_BumpMap` - Normal map
- `_EmissionColor` - Emission color

**URP Lit:**
- `_BaseColor` - Base color
- `_BaseMap` - Base texture
- `_Smoothness` - Smoothness
- `_Metallic` - Metallic

## Example Usage

```python
import unity_skills

# Create a red material
unity_skills.call_skill("material_create",
    name="RedMaterial",
    shaderName="Standard",
    savePath="Assets/Materials/RedMaterial.mat"
)

# Set the color
unity_skills.call_skill("material_set_color",
    path="Assets/Materials/RedMaterial.mat",
    r=1, g=0, b=0
)

# Make it metallic
unity_skills.call_skill("material_set_float",
    path="Assets/Materials/RedMaterial.mat",
    propertyName="_Metallic",
    value=0.8
)

# Set a texture
unity_skills.call_skill("material_set_texture",
    path="Assets/Materials/RedMaterial.mat",
    texturePath="Assets/Textures/metal.png"
)

# Assign to object
unity_skills.call_skill("material_assign",
    name="MyCube",
    materialPath="Assets/Materials/RedMaterial.mat"
)

# Quick color change on object (creates instance)
unity_skills.call_skill("material_set_color",
    name="MyCube",
    r=0, g=1, b=0  # Green
)
```

## Response Format

```json
{
  "status": "success",
  "skill": "material_create",
  "result": {
    "success": true,
    "name": "RedMaterial",
    "path": "Assets/Materials/RedMaterial.mat",
    "shader": "Standard"
  }
}
```

## Best Practices

1. Save materials as assets for reuse
2. Use material instances (by name) for runtime changes
3. Use material assets (by path) for persistent changes
4. Check shader property names in Unity Inspector
5. URP/HDRP have different property names than Standard
