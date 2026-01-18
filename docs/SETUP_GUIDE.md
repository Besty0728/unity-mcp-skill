# UnitySkills 配置与测试指南

本指南将帮助你从零开始配置和测试 UnitySkills。

---

## 前置要求

| 工具 | 版本 | 用途 |
|------|------|------|
| Unity | 2021.3+ | 编辑器 |
| Python | 3.8+ | 运行测试脚本 |

---

## 第一步：安装 Unity Package

### 方式 A：通过 Git URL
1. 打开 Unity 项目
2. **Window > Package Manager**
3. 点击 `+` → **Add package from git URL**
4. 输入：
   ```
   https://github.com/你的用户名/unity-mcp-skill.git?path=/MCPForUnity
   ```

### 方式 B：本地安装
1. 将 `MCPForUnity` 文件夹复制到你的 Unity 项目的 `Packages/` 目录

---

## 第二步：启动 REST 服务器

1. 打开 Unity 编辑器
2. 菜单：**Window > UnitySkills > Start REST Server**
3. Console 应显示：
   ```
   [UnitySkills] REST Server started at http://localhost:8090/
   [UnitySkills] Endpoints: GET /skills, POST /skill/{name}
   ```

---

## 第三步：验证服务器

### 使用浏览器
打开：http://localhost:8090/skills

应看到类似：
```json
{
  "version": "1.0.0",
  "skills": [
    {
      "name": "create_cube",
      "description": "Create a cube at the specified position"
    }
  ]
}
```

### 使用命令行
```bash
curl http://localhost:8090/skills
```

---

## 第四步：测试技能调用

### 使用 curl
```bash
# 创建立方体
curl -X POST http://localhost:8090/skill/create_cube \
  -H "Content-Type: application/json" \
  -d '{"x": 0, "y": 1, "z": 0, "name": "TestCube"}'

# 获取场景信息
curl -X POST http://localhost:8090/skill/get_scene_info
```

### 使用 Python
```bash
cd claude_skill_unity/claude_skill_unity/scripts
pip install requests
python unity_skills.py --list
python unity_skills.py create_cube x=0 y=1 z=0 name=TestCube
```

---

## 第五步：在 AI 中使用

### Claude Desktop
1. 将 `claude_skill_unity` 文件夹添加为 Claude Skill
2. 确保 Unity REST 服务器正在运行
3. 对话示例：
   ```
   请在 Unity 中创建一个红色立方体
   ```

### 预期 AI 生成代码
```python
import unity_skills
unity_skills.create_cube(x=0, y=1, z=0, name="RedCube")
unity_skills.set_object_color("RedCube", r=1, g=0, b=0)
```

---

## 故障排除

| 问题 | 解决方案 |
|------|----------|
| 无法连接 localhost:8090 | 确保已启动 REST Server：Window > UnitySkills > Start REST Server |
| 技能未找到 | 检查 C# 方法是否有 `[UnitySkill]` 特性 |
| Python requests 错误 | 运行 `pip install requests` |

---

## 添加自定义技能

在 Unity 项目的 `Editor/` 文件夹创建 C# 文件：

```csharp
using MCPForUnity.Editor.Tools;

public static class MyCustomSkills
{
    [UnitySkill("my_skill", "描述给 AI 看")]
    public static string MySkill(string param1, float param2 = 0)
    {
        // 你的逻辑
        return "Success";
    }
}
```

重启 REST 服务器后，新技能自动可用。

---

## 文件结构

```
unity-mcp-skill/
├── MCPForUnity/              # Unity Package
│   └── Editor/
│       └── Skills/
│           ├── SkillsHttpServer.cs  # HTTP 服务器
│           ├── SkillRouter.cs       # 请求路由
│           ├── SampleSkills.cs      # 示例技能
│           └── UnitySkillAttribute.cs
├── claude_skill_unity/       # Claude Skill
│   └── claude_skill_unity/
│       ├── SKILL.md          # AI 参考文档
│       └── scripts/
│           └── unity_skills.py  # Python 客户端
└── docs/
    └── SETUP_GUIDE.md        # 本文档
```
