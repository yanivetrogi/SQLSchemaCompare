#region Assembly ICSharpCode.SharpZipLib.dll, v2.0.50727
// D:\Google Drive\Team\C#\SQLScripter\SQLScripter_2.0.0.2\JobScripter\bin\Release\ICSharpCode.SharpZipLib.dll
#endregion

using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.IO;
using SQLSchemaScripter;

namespace ICSharpCode.SharpZipLib.Zip
{
    public class ZipOutputStream : DeflaterOutputStream
    {
        public ZipOutputStream(Stream baseOutputStream);

        public bool IsFinished { get; }
        public UseZip64 UseZip64 { get; set; }

        public void CloseEntry();
        public override void Finish();
        public int GetLevel();
        public void PutNextEntry(ZipEntry entry);
        public void SetComment(string comment);
        public void SetLevel(int level);
        public override void Write(byte[] buffer, int offset, int count);
    }
}
