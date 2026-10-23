# GZCTF 自定义版源码构建指南（v1.9.0）

本文说明如何在任意 Linux 服务器上从本仓库构建 GZCTF 镜像。源码、许可证和部署文件可以放在服务器上的任意目录；下面的命令不依赖固定用户名、IP、端口或容器名。

## 准备环境

- .NET SDK 10.0（`dotnet --version`）
- Node.js 22 或更新版本（`node --version`）及 pnpm（`pnpm --version`）
- Docker，包含 BuildKit 和 Compose 插件
- 有效的 Six Labors 许可证文件。请保存在仓库外，不要提交到 Git。
- 构建时可访问 NuGet、npm，以及 Dockerfile 中下载 Satori 运行时文件的地址。

克隆本仓库，并将许可证的**绝对路径**设为环境变量：

```bash
git clone -b develop https://github.com/tamakoandkon/GZCTF.git
cd GZCTF
export SIXLABORS_LICENSE_FILE=/path/to/sixlabors.lic

command -v dotnet && command -v node && command -v pnpm && command -v docker
test -f "$SIXLABORS_LICENSE_FILE"
```

请将 `/path/to/sixlabors.lic` 换成这台服务器上的实际路径。已有源码目录时，可直接进入该目录，从设置环境变量开始。

## 编译并构建镜像

在仓库根目录执行以下命令。根据本机 CPU 架构选择 .NET 运行时标识和 Docker 平台：

```bash
case "$(uname -m)" in
  x86_64)       ARCH=amd64; RID=linux-x64 ;;
  aarch64|arm64) ARCH=arm64; RID=linux-arm64 ;;
  *) echo "不支持的 CPU 架构：$(uname -m)" >&2; exit 1 ;;
esac

cd src/GZCTF
dotnet publish -c Release -r "$RID" --self-contained false \
  -o "publish/linux/$ARCH" \
  -p:SixLaborsLicenseFile="$SIXLABORS_LICENSE_FILE"

docker build --platform "linux/$ARCH" -t gzctf:local .
```

`dotnet publish` 会安装前端依赖并构建前端。`src/GZCTF/Dockerfile` 从构建上下文读取 `publish/linux/$ARCH`，所以 `docker build` 必须在 `src/GZCTF` 目录执行。镜像标签 `gzctf:local` 可以按需要改名，但部署配置中的 `image` 必须与之对应。

## 部署或更新现有服务

新服务器先按 [GZCTF 官方部署文档](https://gzctf.gzti.me/zh)准备数据库、持久化数据、运行配置和 Compose 文件。将 Compose 文件中平台服务的 `image` 设为刚构建的镜像标签，例如 `gzctf:local`。这些部署文件和密钥应保存在仓库外；端口、服务名及路径由各服务器自行配置。

更新已有服务前，先备份数据库和持久化文件。随后在 **Compose 文件所在目录**执行：

```bash
docker compose up -d <平台服务名>
docker compose ps
```

把 `<平台服务名>` 换成 Compose 文件里 GZCTF 服务的实际名称。访问该服务器配置的地址或健康检查路径验证运行状态。升级数据库后通常不能直接降回旧版本；需要回退时使用升级前的备份。

## 常见问题

| 现象 | 检查方法 |
| --- | --- |
| 提示缺少 Six Labors 许可证 | 检查 `SIXLABORS_LICENSE_FILE` 是否指向存在且可读的文件，并传入 `dotnet publish`。 |
| `docker build` 找不到 `/build/linux/...` | 检查 `dotnet publish` 的输出目录是否为 `src/GZCTF/publish/linux/$ARCH`，并从 `src/GZCTF` 运行 `docker build`。 |
| Dockerfile 下载 Satori 失败 | 检查构建机到 GitHub Releases 的网络连接，再重试镜像构建。 |
| 更新镜像后服务仍是旧版本 | 检查 Compose 中的 `image` 标签与构建标签一致，并确认平台服务已重新创建。 |
