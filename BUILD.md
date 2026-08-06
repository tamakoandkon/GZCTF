# GZCTF 源码构建指南（1.8.7）

> 适用于当前部署：源码仓库 `/home/tamako/GZ/upgrade/gzctf-upgrade`（1.8.7），部署目录 `/home/tamako/GZ/deploy`，容器 `gzctf-187`（端口 36306）。

## 环境要求

- .NET SDK 10.0+（本机装在 `/usr/share/dotnet`，**不在 PATH 里**，需先 `export PATH=/usr/share/dotnet:$PATH`）
- Node.js 22+
- pnpm（`npm install -g pnpm`）
- Docker & Docker Compose

## 目录结构

```
源码仓库 /home/tamako/GZ/upgrade/gzctf-upgrade/
├── src/GZCTF/
│   ├── Dockerfile              # 多阶段构建，COPY publish/linux/amd64 进镜像
│   ├── ClientApp/              # React 前端源码
│   ├── GZCTF.csproj            # PublishFrontend 目标会自动构建前端
│   └── publish/linux/amd64/    # dotnet publish 输出（Docker 镜像的输入）
│       └── wwwroot/static/     # 编译后的前端 JS/CSS

部署目录 /home/tamako/GZ/deploy/
├── docker-compose.yml          # 编排文件（无 build 段，直接用 gzctf-187-gzctf:local 镜像）
├── appsettings.json            # 应用配置（只读挂载进容器）
├── license/sixlabors.lic       # SixLabors 许可证（构建和运行都需要）
└── data/                       # 持久化数据（data/db 数据库、data/files 上传文件）
```

## 构建步骤

### 1. 修改源码

- **前端**：`src/GZCTF/ClientApp/src/` 下的 `.tsx`/`.ts`/`.css` 文件
- **后端**：`src/GZCTF/` 下的 `.cs` 文件

### 2. 编译并发布

```bash
export PATH=/usr/share/dotnet:$PATH
cd /home/tamako/GZ/upgrade/gzctf-upgrade/src/GZCTF

dotnet publish -c Release -r linux-x64 --self-contained false \
  -o publish/linux/amd64 \
  -p:SixLaborsLicenseFile=/home/tamako/GZ/deploy/license/sixlabors.lic
```

> 会自动执行：`pnpm install` → `tsc` 类型检查 → `vite build` → 编译 .NET → 输出到指定目录。
>
> ⚠️ **`-p:SixLaborsLicenseFile=...` 必填**，缺失会报 `No Six Labors license found` 并终止构建。
>
> `-o` 必须指向当前目录下的 `publish/linux/amd64`，与 Dockerfile 中的 `COPY publish /build` 对应。

### 3. 重建 Docker 镜像

```bash
cd /home/tamako/GZ/upgrade/gzctf-upgrade/src/GZCTF
docker build -t gzctf-187-gzctf:local .
```

> 用仓库内的 `src/GZCTF/Dockerfile`，**context 必须是 `src/GZCTF`**（它 COPY `publish/${TARGETPLATFORM}`）。
>
> Docker 缓存可能不会检测到文件变化，遇到前端修改没生效时强制重建：

```bash
docker build --no-cache -t gzctf-187-gzctf:local .
```

### 4. 重启服务

```bash
cd /home/tamako/GZ/deploy
docker compose up -d gzctf
```

> compose 项目名为 `deploy`（来自目录名），`docker compose` 会自动重建容器为最新镜像。数据持久化在 `deploy/data/`，重启不丢数据。

### 5. 验证

```bash
curl -s http://localhost:36306/ | head
docker ps --filter name=gzctf
```

## 更新运行时配置

页面 title、keywords 等配置存储在数据库 `Configs` 表中，修改源码不会影响它们。如需更新：

```bash
docker exec gzctf-187-db psql -U postgres -d gzctf -c \
  "UPDATE public.\"Configs\" SET \"Value\" = '<新值>' WHERE \"ConfigKey\" = 'GlobalConfig:Title';"

docker restart gzctf-187
```

常用 ConfigKey：

| Key | 说明 |
|-----|------|
| `GlobalConfig:Title` | 页面标题 |
| `GlobalConfig:Platform` | 平台名称（格式：`标题::CTF`） |
| `GlobalConfig:Description` | 页面描述 |

## 一键脚本

```bash
#!/bin/bash
set -e
REPO=/home/tamako/GZ/upgrade/gzctf-upgrade
DEPLOY=/home/tamako/GZ/deploy

echo "[1/4] dotnet publish..."
export PATH=/usr/share/dotnet:$PATH
cd $REPO/src/GZCTF
dotnet publish -c Release -r linux-x64 --self-contained false \
  -o publish/linux/amd64 \
  -p:SixLaborsLicenseFile=$DEPLOY/license/sixlabors.lic

echo "[2/4] docker build..."
docker build -t gzctf-187-gzctf:local .

echo "[3/4] docker compose up..."
cd $DEPLOY
docker compose up -d gzctf

echo "[4/4] done"
docker ps --filter name=gzctf
```

## 常见问题

| 现象 | 处理 |
|------|------|
| 构建报 `No Six Labors license found` | `dotnet publish` 缺少 `-p:SixLaborsLicenseFile=...` |
| 前端修改没生效 | `docker build --no-cache` 强制重建 |
| 控制台报 `wsrx ... connection refused` | 1.8.7 的流量捕获守护进程 `@xdsec/wsrx`（127.0.0.1:3307）未部署，可选功能，不影响使用 |
| 磁盘空间不足 | 清理旧镜像 `docker rmi gzctf-182-gzctf:latest`、旧构建产物 `src/GZCTF/ClientApp/build/` |
