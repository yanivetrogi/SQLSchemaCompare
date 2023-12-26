using System;
using System.Collections.Generic;
using System.Text;
using System.Management;

namespace SQLSchemaCompare
{
    internal static class SystemInfo
    {
        #region -> Private Variables

        public static bool UseProcessorID;
        public static bool UseBaseBoardProduct;
        public static bool UseBaseBoardManufacturer;
        public static bool UseDiskDriveSignature;
        public static bool UseVideoControllerCaption;
        public static bool UsePhysicalMediaSerialNumber;
        public static bool UseBiosVersion;
        public static bool UseBiosManufacturer;
        public static bool UseWindowsSerialNumber;
        public static StringBuilder sb = new StringBuilder();
        #endregion

        public static string GetSystemInfo(string SoftwareName)
        {
            if(UseProcessorID)
                SoftwareName += RunQuery("Processor", "ProcessorId");

            if (UseBaseBoardProduct)
                SoftwareName += RunQuery("BaseBoard", "Product");

            if (UseBaseBoardManufacturer)
                SoftwareName += RunQuery("BaseBoard", "Manufacturer");

            if (UseDiskDriveSignature )
                SoftwareName += RunQuery("DiskDrive", "Signature");

            if (UseVideoControllerCaption)
                SoftwareName += RunQuery("VideoController", "Caption");

            if (UsePhysicalMediaSerialNumber)
                SoftwareName += RunQuery("PhysicalMedia", "SerialNumber");

            if (UseBiosVersion)
                SoftwareName += RunQuery("BIOS", "Version");

            if (UseWindowsSerialNumber)
                SoftwareName += RunQuery("OperatingSystem", "SerialNumber");

            SoftwareName = RemoveUseLess(SoftwareName);
            
            if (SoftwareName.Length < 40)
                return GetSystemInfo(SoftwareName);

            return SoftwareName.ToUpper(); //.Substring(0, 50).ToUpper();
        }

        private static string RemoveUseLess(string st)
        {
            char ch;
            for (int i = st.Length - 1; i >= 0; i--)
            {
                ch = char.ToUpper(st[i]);
                
                if ((ch < 'A' || ch > 'Z') &&
                    (ch < '0' || ch > '9'))
                {
                    st = st.Remove(i, 1);
                }
            }
            return st;
        }

        private static string RunQuery(string TableName, string MethodName)
        {
            ManagementObjectSearcher MOS = new ManagementObjectSearcher("Select * from Win32_" + TableName);
            foreach (ManagementObject MO in MOS.Get())
            {
                try
                {
                    string mo = MO[MethodName].ToString();
                    sb.Append(mo + ";");
                    return mo;
                }
                catch(Exception )
                {
                    //throw(e.Message);
                }
            }
            return "";
        }
    }
}
