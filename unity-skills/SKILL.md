---
name: unity-skills
description: "Control Unity Editor via REST API. BATCH-FIRST: Always use _batch APIs when operating on >1 objects."
---

# Unity Skills API Reference

## 🚨 CRITICAL RULE: BATCH-FIRST
When operating on MORE THAN ONE object, you MUST use the `_batch` version of the skill.
Individual calls are PROHIBITED for multi-object operations. This applies to ALL categories below.

## Connection
```python
import unity_skills
client = unity_skills.connect(target="ProjectName")  # or connect(port=8090)
result = client.call("skill_name", param1=value1, verbose=False)
```

## Response Format
All skills return: `{"status": "success"|"error", "result": {...}}` or `{"status": "error", "error": "message"}`

---

## BATCH SKILLS REFERENCE (USE THESE FIRST)

### GameObject Batch
| Skill | items format |
|-------|--------------|
| `gameobject_create_batch` | `[{name, primitiveType?, x?, y?, z?}]` |
| `gameobject_delete_batch` | `["name1", "name2"]` or `[{name?, instanceId?}]` |
| `gameobject_set_transform_batch` | `[{name, posX?, posY?, posZ?, rotX?, rotY?, rotZ?, scaleX?, scaleY?, scaleZ?}]` |
| `gameobject_set_active_batch` | `[{name, active}]` |
| `gameobject_set_layer_batch` | `[{name, layer, recursive?}]` |
| `gameobject_set_tag_batch` | `[{name, tag}]` |
| `gameobject_set_parent_batch` | `[{childName, parentName}]` |

### Component Batch
| Skill | items format |
|-------|--------------|
| `component_add_batch` | `[{name, componentType}]` |
| `component_set_property_batch` | `[{name, componentType, propertyName, value}]` |
| `component_remove_batch` | `[{name, componentType}]` |

### Material Batch
| Skill | items format |
|-------|--------------|
| `material_create_batch` | `[{name, shaderName?, savePath?}]` |
| `material_assign_batch` | `[{name, materialPath}]` |
| `material_set_colors_batch` | `[{name, r, g, b, a?, propertyName?}]` |
| `material_set_emission_batch` | `[{name, r, g, b, intensity?, enableEmission?}]` |

### Asset Batch
| Skill | items format |
|-------|--------------|
| `asset_import_batch` | `[{sourcePath, destinationPath}]` |
| `asset_delete_batch` | `[{path}]` |
| `asset_move_batch` | `[{sourcePath, destinationPath}]` |

### Prefab Batch
| Skill | items format |
|-------|--------------|
| `prefab_instantiate_batch` | `[{prefabPath, x?, y?, z?, name?}]` |

### UI Batch
| Skill | items format |
|-------|--------------|
| `ui_create_batch` | `[{type, name, parent?, text?, ...}]` - type: Button/Text/Image/Panel/Slider/Toggle/InputField |

### Script Batch
| Skill | items format |
|-------|--------------|
| `script_create_batch` | `[{scriptName, folder?, template?, namespace?}]` |

---

## SINGLE-OBJECT SKILLS (Use only for 1 object)

### GameObject
- `gameobject_create(name, primitiveType?, x?, y?, z?)` - primitiveType: Cube/Sphere/Capsule/Cylinder/Plane/Quad/Empty
- `gameobject_delete(name?, instanceId?, path?)`
- `gameobject_find(name?, tag?, layer?, component?, useRegex?, limit?)`
- `gameobject_set_transform(name, posX?, posY?, posZ?, rotX?, rotY?, rotZ?, scaleX?, scaleY?, scaleZ?)`
- `gameobject_set_parent(name, parentName)`
- `gameobject_set_active(name, active)`
- `gameobject_get_info(name?, instanceId?, path?)`

### Component
- `component_add(name, componentType)` - componentType: Rigidbody, BoxCollider, Light, AudioSource, etc.
- `component_remove(name, componentType)`
- `component_list(name)`
- `component_set_property(name, componentType, propertyName, value)`
- `component_get_properties(name, componentType)`

### Material
- `material_create(name, shaderName?, savePath?)`
- `material_assign(name?, instanceId?, path?, materialPath)`
- `material_set_color(name?, path?, r, g, b, a?, propertyName?)`
- `material_set_emission(name?, path?, r, g, b, intensity?, enableEmission?)`
- `material_set_texture(name?, path?, texturePath, propertyName?)`
- `material_set_float(name?, path?, propertyName, value)`
- `material_duplicate(sourcePath, newName, savePath?)`
- `material_get_properties(name?, path?)`

