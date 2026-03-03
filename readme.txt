ConverToUTF8 使用说明
=====================

一、功能
将指定目录下的文本文件自动识别编码后，统一转换成目标编码。
程序会递归扫描子目录。

二、运行方法
命令格式：
ConverToUTF8.exe <目录路径> <目标编码>

参数说明：
1) 第一个参数：目录路径
	例如：M:\Temp\1
2) 第二个参数：目标编码
	例如：utf8

三、示例
ConverToUTF8.exe M:\Temp\1 utf8
ConverToUTF8.exe M:\Temp\1 utf8-bom
ConverToUTF8.exe M:\Temp\1 gb18030

四、常见目标编码
utf8        = UTF-8 无 BOM
utf8-bom    = UTF-8 带 BOM
utf16le
utf16be
utf32le
utf32be
gb18030
ascii
iso-8859-1

五、处理规则
1) 扫描常见文本文件扩展名（如 .txt .cs .json .xml 等）
2) 自动跳过疑似二进制文件
3) 尝试识别 BOM（UTF-8/UTF-16/UTF-32）
4) 无 BOM 时优先按 UTF-8 校验，再判断 UTF-16 特征，最后回退 GB18030
5) 仅当转换后内容字节有变化时才写回文件

六、返回码
0 = 全部成功
1 = 参数不足
2 = 目录不存在
3 = 目标编码不支持
4 = 处理过程中有文件失败

七、VS Code 一键发布 AOT（x64）
本项目已配置默认构建任务：发布AOT (x64)
可用 Ctrl+Shift+B 直接发布。

对应命令：
dotnet publish -c Release -r win-x64

发布目录：
bin\Release\net9.0\win-x64\publish\