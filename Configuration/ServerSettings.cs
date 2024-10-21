using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using Microsoft.SqlServer.Dac;

namespace SQLSchemaCompare
{
    public static class ReportServer
    {
        public static string ReportServerInstance { get; set; }
        public static bool ReportServerWindowsAutherntication { get; set; }
        public static string ReportServerUser { get; set; }
        public static string ReportServerPassword { get; set; }
        
    }
    public static class SourceSettings
    {
        public static string SourceServer { get; set; }
        public static string SourceUser { get; set; }
        public static string SourcePassword { get; set; }
        public static bool SourceWindowsAutherntication { get; set; }
        public static string SourceDatabase { get; set; }

        public static bool UseExistingSourceFile { get; set; }
        public static string SourceFileFullPath { get; set; }
        public static string Action { get; set; }
    }

    public static class ScriptFilesOptions
    {
        public static string PathToSaveScriptFiles { get; set; }
        public static bool ZipFiles { get; set; }
        public static string ZipPassword { get; set; }
        public static string DaysToKeepScriptFiles { get; set; }
    }

    public static class PackageOptions
    {
        public static bool ExtractApplicationScopedObjectsOnly { get; set; }
        public static bool ExtractReferencedServerScopedElements { get; set; }
        public static bool IgnoreExtendedProperties { get; set; }
        public static bool IgnorePermissions { get; set; }
        public static bool IgnoreUserLoginMappings { get; set; }
        public static bool ExportData { get; set; }

        public static string TablesToExportData { get; set; }
        public static bool VerifyExtraction { get; set; }
    }

    public static class DeployOptions
    {
        public static bool IgnoreLoginSids { get; set; }
        public static bool IgnorePermissions { get; set; }
        public static bool IgnoreRoleMembership { get; set; }
        public static bool DropObjectsNotInSource { get; set; }
        public static bool DropRoleMembersNotInSource { get; set; }
        public static bool UnmodifiableObjectWarnings { get; set; }
        public static bool VerifyDeployment { get; set; }
        public static bool IncludeTransactionalScripts { get; set; }
        public static bool IgnoreNotForReplication { get; set; }
        public static bool BlockOnPossibleDataLoss { get; set; }

        public static int CommandTimeout { get; set; }
        public static bool DoNotAlterReplicatedObjects { get; set; }        
        public static bool IgnoreKeywordCasing { get; set; }
        public static bool DropIndexesNotInSource { get; set; }
        public static bool IgnoreIdentitySeed { get; set; }
        public static bool IgnoreIncrement { get; set; }


        public static bool CommentOutSetVarDeclarations { get; set; }
        public static bool IgnoreLockHintsOnIndexes { get; set; }
        public static int DatabaseLockTimeout { get; set; }
        public static bool IgnoreIndexOptions { get; set; }
        public static bool IgnoreTableOptions { get; set; }
        public static bool IgnoreWithNocheckOnForeignKeys { get; set; }
        public static bool IgnoreWithNocheckOnCheckConstraints { get; set; }
        public static bool IgnoreUserSettingsObjects { get; set; }
        public static bool IgnoreWhitespace { get; set; }


        public static ObjectType[] ExcludeObjectTypes { get; set; }

    }

    [Serializable]
    public class ServerSettings
    {
        private static List<ServerSettings> _instance;
        private ServerSettings()
        {
        }

        public static void Save()
        {
            XmlSerializer s = new XmlSerializer(typeof(List<ServerSettings>));
            using (TextWriter w = new StreamWriter(@"Servers.config"))
            {
                s.Serialize(w, _instance);
                w.Close();
            }
        }
        
        public static void Load()
        {
            XmlSerializer s = new XmlSerializer(typeof(List<ServerSettings>));
            using (TextReader r = new StreamReader("Servers.config"))
            {
                _instance = (List<ServerSettings>)s.Deserialize(r);
                r.Close();
            }
        }

        public static List<ServerSettings> Instance
        {
            get
            {
                if (_instance == null)
                {
                    Load();
                }
                return _instance;
            }
        }

        public string Databases { get; set; }
        
        public int SqlServerNameLength;
        private string _SqlServer;
        public string SQLServer 
        {             
            get 
            {
                if (Program.SqlServerMaxNameLength < _SqlServer.Length)
                {
                    Program.SqlServerMaxNameLength = _SqlServer.Length;
                }
                return _SqlServer; //.ToUpper(); 
            } 
            set { _SqlServer = value; } 
        }


        public string ServerDisplayName 
        {
            get
            {
                string _server_name = string.Empty;
                _server_name = SQLServer.Replace("]", string.Empty);
                _server_name = SQLServer.Replace("[", string.Empty);
                return _server_name;
            }
        }
        public bool connnectionOK { get; set; }
        public bool AuthenticationMode {get; set;}
        public string SQLUser {get; set; }
        public string SQLPassword {get; set; }
        public bool WriteToConsole { get; set; }
                       
       
    }   
}
