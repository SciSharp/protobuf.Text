# Google Protobuf for TextFormat
Protobuf is not supporting text-format in the Google.Protobuf C# implementation at this moment. This project provides text format support for [protobuf](https://github.com/protocolbuffers/protobuf) in C#. 

[![Join the chat at https://gitter.im/publiclab/publiclab](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/sci-sharp/community) [![NuGet](https://img.shields.io/nuget/dt/Protobuf.Text.svg)](https://www.nuget.org/packages/Protobuf.Text)

```shell
PM> Install-Package Protobuf.Text
```

**How to use:**

```csharp
var config = File.ReadAllText("PATH");
var parsed = TextParser.Default.Parse<T>(config);
```

Related [issue](https://github.com/protocolbuffers/protobuf/issues/6654) on Protobuf repo.

