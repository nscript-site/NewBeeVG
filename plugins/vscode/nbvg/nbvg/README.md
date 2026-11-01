# nbvg

NewBeeVG Visual Studio Code 插件：在编辑器标题栏直接为当前 C# 文件启动 `nbvg` 监听。

## Features

- 打开任意 `.cs` 文件时，编辑区右上角出现 `Watch` 按钮（眼睛图标）。
- 点击后在集成终端执行：
  ```
  nbvg.exe watch "<C# 文件的绝对路径>"
  ```
- 每个文件对应一个终端（名为 `nbvg: 文件名`），重复点击复用同一终端，关闭后自动回收。
- 也可以在命令面板中执行 `nbvg: Watch`。

## Requirements

`nbvg.exe` 需要在 `PATH` 中可用，或将其所在目录加入 `PATH`。

## Extension Settings

暂无。

## Known Issues

暂无。

## Release Notes

### 1.0.1

- 为 `.cs` 文件提供标题栏 `Watch` 按钮，执行 `nbvg.exe watch "<file>"`。
- 采用 MIT 许可证。
