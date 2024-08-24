// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using System.IO;
using Mawosoft.Extensions.BenchmarkDotNet;

namespace XmlBenchmarks
{
    public class FileParamWrapper<T> : ParamWrapper<T>
    {
        public FileParamWrapper(T value, string filePath)
            : base(value, Path.GetFileNameWithoutExtension(filePath)) { }
    }

    public class FilePathWrapper : FileParamWrapper<string>
    {
        public FilePathWrapper(string filePath) : base(filePath, filePath) { }
    }

    public class FileBytesWrapper : FileParamWrapper<byte[]>
    {
        public FileBytesWrapper(byte[] value, string filePath) : base(value, filePath) { }
    }
}