### Scene
- `scene_create(scenePath)`
- `scene_load(scenePath, additive?)`
- `scene_save(scenePath?)`
- `scene_get_info()`
- `scene_get_hierarchy(maxDepth?)`
- `scene_screenshot(filename?, width?, height?)`

### Light
- `light_create(name, lightType, x?, y?, z?, r?, g?, b?, intensity?, range?, shadows?)`
- `light_set_properties(name, r?, g?, b?, intensity?, range?, shadows?)`
- `light_get_info(name)`
- `light_find_all(lightType?, limit?)`
- `light_set_enabled(name, enabled)`

### Prefab
- `prefab_create(gameObjectName, savePath)`
- `prefab_instantiate(prefabPath, x?, y?, z?, name?)`
- `prefab_apply(gameObjectName)`
- `prefab_unpack(gameObjectName, completely?)`

### Asset
- `asset_import(sourcePath, destinationPath)`
- `asset_delete(assetPath)`
- `asset_move(sourcePath, destinationPath)`
- `asset_duplicate(assetPath)`
- `asset_find(searchFilter?, limit?)`
- `asset_create_folder(folderPath)`
- `asset_refresh()`
- `asset_get_info(assetPath)`

### UI
- `ui_create_canvas(name, renderMode?)` - renderMode: ScreenSpaceOverlay/ScreenSpaceCamera/WorldSpace
- `ui_create_panel(name, parent?, r?, g?, b?, a?)`
- `ui_create_button(name, parent?, text?, width?, height?)`
- `ui_create_text(name, parent?, text?, fontSize?)`
- `ui_create_image(name, parent?, spritePath?, width?, height?)`
- `ui_create_inputfield(name, parent?, placeholder?)`
- `ui_create_slider(name, parent?, minValue?, maxValue?, value?)`
- `ui_create_toggle(name, parent?, label?, isOn?)`
- `ui_set_text(name, text)`
- `ui_find_all(uiType?, limit?)`

### Script
- `script_create(scriptName, folder?, template?, namespace?)`
- `script_read(scriptPath)`
- `script_delete(scriptPath)`
- `script_find_in_file(pattern, folder?, isRegex?, limit?)`
- `script_append(scriptPath, content, atLine?)`

### Editor
- `editor_play()` / `editor_stop()` / `editor_pause()`
- `editor_select(gameObjectName?, instanceId?)`
- `editor_get_selection()`
- `editor_undo()` / `editor_redo()`
- `editor_get_state()`
- `editor_execute_menu(menuPath)`
- `editor_get_tags()` / `editor_get_layers()`

### Console
- `console_start_capture()` / `console_stop_capture()`
- `console_get_logs(filter?, limit?)`
- `console_clear()`
- `console_log(message, type?)`

### Animator
- `animator_create_controller(name, folder?)`
- `animator_add_parameter(controllerPath, paramName, paramType)` - paramType: float/int/bool/trigger
- `animator_get_parameters(controllerPath)`
- `animator_set_parameter(name, paramName, paramType, floatValue?/intValue?/boolValue?)`
- `animator_play(name, stateName, layer?)`
- `animator_get_info(name)`
- `animator_assign_controller(name, controllerPath)`
- `animator_list_states(controllerPath, layer?)`

### Shader
- `shader_create(shaderName, savePath?, template?)`
- `shader_read(shaderPath)`
- `shader_list(filter?, limit?)`

### Validation
- `validate_scene(checkMissingScripts?, checkMissingPrefabs?, checkDuplicateNames?)`
- `validate_find_missing_scripts(searchInPrefabs?)`
- `validate_cleanup_empty_folders(rootPath?, dryRun?)`
- `validate_find_unused_assets(assetType?, limit?)`
- `validate_texture_sizes(maxRecommendedSize?, limit?)`
- `validate_project_structure(rootPath?, maxDepth?)`
- `validate_fix_missing_scripts(dryRun?)`

---

## Notes
1. All operations are **Transactional**: Failures auto-revert entire operation.
2. Use `verbose=True` only when you need full details for large result sets.
3. After `script_create`, Unity reloads. Wait before next call.
