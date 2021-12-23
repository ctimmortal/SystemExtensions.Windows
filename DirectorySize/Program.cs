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

            try
            {
                Queue<DirectoryInfo> q = new(directory.GetDirectories());
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

        static long DirectorySizeAccumulator(long total, DirectoryInfo directory)
        {
            total += GetDirectorySize(directory);
            return total;
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

            directory.GetDirectories().Aggregate

            try
            {
                Queue<DirectoryInfo> q = new(directory.GetDirectories());
                while (q.TryDequeue(out DirectoryInfo? d))
                {
                    size += GetDirectorySize(d);
                }
            }
            catch { }

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
        public ByteUnits Unit { get; set; } = ByteUnits.B;

        public double Size => Convert(GetLargestUnit());

        public int Divisor => usebinary ? 1024 : 1000;

        public ByteSize(long size, bool usebinary = true)
        {
            this.size = size;
            this.usebinary = usebinary;
        }

        public ByteSize(long size, ByteUnits unit, bool usebinary = true) : this(size, usebinary) => Unit = unit;

        public ByteSize(double size, ByteUnits unit, bool usebinary = true) :
            this((long)Math.Round(Convert(size, unit, ByteUnits.B, usebinary)), unit, usebinary)
        { }

        public static ByteUnits GetLargestUnit(long size, bool usebinary = true) =>
            (ByteUnits)Math.Truncate(Math.Log(size, usebinary ? 1024 : 1000));

        public ByteUnits GetLargestUnit() => GetLargestUnit(size);

        public static double Convert(double size, ByteUnits from_unit, ByteUnits to_unit, bool usebinary = true) =>
            size / Math.Pow(usebinary ? 1024 : 1000, (int)to_unit - (int)from_unit);

        public static double Convert(long size, ByteUnits to_unit, bool usebinary = true) =>
            Convert(size, ByteUnits.B, to_unit, usebinary);

        public double Convert(ByteUnits to_unit) => Convert(size, to_unit, usebinary);

        long size;
        bool usebinary;

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
#nullable disable
}
