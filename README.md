# 🎮 UnitySkills

> **通过 REST API 直接控制 Unity Editor** — 让 AI 生成极简脚本完成场景操作。

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)
![Skills](https://img.shields.io/badge/Skills-45%2B-green)

---

**UnitySkills** 是一个轻量级的 Unity 插件，允许 AI Agent 通过 HTTP 协议直接控制 Unity 编辑器。支持 **Claude Code**、**Antigravity**、**Gemini CLI** 等主流 AI 工具。

> 💡 本项目基于 [unity-mcp](https://github.com/CoplayDev/unity-mcp) 开发。遵循 MIT 协议。

## ✨ 核心特点

- 🚀 **极简调用** - 仅需 3 行 Python 代码即可与 Unity 交互
- ⚡ **零开销** - 直接 HTTP 通信，无 MCP 中间层损耗
- 📉 **高效 Token** - 相比传统排查方式节省 80%+ Token
- 🎯 **45+ Skills** - 覆盖 GameObject、Component、Material、Light、Animator、UI、Project 等 15 大类
- 🎨 **智能渲染管线** - 自动检测 Built-in/URP/HDRP，正确创建材质
- 🤖 **多 AI 支持** - 支持 Claude Code、Antigravity、Gemini CLI

## 🎮 支持的 Skill 分类

| 分类 | 功能 | Skills 数量 |
|-----|------|------------|
| **GameObject** | 创建、删除、查找、变换 | 7 |
| **Component** | 添加、移除、配置组件 | 5 |
| **Scene** | 场景管理、截图 | 6 |
| **Material** | 材质和着色器操作（智能管线检测） | 5 |
| **Prefab** | 预制体操作 | 4 |
| **Asset** | 资源管理 | 8 |
| **Light** | 灯光创建和配置 | 5 |
| **Animator** | 动画控制器管理 | 8 |
| **UI** | UI 元素创建 | 10 |
| **Editor** | 编辑器控制 | 11 |
| **Console** | 调试和日志 | 5 |
| **Script** | 脚本管理 | 4 |
| **Shader** | 着色器操作 | 3 |
| **Validation** | 项目验证和清理 | 7 |
| **Project** | 项目信息和渲染管线检测 | 4 |

## 🏁 快速开始

### 1. 安装 Unity 插件

在 Unity Package Manager 中通过 Git URL 添加：
```text
https://github.com/Besty0728/unity-mcp-skill.git?path=/MCPForUnity
```

### 2. 启动 REST 服务

在 Unity 菜单栏点击：
`Window > UnitySkills > Start REST Server`

> 服务默认运行在 `http://localhost:8080`

---

## 🤖 AI 工具配置

### 方式一：Claude Code / Antigravity（推荐）

将 `claude_skill_unity/claude_skill_unity/` 目录复制到你的 Skills 目录：

**Claude Code：**
```bash
# macOS/Linux
cp -r claude_skill_unity/claude_skill_unity ~/.claude/skills/unity

# Windows
xcopy /E claude_skill_unity\claude_skill_unity %USERPROFILE%\.claude\skills\unity\
```

**Antigravity：**
```bash
# macOS/Linux
cp -r claude_skill_unity/claude_skill_unity ~/.antigravity/skills/unity

# Windows
xcopy /E claude_skill_unity\claude_skill_unity %USERPROFILE%\.antigravity\skills\unity\
```

安装完成后，AI 将自动发现并使用 Unity Skills。

---

### 方式二：Gemini CLI

**前置要求：** Node.js 20+

**步骤 1：安装 Gemini CLI**
```bash
npm install -g @google/gemini-cli
```

**步骤 2：安装 Unity Skill**

**方式 A：使用 Unity 编辑器一键安装（推荐）**
1. 在 Unity 中打开 `Window > UnitySkills`
2. 切换到 "AI Config" 标签页
3. 在 "Gemini CLI" 部分点击 "安装到项目" 或 "全局安装"

**方式 B：手动复制**
```bash
# macOS/Linux
cp -r .gemini/skills/unity-skills ~/.gemini/skills/

# Windows
xcopy /E .gemini\skills\unity-skills %USERPROFILE%\.gemini\skills\unity-skills\
```

**步骤 3：启用 Skills 功能**

Gemini CLI 的 Skills 是实验性功能，需要在设置中启用：
```bash
gemini
# 进入交互模式后输入
/settings
# 搜索 "Skills" 并启用 experimental.skills
```

**步骤 4：使用**
```bash
cd /your/unity/project
gemini
# 输入: 帮我在 Unity 中创建一个红色立方体
```

> 💡 Gemini CLI 每日免费 1000 次请求

---

## 📝 Python 调用示例

```python
import requests

def call_skill(skill_name, **params):
    url = f"http://localhost:8080/skill/{skill_name}"
    response = requests.post(url, json=params)
    return response.json()

# 创建一个立方体
call_skill("gameobject_create", name="MyCube", primitiveType="Cube", x=0, y=1, z=0)

# 检测渲染管线
pipeline = call_skill("project_get_render_pipeline")
print(f"当前渲染管线: {pipeline['pipeline']}")  # Built-in, URP, 或 HDRP

# 设置颜色（自动适配渲染管线）
call_skill("material_set_color", name="MyCube", r=1, g=0, b=0)

# 创建材质（自动选择正确的着色器）
call_skill("material_create", name="MyMaterial", path="Assets/Materials/MyMaterial.mat")

# 创建点光源
call_skill("light_create", name="MyLight", lightType="Point", x=0, y=3, z=0, intensity=2)

# 创建 UI 按钮
call_skill("ui_create_canvas", name="MainMenu")
call_skill("ui_create_button", name="StartBtn", parent="MainMenu", text="开始游戏")

# 验证场景
call_skill("validate_scene", checkMissingScripts=True)
```

## 🎨 渲染管线智能检测

UnitySkills 会自动检测项目的渲染管线，并选择正确的着色器和属性名：

| 渲染管线 | 默认着色器 | 颜色属性 | 纹理属性 |
|---------|----------|---------|---------|
| **Built-in** | Standard | `_Color` | `_MainTex` |
| **URP** | Universal Render Pipeline/Lit | `_BaseColor` | `_BaseMap` |
| **HDRP** | HDRP/Lit | `_BaseColor` | `_BaseColorMap` |

> 创建材质时无需手动指定着色器，系统会自动选择适合当前项目的着色器

## 📚 文档资源

- [🛠️ 配置指南](docs/SETUP_GUIDE.md)
- [📖 Skills API 参考](claude_skill_unity/claude_skill_unity/SKILL.md)

## 📂 目录结构

```text
├── MCPForUnity/              # Unity Package 源码
│   └── Editor/Skills/        # 所有 Skill 实现
│       ├── GameObjectSkills.cs
│       ├── ComponentSkills.cs
│       ├── SceneSkills.cs
│       ├── MaterialSkills.cs     # 智能渲染管线检测
│       ├── PrefabSkills.cs
│       ├── AssetSkills.cs
│       ├── LightSkills.cs        # 灯光操作
│       ├── AnimatorSkills.cs     # 动画控制器
│       ├── UISkills.cs           # UI 创建
│       ├── ValidationSkills.cs   # 项目验证
│       ├── ProjectSkills.cs      # 项目信息和管线检测
│       ├── UnitySkillsWindow.cs  # Unity 编辑器窗口（一键安装）
│       ├── SkillInstaller.cs     # AI Skills 安装器
│       └── ...
├── claude_skill_unity/       # Claude Code / Antigravity Skills
│   └── claude_skill_unity/
│       ├── SKILL.md          # Skills 定义文件
│       ├── scripts/          # Python 脚本
│       ├── references/       # 参考文档
│       └── skills/           # 分类 Skills 文档
├── .gemini/                  # Gemini CLI Skills
│   └── skills/unity-skills/
│       └── SKILL.md          # Gemini Skills 定义
└── docs/                     # 项目文档
```

## ❓ 常见问题

### Q: 材质创建失败怎么办？

A: UnitySkills 会自动检测渲染管线并选择正确的着色器。如果仍然失败，可以先运行：
```python
call_skill("project_get_render_pipeline")
```
查看当前渲染管线，然后手动指定着色器：
```python
call_skill("material_create", name="MyMat", shaderName="Universal Render Pipeline/Lit")
```

### Q: 如何在 URP 项目中设置材质颜色？

A: 直接使用 `material_set_color`，系统会自动使用正确的属性名（`_BaseColor`）：
```python
call_skill("material_set_color", name="MyObject", r=1, g=0, b=0)
```

### Q: Gemini CLI 提示 Node.js 版本过低？

A: Gemini CLI 需要 Node.js 20+，请升级：
```bash
# 使用 nvm (推荐)
nvm install 20
nvm use 20

# 或直接从官网下载: https://nodejs.org/
```

### Q: Gemini CLI 找不到 Unity Skill？

A: 确保：
1. Skills 已复制到 `~/.gemini/skills/unity-skills/` 目录
2. 在 `/settings` 中启用了 `experimental.skills`
3. 运行 `/skills reload` 刷新技能列表
4. 运行 `/skills list` 查看是否已加载

### Q: Unity REST Server 无法启动？

A: 请检查：
1. 端口 8080 是否被占用
2. 尝试在 Unity Console 中查看错误信息
3. 确保 Unity 版本 >= 2021.3

## 📄 License

本项目采用 [MIT License](LICENSE) 授权。
