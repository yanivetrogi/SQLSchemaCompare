using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace SQLSchemaCompare
{
    class License
    {
        public static bool CheckLicense()
        {
            bool bRes = false;
            try
            {
                // create computer UniqueID 
                string MachineId = ComputerIDGnr.GetComputerID();

                // get computer name
                string MachineName = Environment.MachineName;
                string LicenseFile = MachineName + ".lic";
                string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                LicenseFile = Path.Combine(appPath, LicenseFile);


                // If there is a lic file verify it is valid
                if (File.Exists(LicenseFile))
                {
                    StreamReader sr = new StreamReader(LicenseFile);
                    string sFileCont = sr.ReadToEnd();
                    sr.Close();

                    // deycript 
                    ToolBox.BUS data = CryptoString.DecryptActivationKey(sFileCont);
                    string sNewComp = data[CryptoString.COMPUTERID].ToString();

                    if (sNewComp.Trim() == MachineId.Trim())
                    {
                        //Program.IsFreeWare = false;
                        //if (data["Edition"] == "ENT")
                        //{
                        //    Program.IsStandard = false;
                        //}
                        //else
                        //{
                        //    Program.IsStandard = true;
                        //}
                        //bRes = true;
                    }

                    //if (sNewComp.Trim() == MachineId.Trim())
                    //{
                    //    // software is licenced                        
                    //    IsFreeWare = false;
                    //    bRes = true;
                    //}
                    //else
                    //{
                    //    // software is not licenced   
                    //    IsFreeWare = true;
                    //}
                    //return bRes;
                }
                else
                // software is NOT licenced
                {
                    string TekFile = MachineName + ".tek";
                    TekFile = Path.Combine(appPath, TekFile);
                    // If the tek file does not exist create it
                    if (!File.Exists(TekFile))
                    {
                        StreamWriter sw = new StreamWriter(TekFile);
                        sw.Write(MachineId);
                        sw.Close();
                    }
                    // Run as Freeware edition
                    //Program.IsFreeWare = true;
                    bRes = false;
                }
                return bRes;
            }
            catch (Exception e) { Program.WriteToLog("", "", "Error", e.ToString()); return false; }

        }

    }
}
