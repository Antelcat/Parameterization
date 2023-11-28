# Antelcat.Parameterization：轻松构建命令行应用程序的源代码生成器

### [English](readme.md) | 中文

欢迎使用 Antelcat.Parameterization，这是一个旨在彻底改变您创建命令行应用程序方式的强大源代码生成器。该工具通过仅使用类和方法上的属性标记，自动生成解析方法，简化了构建 CLI 应用程序的过程。

## 特点

- **属性驱动开发**：使用属性轻松定义命令和参数。
- **自动解析**：自动生成用于解析命令行参数的方法。
- **自定义类型转换器**：与 `System.ComponentModel.StringConverter` 良好兼容。

## 演示

[演示程序](https://github.com/Antelcat/Antelcat.Parameterization/blob/master/src/Antelcat.Parameterization.Demo/Program.cs)实现了一个简单的 [Docker](https://www.docker.com/) 应用程序来展示其功能。

1. **拉取镜像**：

   命令：

   ```bash
   > pull ubuntu
   ```

   输出：

   ```bash
   Pulling image ubuntu:latest...
   Successfully Pulled image ubuntu:latest
   ```

   在此示例中，自定义的 `Image` 类可以从 `string` 无缝转换，这多亏了应用于 `Image` 的 `[TypeConverter(typeof(ImageConverter))]` 属性。另外，对于局部使用，`[Argument(Converter = typeof(ImageConverter))] Image image` 注解允许进行同样的无缝转换。

2. **运行容器**：

   命令：

   ```bash
   > run ubuntu
   ```

   输出：

   ```bash
   ubuntu:latest ubuntu running
   ```

   这展示了当输入中未提供 `name` 参数时，如何自动使用其默认值。

3. **显示容器统计信息**：

   命令：

   ```bash
   > ps
   ```

   输出：

   ```bash
   CONTAINER_ID    IMAGE           NAME    STATUS
   00a57dbe        ubuntu:latest   ubuntu  running
   ```

4. **停止一个容器并运行一个新容器**：

   命令：

   ```bash
   > stop
   > stop 00a57dbe
   > run --name "my container" --image kali
   ```

   输出：

   ```bash
   Argument "id" is not specified.
   Stopping container 00a57dbe...
   Pulling image kali:latest...
   Successfully Pulled image kali:latest
   kali:latest my container running
   ```

   这个例子突出了命名开关的使用，允许参数重新排序。此外，它展示了如何正确解析用 `""` 包围的字符串。

5. **显示更新后的容器统计信息**：

   命令：

   ```bash
   > ps --all
   ```

   输出：

   ```bash
   CONTAINER_ID    IMAGE           NAME          STATUS
   00a57dbe        ubuntu:latest   ubuntu        stopped
   0419fcea        kali:latest     my container  running
   ```

   首先，`Argument(FullName = "all")` 替换了原始 `showAll` 的全名。`Argument(ShortName = "a")` 表示 `ps -a` 也能以同样的方式工作。

   其次，`ArgumentAttribute.DefaultValue` 的作用很明显。当一个开关被使用但未提供相应值时，`ArgumentAttribute.DefaultValue` 会被自动转换并用于填充参数，而不考虑 `bool showAll = false` 的默认参数值。

## 安装

- #### 🚧 通过 Nuget

  Nuget 包正在开发中

- **通过源代码**

  1. 克隆或下载源代码的压缩包。

  2. 按照以下方式引用 `Antelcat.Parameterization` 和 `Antelcat.Parameterization.SourceGenerators`。

     ```xml
     <ItemGroup>
         <ProjectReference Include="..\Antelcat.Parameterization\Antelcat.Parameterization.csproj"/>
         <ProjectReference Include="..\Antelcat.Parameterization.SourceGenerators\Antelcat.Parameterization.SourceGenerators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>
     </ItemGroup>
     ```

     **注意** `OutputItemType="Analyzer" ReferenceOutputAssembly="false"` 是必须的，因为它是一个源代码生成器。

   3. 尽情享用。

## 贡献

欢迎对本项目做出贡献！无论是报告错误、建议增强功能，还是添加新特性，我们都非常重视您的反馈。

## 待办事项

- [ ] Nuget 包。
- [ ] 自动生成帮助文档。
- [ ] 检查多余参数。
- [ ] 参数组合，例如 `-it` 将同时开启 `-i` 和 `-t`。