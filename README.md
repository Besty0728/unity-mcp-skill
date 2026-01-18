# 🎮 UnitySkills

> **通过 REST API 直接控制 Unity Editor** — 让 AI 生成极简脚本完成场景操作。

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)

---

**UnitySkills** 是一个轻量级的 Unity 插件，允许 AI Agent 通过 HTTP 协议直接控制 Unity 编辑器。无需复杂的 MCP 协议配置，开箱即用。

> 💡 本项目基于 [unity-mcp](https://github.com/CoplayDev/unity-mcp) 开发。遵循 MIT 协议。

## ✨ 核心特点

- 🚀 **极简调用** - 仅需 3 行 Python 代码即可与 Unity 交互
- ⚡ **零开销** - 直接 HTTP 通信，无 MCP 中间层损耗
- 📉 **高效 Token** - 相比传统排查方式节省 80%+ Token

## 🏁 快速开始

### 1. 安装插件
在 Unity Package Manager 中通过 Git URL 添加：
```text
https://github.com/Besty0728/unity-mcp-skill.git?path=/MCPForUnity
```

### 2. 启动服务
在 Unity 菜单栏点击：
`Window > UnitySkills > Start REST Server`

### 3. Python 调用示例
```python
import unity_skills

# 创建一个立方体
unity_skills.create_cube(x=0, y=1, z=0)
```

## 📚 文档资源

- [🛠️ 配置指南](docs/SETUP_GUIDE.md)
- [📖 Skills API 参考](claude_skill_unity/claude_skill_unity/SKILL.md)

## 📂 目录结构

```text
├── MCPForUnity/          # Unity Package 源码
├── claude_skill_unity/   # Python/Claude 客户端 Skill
└── docs/                 # 项目文档
```

## 📄 License

本项目采用 [MIT License](LICENSE) 授权。
