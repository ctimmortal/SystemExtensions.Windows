using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace DirectorySize
{
#nullable enable

    class Program
    {
        static void Main(string[] args)
        {
            string p = @"C:\Users\cstrange";
            DirectoryInfo directory = new(p);

            FileInfo f = new("directory_sizes.txt");
            if (f.Exists) { f.Delete(); }
            Queue<DirectoryInfo> q = new(directory.GetDirectories());

            try
            {
                while (q.TryDequeue(out DirectoryInfo d))
                {
                    ByteSize result = new(GetDirectorySize(d));
                    string message = $"Total size of {d.FullName.Replace(p, "~")} is {result}";
                    Console.WriteLine(message);
                    using (StreamWriter stream = new(f.FullName, true))
                    {
                        stream.WriteLine(message);
                    }
                }
            }
            catch { }
        }

        static bool TryComputeDirectorySize(DirectoryInfo directory, out ByteSize? result)
        {
            try
            {
                result = new(GetDirectorySize(directory));
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        static async Task<long> GetDirectorySizeAsync(DirectoryInfo directory)
        {
            long size = 0;
            try
            {
                size = directory.GetFiles().Sum(f => f.Length);
            }
            catch
            {
                return size;
            }

            Queue<DirectoryInfo> q = new(directory.GetDirectories());
            List<Task<long>> tasks = new();

            try
            {
                while (q.TryDequeue(out DirectoryInfo d))
                {
                    tasks.Add(Task.Run(async () => size += await GetDirectorySizeAsync(d)));
                }
            }
            catch { }
            long[] t = await Task.WhenAll(tasks);

            return size + t.Sum();
        }

        static long GetDirectorySize(DirectoryInfo directory)
        {
            long size = 0;
            try
            {
                size = directory.GetFiles().Sum(f => f.Length);
            }
            catch
            {
                return size;
            }

            Queue<DirectoryInfo> q = new(directory.GetDirectories());
            try
            {
                while (q.TryDequeue(out DirectoryInfo d))
                {
                    size += GetDirectorySize(d);
                }
            }
            catch { }

            //var tasks = await Task.WhenAll(directory.GetDirectories());

            //size += directory.GetDirectories().ToList();

            // Add subdirectory sizes.
            //DirectoryInfo[] dis = directory.GetDirectories();
            //foreach (DirectoryInfo di in dis)
            //{
            //    size += GetDirectorySize(di);
            //}

            return size;
        }

        static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return string.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }
    }

    public enum ByteUnits
    {
        B,
        kB,
        MB,
        GB,
        TB,
        PB,
    }

    public class ByteSize
    {
        public static ByteUnits GetLargestUnit(long size)
        {
            int i = 0;
            while (i < Enum.GetNames<ByteUnits>().Length && size > 1.1)
            {
                i++;
                size /= 1000;
            }
            return (ByteUnits)i;
        }

        public ByteUnits GetLargestUnit() => GetLargestUnit(size);

        public double Size => Convert(GetLargestUnit());

        public ByteUnits Unit { get; set; } = ByteUnits.B;

        public static double Convert(double size, ByteUnits from_unit, ByteUnits to_unit) =>
            Math.Round(size / Math.Pow(1000, (int)to_unit - (int)from_unit), 2);

        public static double Convert(long size, ByteUnits to_unit) => Convert(size, ByteUnits.B, to_unit);

        public double Convert(ByteUnits to_unit) => Convert(size, to_unit);

        public ByteSize(double size, ByteUnits unit)
        {
            Unit = unit;
            this.size = (long)Math.Round(Convert(size, unit, ByteUnits.B));
        }

        public ByteSize(long size, ByteUnits unit) : this(size) => Unit = unit;

        public ByteSize(long size) => this.size = size;

        long size;

        public override string ToString() => $"{Size}{GetLargestUnit()}";
    }

    internal static class ProjectSourcePath
    {
        private const string myRelativePath = "Program.cs";
        private static string? lazyValue;
        public static string Value => lazyValue ??= CalculatePath();

        public static string GetSourceFilePathName([CallerFilePath] string? callerFilePath = null)
            => callerFilePath ?? "";

        private static string CalculatePath()
        {
            string pathName = GetSourceFilePathName();
            return pathName.EndsWith(myRelativePath, StringComparison.Ordinal)
                ? pathName.Substring(0, pathName.Length - myRelativePath.Length)
                : throw new InvalidOperationException("Could not calculate the project path.");
        }
    }
}
