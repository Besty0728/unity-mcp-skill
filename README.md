# UnitySkills

通过 REST API 直接控制 Unity Editor，让 AI 生成极简脚本完成场景操作。

## 特点

- ✅ **极简调用** - 3 行 Python 代码完成操作
- ✅ **无 MCP 开销** - 直接 HTTP 通信
- ✅ **80%+ Token 节省** - 相比传统方式

## 快速开始

### 1. 安装
```
Window > Package Manager > + > Add package from git URL
https://github.com/Besty0728/unity-skills.git?path=/MCPForUnity
```

### 2. 启动服务器
```
Window > UnitySkills > Start REST Server
```

### 3. 使用
```python
import unity_skills
unity_skills.create_cube(x=0, y=1, z=0)
```

## 文档

- [配置指南](docs/SETUP_GUIDE.md)
- [Skills 参考](claude_skill_unity/claude_skill_unity/SKILL.md)

## 目录结构

```
├── MCPForUnity/          # Unity Package
├── claude_skill_unity/   # Claude Skill
└── docs/                 # 文档
```

## License

MIT
