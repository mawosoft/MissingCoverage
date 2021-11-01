// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.XPath;

namespace XmlBenchmarks
{
    internal static class TestFiles
    {
        public enum FileSelector
        {
            All = -2,
            AllKnown = -1,
            Small = 0,
            Big = 1,
            Merged = 2,
            Large = 3,
        }

        private const string EnvVarPrefix = nameof(XmlBenchmarks) + "_" + nameof(TestFiles) + "_";
        private const string EnvVarDirectory = EnvVarPrefix + "Directory";
        private const string EnvVarTestFileSelector = EnvVarPrefix + "TestFileSelector";
        private const string EnvVarMockFileSizeMB = EnvVarPrefix + "MockFileSizeMB";
        private const int MaxMockFileSizeMB = 300;
        private static readonly string[] s_knownFileNames = { "coverlet small.xml", "coverlet big.xml", "fcc merged.xml", "mock large.xml" };
        private static readonly string?[] s_knownFilePaths = new string[s_knownFileNames.Length];
        private static readonly List<string> s_otherFilePaths = new();
        private static readonly bool s_defaultSetupDone;

        public static string DirectoryPath { get; private set; } = "";
        public static FileSelector TestFileSelector { get; private set; } = FileSelector.AllKnown;
        public static int MockFileSizeMB { get; private set; } = 10;

        public static List<string> GetFilePaths()
        {
            if (TestFileSelector is FileSelector.Large or FileSelector.All or FileSelector.AllKnown
                && MockFileSizeMB > 0  && s_knownFilePaths[(int)FileSelector.Large] == null)
            {
                CreateMockFile();
            }
            List<string> result = new();
            switch (TestFileSelector)
            {
                case FileSelector.All:
                    result.AddRange(Array.FindAll(s_knownFilePaths, item => item != null)!);
                    result.AddRange(s_otherFilePaths);
                    break;
                case FileSelector.AllKnown:
                    result.AddRange(Array.FindAll(s_knownFilePaths, item => item != null)!);
                    break;
                default:
                    Debug.Assert((int)TestFileSelector >= 0 && (int)TestFileSelector < s_knownFilePaths.Length);
                    if (s_knownFilePaths[(int)TestFileSelector] == null)
                        throw new FileNotFoundException(null, s_knownFileNames[(int)TestFileSelector]);
                    result.Add(s_knownFilePaths[(int)TestFileSelector]!);
                    break;
            }
            return result;
        }

        public static List<FilePathWrapper> GetWrappedFilePaths()
            => GetFilePaths().ConvertAll(static f => new FilePathWrapper(f));

        public static IEnumerable<FileBytesWrapper> GetWrappedFileBytes()
        {
            // For use as ArgumentsSource, we cannot simply return a MemoryStream here because BenchmarkDotNet
            // will reuse the argument. However, XPathDocument and other stream consumers will dispose the stream
            // after reading. We return a byte[] array instead. The MemoryStream constructor only contains a few
            // variable assignments.
            foreach(string filePath in GetFilePaths())
            {
                yield return new FileBytesWrapper(File.ReadAllBytes(filePath), filePath);
            }
        }

        static TestFiles()
        {
            Setup(null, null, null);
            s_defaultSetupDone = true;
        }

        public static void Setup(string? directoryPath, FileSelector? testFileSelector, int? mockFileSizeMB)
        {
            if (s_defaultSetupDone && directoryPath == null && testFileSelector == null && mockFileSizeMB == null)
            {
                return;
            }

            if (testFileSelector == null)
            {
                string? s = Environment.GetEnvironmentVariable(EnvVarTestFileSelector);
                if (Enum.TryParse(s, ignoreCase: true, out FileSelector value))
                {
                    testFileSelector = value;
                }
            }
            if (testFileSelector != null && Enum.IsDefined(testFileSelector.Value))
            {
                TestFileSelector = testFileSelector.Value;
            }
            Environment.SetEnvironmentVariable(EnvVarTestFileSelector, TestFileSelector.ToString());

            if (mockFileSizeMB == null)
            {
                string? s = Environment.GetEnvironmentVariable(EnvVarMockFileSizeMB);
                if (int.TryParse(s, out int value))
                {
                    mockFileSizeMB = value;
                }
            }
            if (mockFileSizeMB == null || mockFileSizeMB < 0 || mockFileSizeMB > MaxMockFileSizeMB)
            {
                mockFileSizeMB = null;
            }
            if (mockFileSizeMB != null)
            {
                MockFileSizeMB = mockFileSizeMB.Value;
            }
            Environment.SetEnvironmentVariable(EnvVarMockFileSizeMB, MockFileSizeMB.ToString());

            if (directoryPath == null)
            {
                directoryPath = Environment.GetEnvironmentVariable(EnvVarDirectory);
            }
            if (directoryPath != null)
            {
                DirectoryPath = directoryPath;
            }
            else
            {
                DirectoryPath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                    ?? Directory.GetCurrentDirectory(),
                    "../../../../../testdata"));
            }
            if (!Directory.Exists(DirectoryPath))
            {
                throw new DirectoryNotFoundException(DirectoryPath);
            }
            Environment.SetEnvironmentVariable(EnvVarDirectory, DirectoryPath);

