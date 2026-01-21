#!/usr/bin/env python3
"""
UnitySkills - Minimal Python client for Unity REST API.
AI agents use this library to directly control Unity Editor.

Usage:
    import unity_skills
    unity_skills.create_cube(x=1, y=2, z=3)
"""

import requests
from typing import Any, Dict, Optional

UNITY_URL = "http://localhost:8090"


def call_skill(name: str, **kwargs) -> Dict[str, Any]:
    """
    Call a Unity skill by name.
    
    Args:
        name: Skill name (e.g., "create_cube")
        **kwargs: Skill parameters
        
    Returns:
        Response dict with 'status' and 'result' keys
    """
    try:
        response = requests.post(
            f"{UNITY_URL}/skill/{name}",
            json=kwargs,
            timeout=30
        )
        return response.json()
    except requests.exceptions.ConnectionError:
        return {"status": "error", "error": "Unity not running. Start REST server in Unity: Window > UnitySkills > Start REST Server"}
    except Exception as e:
        return {"status": "error", "error": str(e)}


def get_skills() -> Dict[str, Any]:
    """Get list of all available skills."""
    try:
        response = requests.get(f"{UNITY_URL}/skills", timeout=5)
        return response.json()
    except Exception as e:
        return {"status": "error", "error": str(e)}


def health() -> bool:
    """Check if Unity server is running."""
    try:
        response = requests.get(f"{UNITY_URL}/health", timeout=2)
        return response.json().get("status") == "ok"
    except:
        return False


def is_unity_running() -> bool:
    """Alias for health() - check if Unity server is running."""
    return health()


def wait_for_unity(timeout: int = 30, interval: float = 0.5) -> bool:
    """
    Wait for Unity server to become available.
    Useful after script creation triggers domain reload.
    
    Args:
        timeout: Maximum seconds to wait
        interval: Seconds between checks
        
    Returns:
        True if Unity became available, False if timeout
    """
    import time
    start = time.time()
    while time.time() - start < timeout:
        if health():
            return True
        time.sleep(interval)
    return False


# ============================================================
# Convenience functions for common skills
# ============================================================

def create_cube(x: float = 0, y: float = 0, z: float = 0, name: str = "Cube") -> Dict:
    """Create a cube at the specified position."""
    return call_skill("gameobject_create", name=name, primitiveType="Cube", x=x, y=y, z=z)


def create_sphere(x: float = 0, y: float = 0, z: float = 0, name: str = "Sphere") -> Dict:
    """Create a sphere at the specified position."""
    return call_skill("gameobject_create", name=name, primitiveType="Sphere", x=x, y=y, z=z)


def set_object_color(object_name: str, r: float = 1, g: float = 1, b: float = 1) -> Dict:
    """Set the color of a GameObject's material."""
    return call_skill("material_set_color", gameObjectName=object_name, r=r, g=g, b=b)


def delete_object(object_name: str) -> Dict:
    """Delete a GameObject by name."""
    return call_skill("gameobject_delete", name=object_name)


def get_scene_info() -> Dict:
    """Get information about the current scene."""
    return call_skill("scene_get_info")


def find_objects_by_tag(tag: str) -> Dict:
    """Find all GameObjects with a specific tag."""
    return call_skill("gameobject_find", tag=tag)


# ============================================================
# CLI for testing
# ============================================================

if __name__ == "__main__":
    import sys
    import json
    
    if len(sys.argv) < 2:
        print("Usage: python unity_skills.py <skill_name> [param=value ...]")
        print("       python unity_skills.py --list")
        print("       python unity_skills.py --health")
        sys.exit(1)
    
    if sys.argv[1] == "--list":
        print(json.dumps(get_skills(), indent=2))
    elif sys.argv[1] == "--health":
        print("Unity server is", "running" if health() else "not running")
    else:
        skill_name = sys.argv[1]
        kwargs = {}
        for arg in sys.argv[2:]:
            if "=" in arg:
                key, value = arg.split("=", 1)
                # Try to parse as number
                try:
                    value = float(value)
                    if value.is_integer():
                        value = int(value)
                except ValueError:
                    pass
                kwargs[key] = value
        
        result = call_skill(skill_name, **kwargs)
        print(json.dumps(result, indent=2))
