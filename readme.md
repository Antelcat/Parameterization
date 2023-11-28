# Antelcat.Parameterization: Effortless Command-Line Application Builder

### English | [中文](readme_cn.md)

Welcome to Antelcat.Parameterization, a powerful source generator designed to revolutionize the way you create command-line applications. This tool simplifies the process of building CLI applications by automatically generating parsing methods with just attribute marking on classes and methods.

## Features

- **Attribute-Driven Development**: Easily define commands and arguments using attributes.
- **Automatic Parsing**: Automatically generates methods for parsing command-line arguments.
- **Custom Type Converter**: Works well with `System.ComponentModel.StringConverter`.

## Demo

The [Demo Program](https://github.com/Antelcat/Antelcat.Parameterization/blob/master/src/Antelcat.Parameterization.Demo/Program.cs) implements a simple [Docker](https://www.docker.com/) application to show its features.

1. **Pull an Image**:

   Command:

   ```bash
   > pull ubuntu
   ```

   Output:

   ```bash
   Pulling image ubuntu:latest...
   Successfully Pulled image ubuntu:latest
   ```

   In this example, a custom `Image` class is seamlessly converted from a `string`, thanks to the `[TypeConverter(typeof(ImageConverter))]` attribute applied to `Image`. Additionally, for locally use, the `[Argument(Converter = typeof(ImageConverter))] Image image` annotation allows the same seamless conversion.

2. **Run a Container**:

   Command:

   ```bash
   > run ubuntu
   ```

   Output:

   ```bash
   ubuntu:latest ubuntu running
   ```

   This demonstrates how the default value for the `name` parameter is automatically used when it's not provided in the input.

3. **Display Container Statistics**:

   Command:

   ```bash
   > ps
   ```

   Output:

   ```bash
   CONTAINER_ID    IMAGE           NAME    STATUS
   00a57dbe        ubuntu:latest   ubuntu  running
   ```

4. **Stop a Container and Run a New One**:

   Commands:

   ```bash
   > stop
   > stop 00a57dbe
   > run --name "my container" --image kali
   ```

   Output:

   ```bash
   Argument "id" is not specified.
   Stopping container 00a57dbe...
   Pulling image kali:latest...
   Successfully Pulled image kali:latest
   kali:latest my container running
   ```

   This example highlights the use of named switches, allowing argument reordering. Additionally, it showcases how strings enclosed in `""` are correctly parsed.

5. **Display Updated Container Statistics**:

   Command:

   ```bash
   > ps --all
   ```

   Output:

   ```bash
   CONTAINER_ID    IMAGE           NAME          STATUS
   00a57dbe        ubuntu:latest   ubuntu        stopped
   0419fcea        kali:latest     my container  running
   ```

   First, `Argument(FullName = "all")` replaces the full name of the original `showAll`. `Argument(ShortName = "a")` means `ps -a` also works as same.

   Second, the effect of `ArgumentAttribute.DefaultValue` is evident. When a switch is used without an accompanying value, the `ArgumentAttribute.DefaultValue` is automatically converted and used to fill the argument, regardless of the default parameter value of `bool showAll = false`.

## Installation

- #### 🚧 Via Nuget

  Nuget package is on the way

- **Via source code**

  1. Clone or download zip of the source code.
  2. Reference `Antelcat.Parameterization` and `Antelcat.Parameterization.SourceGenerators` as follows.
  
     ```xml
     <ItemGroup>
         <ProjectReference Include="..\Antelcat.Parameterization\Antelcat.Parameterization.csproj"/>
         <ProjectReference Include="..\Antelcat.Parameterization.SourceGenerators\Antelcat.Parameterization.SourceGenerators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>
     </ItemGroup>
     ```
  
     **NOTICE** that `OutputItemType="Analyzer" ReferenceOutputAssembly="false"` is necessary because it is a Source Generator.
  
  3. Enjoy.

## Contributing

We welcome contributions to this project! Whether it's reporting bugs, suggesting enhancements, or adding new features, your input is valuable to us.

## TODO

- [ ] Nuget package.
- [ ] Automatically generate help documents.

- [ ] Check for excess parameters.
- [ ] Parameter combination, e.g. `-it` will both open `-i` and `-t`.