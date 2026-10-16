# EmbodiedPhysicsLab

支持具身化交互的虚拟物理实验室 Unity 项目。

目前项目已经实现实验大厅、第一人称虚拟化身，以及三个基础物理实验场景。当前版本可直接使用普通电脑键盘和鼠标运行，后续将继续加入多人协同和 VR 设备支持。

## 1. 开发环境

推荐使用以下环境打开项目：

- Unity：**6000.3.11f1（Unity 6.3 LTS）**
- 操作系统：Windows
- 渲染管线：URP
- OpenXR Plugin：1.16.1
- XR Plug-in Management：4.5.4
- Meta XR SDK：201.0.0
- Meta XR Movement SDK：201.0.0

项目所需 Unity Package 已记录在 `Packages/` 中，使用正确版本的 Unity 打开项目后会自动解析和安装依赖。

目前没有 VR 头显也可以运行桌面版进行测试。

## 2. 如何打开项目

克隆仓库后，在 Unity Hub 中选择：

`添加 → 从磁盘添加项目`

然后选择仓库根目录：

```text
EmbodiedPhysicsLab/