            foreach (string filePath in Directory.EnumerateFiles(DirectoryPath, "*.xml"))
            {
                string fileName = Path.GetFileName(filePath);
                int i = Array.FindIndex(s_knownFileNames,
                    item => item.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                switch (i)
                {
                    case < 0:
                        s_otherFilePaths.Add(filePath);
                        break;
                    case (int)FileSelector.Large:
                        if (MockFileSizeMB > 0
                            && Math.Round(new FileInfo(filePath).Length / (1024d*1024d)) == MockFileSizeMB)
                        {
                            s_knownFilePaths[i] = filePath;
                        }
                        break;
                    default:
                        s_knownFilePaths[i] = filePath;
                        break;
                }
            }
        }

        public static void Cleanup()
        {
            string? f;
            if ((f = s_knownFilePaths[(int)FileSelector.Large]) != null)
            {
                File.Delete(f);
                s_knownFilePaths[(int)FileSelector.Large] = null;
            }
        }

        public static void CreateMockFile()
        {
            string? sourceFilePath = null;
            foreach (FileSelector fs in new[] { FileSelector.Big, FileSelector.Merged, FileSelector.Small })
            {
                if (s_knownFilePaths[(int)fs] != null)
                {
                    sourceFilePath = s_knownFilePaths[(int)fs];
                    break;
                }
            }
            if (sourceFilePath == null)
                throw new InvalidOperationException("No source for mock file.");
            s_knownFilePaths[(int)FileSelector.Large] = null;
            string targetFilePath = Path.Combine(DirectoryPath, s_knownFileNames[(int)FileSelector.Large]);

            FileStream targetStream = File.Create(targetFilePath);
            XmlWriter writer = XmlWriter.Create(targetStream,
                new XmlWriterSettings() { Indent = true, CloseOutput = true });
            XPathDocument sourceDoc = new(sourceFilePath);
            XPathNavigator navi = sourceDoc.CreateNavigator();
            writer.WriteStartElement("coverage"); // We can omit some attributes for our purposes
            XPathNavigator? coverageSources = navi.SelectSingleNode("/coverage/sources");
            if (coverageSources != null)
            {
                coverageSources.WriteSubtree(writer);
            }
            writer.WriteStartElement("packages");
            List<(XPathNavigator, long)> classes = new();
            XPathNodeIterator nodes = navi.Select("//class");
            foreach (XPathNavigator @class in nodes)
            {
                classes.Add((@class, @class.OuterXml.Length));
            }
            writer.Flush();
            long maxBytesToWrite = MockFileSizeMB * 1024L * 1024L;
            long bytesFlushed = targetStream.Length;
            for (int i = 1; bytesFlushed < maxBytesToWrite; i++)
            {
                writer.WriteStartElement("package");
                writer.WriteAttributeString("name", $"mock{i}");
                writer.WriteStartElement("classes");
                long bytesWritten = bytesFlushed;
                foreach ((XPathNavigator @class, long size) in classes)
                {
                    if (bytesWritten + size > maxBytesToWrite) break;
                    bytesWritten += size;
                    @class.WriteSubtree(writer);
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.Flush();
                bytesFlushed = targetStream.Length;
            }
            writer.Close();
            s_knownFilePaths[(int)FileSelector.Large] = targetFilePath;
        }
    }
}
