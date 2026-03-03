# ConverToUTF8

批量扫描目录中的文本文件，自动识别源编码并转换为指定目标编码。

## 环境

- .NET SDK 9

## 用法

```bash
ConverToUTF8 <目录> <目标编码>
```

示例：

```bash
ConverToUTF8 D:\Project utf8
ConverToUTF8 D:\Project utf8-bom
ConverToUTF8 D:\Project gb18030
```

## 支持的常见目标编码

- utf8（UTF-8 无 BOM）
- utf8-bom（UTF-8 带 BOM）
- utf16le
- utf16be
- utf32le
- utf32be
- gb18030
- ascii
- iso-8859-1

也支持传入 `Encoding.GetEncoding(...)` 可识别的编码名称。

## 扫描与转换规则

- 递归扫描输入目录
- 仅处理常见文本扩展名（如 `.txt`、`.cs`、`.json`、`.xml` 等）
- 自动跳过疑似二进制文件
- 自动识别 BOM（UTF-8/UTF-16/UTF-32）
- 无 BOM 文件优先按 UTF-8 校验，其次按 UTF-16 特征判断，最后回退到 GB18030

## AOT 发布

当前项目已启用 AOT 配置。

发布命令（Windows x64）：

```bash
dotnet publish -c Release -r win-x64
```

发布输出目录：

- `bin/Release/net9.0/win-x64/publish/`

## VS Code 一键 Build AOT

已添加默认任务按钮：`发布AOT (x64)`。

- 直接按 `Ctrl+Shift+B` 可触发默认构建任务：`发布AOT (x64)`
- 自动执行：`dotnet publish -c Release -r win-x64`

如需“底部状态栏可点击按钮”，请安装推荐扩展：`actboy168.tasks`（项目已在 `.vscode/extensions.json` 推荐）。
