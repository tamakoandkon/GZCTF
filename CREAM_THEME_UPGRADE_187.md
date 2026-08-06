# GZCTF 1.8.7 浅色主题米黄背景还原说明

> 升级 1.8.2 → 1.8.7 后，浅色模式下主页 / 文章 / 赛事等页面的米黄背景（`#F3F2EB`）消失。本文记录根因、修复内容与验证结果。

## 背景

- 1.8.2 使用 Mantine **v8.3.15**，页面内容区背景为米黄 `gray-0` = `#F3F2EB`。
- 1.8.7 升级到 Mantine **v9.4.1**，页面内容区背景变成了近白 `light-2` = `#EFEFEF`，暖米黄的色调消失。

## 根因（三个层面）

### 1. 主题色板被上游改动 —— 已由提交 `98eecfc7` 修复

上游 1.8.7 的 `ThemeOverride.ts` 把：

| 色板 | 1.8.2 | 上游 1.8.7 | 本地修复后 |
|---|---|---|---|
| `gray[0]` | `#F3F2EB`（米黄） | `#EBEBEB`（灰） | `#F3F2EB` ✅ |
| `light[0]` | `#FDFDFD` | `#FFFFFF`（纯白） | `#FDFDFD` ✅ |

### 2. 页面内容区背景被改 —— **本次修复的核心**

页面内容容器 `.main`（`AppNavbar.module.css`）与页面标题条（`IconHeader.module.css`）的背景从 `gray-0` 改成了 `light-2`：

| 规则 | 1.8.2 | 1.8.7 |
|---|---|---|
| `.main`（铺满视口的内容 Stack） | `light-dark(var(--mantine-color-gray-0), …)` = `#F3F2EB` 米黄 | `light-dark(var(--mantine-color-light-2), …)` = `#EFEFEF` 近白 |
| `IconHeader`（页面标题条） | `gray-0` = 米黄 | `light-2` = 近白 |

关键点：`<body>` 在 1.8.2 和 1.8.7 里都是米黄，但 `body` 被 `.main` 这个 `min-height: 100vh` 的 Stack **整屏盖住**。所以用户看到的背景其实是 `.main` 的颜色——它从米黄变成了近白，这就是"米黄消失"的直接原因。

### 3. `--mantine-color-body` 覆盖属于过度修正 —— **本次撤销**

提交 `b40178c5` 曾把浅色模式 `--mantine-color-body` 覆盖为 `gray-0`。经核对两个版本的编译产物：

- Mantine **v8 和 v9 里 `Paper` / `AppShell` / `Modal` / `Table` 粘性表头等组件的 `--mantine-color-body` 用法完全一致**，浅色下默认都是 `#fff`，v9 并未新增该行为。
- 因此该覆盖让 `Paper` / `Modal` 从 1.8.2 的**白色**变成了**米黄**，属于偏离 1.8.2 的过度修正。
- 为严格还原 1.8.2，本次将其撤销（`App.css` 恢复为 1.8.2 原样）。`body` 的米黄仍由 `App.css` 的 `background-color: light-dark(var(--mantine-color-gray-0), …)` 提供。

## 本次改动内容

```diff
# src/GZCTF/ClientApp/src/styles/App.css —— 撤销 --mantine-color-body 覆盖
-/* Mantine v9 sets AppShell/Paper backgrounds to --mantine-color-body, ... */
-:root[data-mantine-color-scheme='light'] {
-  --mantine-color-body: var(--mantine-color-gray-0);
-}

# src/GZCTF/ClientApp/src/styles/components/AppNavbar.module.css —— 内容区还原米黄
-  background-color: light-dark(var(--mantine-color-light-2), var(--mantine-color-gray-7));   /* .main */
+  background-color: light-dark(var(--mantine-color-gray-0), var(--mantine-color-gray-7));

# 侧边栏 navbar 还原 1.8.2 的 light-1
-  background-color: light-dark(var(--mantine-color-light-0), var(--mantine-color-gray-8));   /* .navbar */
+  background-color: light-dark(var(--mantine-color-light-1), var(--mantine-color-gray-8));

# src/GZCTF/ClientApp/src/styles/components/IconHeader.module.css —— 标题条还原米黄
-  background-color: light-dark(var(--mantine-color-light-2), var(--mantine-color-gray-7));   /* .group 与 [data-sticky]::before */
+  background-color: light-dark(var(--mantine-color-gray-0), var(--mantine-color-gray-7));
```

## 验证结果

真实浏览器实测运行中的 1.8.7 实例（浅色模式），逐页读取计算样式：

| 检查项 | 1.8.2 | 1.8.7（修复前） | 1.8.7（本次修复后） |
|---|---|---|---|
| `<body>` 背景 | `#F3F2EB` | `#F3F2EB` | `#F3F2EB` |
| 内容区 `.main` Stack | `#F3F2EB` 米黄 | `#EFEFEF` 近白 ❌ | `#F3F2EB` 米黄 ✅ |
| 卡片 `Card` | 白 | 白 | 白 |
| `IconHeader` 标题条 | `#F3F2EB` | `#EFEFEF` ❌ | `#F3F2EB` ✅ |

> 注意：`Card` 组件在 v8 / v9 中都硬编码 `background-color: var(--mantine-color-white)`，不走 `--mantine-color-body`，所以卡片始终是白色，与 1.8.2 一致。

## 部署方式

```bash
cd src/GZCTF/ClientApp
pnpm install
pnpm build          # 重新生成 ClientApp/build/static/*.css
cd ../..
dotnet publish     # 或按项目现有发布流程重新打包镜像 / 发布目录
```

重新部署后浅色模式即恢复 1.8.2 的米黄观感。
