#region Assembly ICSharpCode.SharpZipLib.dll, v2.0.50727
// D:\Google Drive\Team\C#\SQLScripter\SQLScripter_2.0.0.2\JobScripter\bin\Release\ICSharpCode.SharpZipLib.dll
#endregion

using System;
using SQLSchemaScripter;

namespace ICSharpCode.SharpZipLib.Zip
{
    public class ZipEntry : ICloneable
    {
        public ZipEntry(string name);
        [Obsolete("Use Clone instead")]
        public ZipEntry(ZipEntry entry);

        public bool CanDecompress { get; }
        public bool CentralHeaderRequiresZip64 { get; }
        public string Comment { get; set; }
        public long CompressedSize { get; set; }
        public CompressionMethod CompressionMethod { get; set; }
        public long Crc { get; set; }
        public DateTime DateTime { get; set; }
        public long DosTime { get; set; }
        public int ExternalFileAttributes { get; set; }
        public byte[] ExtraData { get; set; }
        public int Flags { get; set; }
        public bool HasCrc { get; }
        public int HostSystem { get; set; }
        public bool IsCrypted { get; set; }
        public bool IsDirectory { get; }
        public bool IsDOSEntry { get; }
        public bool IsFile { get; }
        public bool IsUnicodeText { get; set; }
        public bool LocalHeaderRequiresZip64 { get; }
        public string Name { get; }
        public long Offset { get; set; }
        public long Size { get; set; }
        public int Version { get; }
        public int VersionMadeBy { get; }
        public long ZipFileIndex { get; set; }

        public static string CleanName(string name);
        public object Clone();
        public void ForceZip64();
        public bool IsCompressionMethodSupported();
        public static bool IsCompressionMethodSupported(CompressionMethod method);
        public bool IsZip64Forced();
        public override string ToString();
    }
}
