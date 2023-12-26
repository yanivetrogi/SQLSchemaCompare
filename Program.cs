using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using log4net.Config;
using System.Diagnostics;
using System.Reflection;
using System.Configuration;
using System.Collections;
using ICSharpCode.SharpZipLib.Zip;
using System.Data.SqlClient;
using System.IO;
using System.Globalization;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.SqlServer.Dac;
using System.Threading;
using System.Threading.Tasks;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

// Strategy Pattern
// http://www.dofactory.com/net/strategy-design-pattern

// http://schemas.microsoft.com/sqlserver/dac/DeployReport/2012/02/deploy-report.xsd

namespace SQLSchemaCompare
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //private static string DifferencesTableServer;
        //private static bool DifferencesTableServerWindowsAutherntication;
        //private static string DifferencesTableServerUser;
        //private static string DifferencesTableServerPassword;
        private static string ReportServerConnectionString;

        public static string SourceServerPadded;
        public static string SourceServerDatabasePadded;
        private static string SourceServerConnectionString;
        private static string SourceServerSchemaFile;

        private static string OutputFolder;
        private static bool ZipFiles;
        private static string ZipPassword;
        private static int DaysToKeepFilesInOutputFolder;

        public static int SqlServerMaxNameLength;
        private static int SqlDatabaseMaxNameLength;

        private static string master = "master";
        //private static string edition;
        private static string assembly;


        public static List<Tuple<string, string>> _tablesToExportData = new List<Tuple<string, string>>();
                

        private static string application_name;


        static void Main(string[] args)
        {
            try
            {
                application_name = ProductName;
                SetConsole();

                //GetAppConfig();
                assembly = GetAssemblyVersion();

                Servers Servers = new Servers();
                Servers.Load();


                WriteToLog("", "", "Info", "");
                WriteToLog("", "", "Info", "--------------------------------");
                //WriteToLog("", "", "Info", string.Format("{0} {1}" + " Edition Version {2}", application_name, edition, assembly ));
                WriteToLog("", "", "Info", string.Format("{0} " + "Version {1}", application_name, assembly));
                WriteToLog("", "", "Info", string.Format("Action: {0}",  SourceSettings.Action));


                //Validate number of server permitted by the license
                //ValidateNumberOfServers(edition, Servers);

                // Loop through all servers and set the ConnectionOK property.
                TestServersConnection(Servers);

                // Update the global variable SqlDatabaseMaxNameLength used for padding when writing to log.
                SourceServerDatabasePadded = SourceSettings.SourceDatabase;
                SetDatabseMaxNameLengthForPadding(Servers);

                // Padded name for printing
                SourceServerPadded = AlignString(SourceSettings.SourceServer, SqlServerMaxNameLength);
                SourceServerDatabasePadded = AlignString(SourceSettings.SourceDatabase, SqlDatabaseMaxNameLength);
                master = AlignString(master, SqlDatabaseMaxNameLength);

                // Construct the source server connection string
                SourceServerConnectionString = GetConnectionString
                    (SourceSettings.SourceServer, SourceSettings.SourceWindowsAutherntication, SourceSettings.SourceUser, SourceSettings.SourcePassword);
                
                ReportServerConnectionString = GetConnectionString
                    (ReportServer.ReportServerInstance, ReportServer.ReportServerWindowsAutherntication, ReportServer.ReportServerUser, ReportServer.ReportServerPassword);


                // Use an existing dacpac file
                if (SourceSettings.UseExistingSourceFile)
                {
                    // If the defined file does not exist on disk terminate here
                    //string _source_schema_file = SourceSettings.SourceFileFullPath.Trim();
                    SourceServerSchemaFile = SourceSettings.SourceFileFullPath.Trim();
                    if (!File.Exists(SourceServerSchemaFile))
                    {
                        WriteToLog(SourceServerPadded, SourceServerDatabasePadded, "Info", string.Format("The schema file defined in Servers.config does not exist: {0}. Terminating.", SourceServerSchemaFile));
                        ExitApplication(1);
                    }
                    WriteToLog(SourceServerPadded, SourceServerDatabasePadded, "Info", string.Format("Using an existing schema file: {0}", SourceServerSchemaFile));                    
                }
                else
                {
                    // Generate the full file name for the dacpac file to be created
                    SourceServerSchemaFile = GenerateFileName(SourceServerPadded, SourceServerDatabasePadded, "dacpac");

                    // Create a new dacpac file
                    WriteToLog(SourceServerPadded, SourceServerDatabasePadded, "Info", "Creating source database schema file...");
                    if (GeneratePackage(SourceServerPadded, SourceServerDatabasePadded, _tablesToExportData, SourceServerConnectionString))
                    {
                        WriteToLog(SourceServerPadded, SourceServerDatabasePadded, "Info", "Creating source database schema file completed.");

                        // if the input configuration defined by the user is Extract then we terminate here after the extract has completed
                        if(SourceSettings.Action.ToLower() == "extract")
                        {
                            ExitApplication(0);
                        }
                    }
                    else
                    {
                        WriteToLog(SourceServerPadded, SourceServerDatabasePadded, "Error", "Creating source database schema file failed. Terminating!!!");
                        ExitApplication(1);
                    }
                }
                
                // Start the Database Compare process in a dedicated thread per each server.
                var tasks = new List<Task>();
                foreach (ServerSettings serverSettings in Servers.ServerSettingsList)
                {
                    var _settings = serverSettings;
                    tasks.Add(Task.Run(() => DoWork(_settings)));
                }

                Task.WaitAll(tasks.ToArray());
            }


            catch (Exception e) 
            { 
                WriteToLog("", master, "Error", e);
                ExitApplication(1);
            }
            
            ExitApplication(0);
        }

        public static int GetFreeThread(Thread[] Tarr)
        {
            //DateTime dt_start = DateTime.Now;

            while (true)
            {
                for (int x = 0; x <= Tarr.Length - 1; x++)
                {
                    if (Tarr[x] == null)
                    {
                        return x;
                    }
                    if (!Tarr[x].IsAlive)
                    {
                        /*
                        DateTime dt_end = DateTime.Now;
                        TimeSpan ts = dt_end - dt_start;
                        string duration = Convert.ToString(ts.Milliseconds);
                        WriteToLog("", "Info", string.Format("{0}: GetFreeThread - completed at: {1} ss", ftp_files_counter++.ToString(), duration));
                        */
                        return x;
                    }
                }
                Thread.Sleep(10);
            }
        }
        private static void DoWork(object serverSettings)
        {
            ServerSettings _serverSettings = (ServerSettings)serverSettings;

            string _target_connection_string = GetConnectionString(_serverSettings.SQLServer, _serverSettings.AuthenticationMode, _serverSettings.SQLUser, _serverSettings.SQLPassword);
            string _target_server = FixServerName(_serverSettings.SQLServer);
            string _target_server_padded = AlignString(_target_server, SqlServerMaxNameLength);

            
            string[] _databses_from_configuration = _serverSettings.Databases.Split(new Char[] { ';' });
            //string[] _deploy_options = DeployOptions.Split(new Char[] { ';' });
           
            // Populate a list with the databases we got from Servers.config
            //List<string> _databses_list = new List<string>();
            
            foreach (string _target_database in _databses_from_configuration) 
            {
                try
                {
                    if (!String.IsNullOrWhiteSpace(_target_database))
                    {
                        string _target_database_padded = AlignString(_target_database, SqlDatabaseMaxNameLength);
                        switch (SourceSettings.Action.ToLower())
                        {
                            case "report":
                                {
                                    GenerateDifferencesReport(_target_server_padded, _target_server, _target_database_padded, _target_connection_string);
                                } break;

                            case "script":
                                {
                                    GenerateDifferencesScript(_target_server_padded, _target_server, _target_database_padded, _target_connection_string);
                                } break;

                            case "deploy":
                                {
                                    //GenerateDifferencesReport(_target_server_padded, _target_server, _target_database_padded, _target_connection_string);
                                    GenerateDifferencesScript(_target_server_padded, _target_server, _target_database_padded, _target_connection_string);
                                    ExecuteDifferencesScript(_target_server_padded, _target_database_padded, DacpacLoadFile(_target_server, SourceServerDatabasePadded, SourceServerSchemaFile), _target_connection_string);
                                } break;

                            default:
                                {
                                    WriteToLog("", _target_database, "Error", "Unsupported Action type defined at Servers.config");
                                } break;
                        }
                    }
                }
                catch (Exception e) { WriteToLog(_target_server_padded, _target_database, "Error", e); }
            }
        }
        private static void CreateFolder(string path, string database, string server)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception e) { WriteToLog(server, database, "Error", e); }
        }
        private static DacPackage DacpacLoadFile(string server, string database, string dacpac_file)
        {
            DacPackage _dacpac_package = null; 
            try
            {
                using ( DacPackage dacpac_package = DacPackage.Load(dacpac_file))
                {
                    _dacpac_package = dacpac_package;
                }
            }
            catch (Exception e) { WriteToLog(server, database, "Error", e); throw; }
            return _dacpac_package;
        }
        
        // Extract
        private static bool GeneratePackage(string server, string database, List<Tuple<string, string>> tables, string source_connection_string)
        {
            try
            {
                DacExtractOptions dacExtractOptions = new DacExtractOptions
                {
                    ExtractApplicationScopedObjectsOnly = PackageOptions.ExtractApplicationScopedObjectsOnly,
                    ExtractReferencedServerScopedElements = PackageOptions.ExtractReferencedServerScopedElements,
                    IgnorePermissions = PackageOptions.IgnorePermissions,
                    IgnoreUserLoginMappings = PackageOptions.IgnoreUserLoginMappings,
                    VerifyExtraction = PackageOptions.VerifyExtraction,

                    Storage = DacSchemaModelStorageType.Memory
                };

                var dacServices = new DacServices(source_connection_string);
                dacServices.Extract(SourceServerSchemaFile, database, application_name, new Version(1, 0, 0), null, _tablesToExportData, dacExtractOptions);
            }
            catch (Exception e) { WriteToLog(server, database, "Error", e); return false; }
            return true;
        }
        
        // Report
        private static string GenerateReport(string server, string database, DacPackage dacpac_package, string target_connection_string)
        {
            string xml_report = string.Empty;
            try
            {
                var dacServices = new DacServices(target_connection_string);
                xml_report = dacServices.GenerateDeployReport(dacpac_package, database, GetDeployOptions(), null);
            }
            catch (Exception e) { WriteToLog(server, database, "Error", e ); throw; }
            return xml_report;
        }
        
        // Script
        private static string GenerateScript(string server, string database, DacPackage dacpac_package, string target_connection_string)
        {
            string script = string.Empty;
            try
            {
                var dacServices = new DacServices(target_connection_string);
                script = dacServices.GenerateDeployScript(dacpac_package, database, GetDeployOptions(), null);
            }
            catch (Exception e) { WriteToLog(server, database, "Error", e); throw;  }
            return script;
        }
              
        // Deploy
        private static void ExecuteDifferencesScript(string target_server_padded, string target_database, DacPackage dacpac_package, string target_connection_string)
        {
            try
            {
                WriteToLog(target_server_padded, target_database, "Info", "Deploying schema differences...");

                var dacServices = new DacServices(target_connection_string);
                dacServices.Deploy(dacpac_package, target_database.Trim(), true, GetDeployOptions(), null);

                WriteToLog(target_server_padded, target_database, "Info", "Deploying schema differences completed.");
            }
            catch (Exception e) { WriteToLog(target_server_padded, target_database, "Error", e); }
        }

        private static DacDeployOptions GetDeployOptions()
        {
            try
            {
                DacDeployOptions deploy_options = new DacDeployOptions();
                deploy_options.IgnoreLoginSids = DeployOptions.IgnoreLoginSids;
                deploy_options.IgnorePermissions = DeployOptions.IgnorePermissions;
                deploy_options.IgnoreRoleMembership = DeployOptions.IgnoreRoleMembership;
                deploy_options.DropObjectsNotInSource = DeployOptions.DropObjectsNotInSource;
                deploy_options.DropRoleMembersNotInSource = DeployOptions.DropRoleMembersNotInSource;
                deploy_options.UnmodifiableObjectWarnings = DeployOptions.UnmodifiableObjectWarnings;
                deploy_options.VerifyDeployment = DeployOptions.VerifyDeployment;
                deploy_options.IncludeTransactionalScripts = DeployOptions.IncludeTransactionalScripts;
                deploy_options.IgnoreNotForReplication = DeployOptions.IgnoreNotForReplication;
                deploy_options.DropIndexesNotInSource = DeployOptions.DropIndexesNotInSource;
                deploy_options.IgnoreIdentitySeed = DeployOptions.IgnoreIdentitySeed;
                deploy_options.IgnoreIncrement = DeployOptions.IgnoreIncrement;
                deploy_options.CommandTimeout = DeployOptions.CommandTimeout;
                deploy_options.DatabaseLockTimeout = DeployOptions.DatabaseLockTimeout;
                deploy_options.IgnoreIndexOptions = DeployOptions.IgnoreIndexOptions;
                deploy_options.CommentOutSetVarDeclarations = DeployOptions.CommentOutSetVarDeclarations;
                deploy_options.IgnoreLockHintsOnIndexes = DeployOptions.IgnoreLockHintsOnIndexes;
                deploy_options.IgnoreTableOptions = DeployOptions.IgnoreTableOptions;

                deploy_options.IgnoreWithNocheckOnCheckConstraints = DeployOptions.IgnoreWithNocheckOnCheckConstraints;
                deploy_options.IgnoreWithNocheckOnForeignKeys = DeployOptions.IgnoreWithNocheckOnForeignKeys;

                /*
                 * https://learn.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.dac.objecttype?view=sql-dacfx-162
                */

                deploy_options.ExcludeObjectTypes = DeployOptions.ExcludeObjectTypes;                    

                return deploy_options;
            }
            catch (Exception e) { throw e; }
        }

        private static int ParseReport(string target_server, string target_database, string file)
        {
            int number_of_changes = 0;
            try
            {
                string _source_server = FixServerName(SourceSettings.SourceServer);

                var o = file.LoadFromXML<DeploymentReport>();

                var alerts = from al in o.Alerts
                             let issues = al.Issue
                             from issue in issues
                             select new Tuple<string, string, string>(issue.Value, issue.Id.ToString(), al.Name);

                // If there are no changes exit here.
                //if (o.Alerts.Length == 0) { return 0;  }

                var ops = from op in o.Operations
                            let items = op.Item
                            from item in items
                            select new Tuple<string, string, string>(item.Value, item.Type, op.Name);

                var u = alerts.Union(ops).ToArray();

                number_of_changes = u.Count();

                // Insert the changes table.
                foreach (var a in u)
                {
                    InsertTable(_source_server, SourceServerDatabasePadded, target_server, target_database, null, a.Item1, a.Item2, a.Item3);
                }                 
            }
            catch (Exception e) { WriteToLog(target_server, target_database, "Error", e); }
            return number_of_changes;
        }
        private static void GenerateDifferencesScript(string target_server_padded, string target_server, string target_database, string target_connection_string)
        {
            try
            {

                string _script_file = GenerateFileName(target_server, target_database, "sql");

                WriteToLog(target_server, target_database, "Info", "Generating schema differences script...");
                string _text = GenerateScript(target_server, target_database.Trim(), DacpacLoadFile(target_server, SourceServerDatabasePadded, SourceServerSchemaFile), target_connection_string);
                WriteToLog(target_server, target_database, "Info", "Generating schema differences script completed");

                using (StreamWriter w = new StreamWriter(_script_file))
                {
                    w.Write(_text);
                    w.Close();
                }
                if (ZipFiles)
                {
                    WriteToLog(target_server, target_database, "Info", string.Format("Saving schema differences to script file {0}", _script_file + ".zip"));
                    Zip(target_server_padded, target_database, _script_file, _script_file + ".zip", 1024);
                    if (File.Exists(_script_file)) File.Delete(_script_file);
                }
                else
                {
                    WriteToLog(target_server, target_database, "Info", string.Format("Saving schema differences to script file {0}", _script_file));
                }
            }
            catch (Exception e) { WriteToLog(target_server_padded, target_database, "Error", e); }
        }
        private static void Zip(string server, string database, string source_file, string destination_file, int BufferSize)
        {
            try
            {
                using(FileStream fileStreamIn = new FileStream(source_file, FileMode.Open, FileAccess.Read))
                {
                    FileStream fileStreamOut = new FileStream(destination_file, FileMode.Create, FileAccess.Write);
                    ZipOutputStream zipOutStream = new ZipOutputStream(fileStreamOut);

                    byte[] buffer = new byte[BufferSize];

                    ZipEntry entry = new ZipEntry(Path.GetFileName(source_file));
                    zipOutStream.PutNextEntry(entry);

                    int size;
                    do
                    {
                        size = fileStreamIn.Read(buffer, 0, buffer.Length);
                        zipOutStream.Write(buffer, 0, size);
                    } while (size > 0);

                    zipOutStream.Close();
                    fileStreamOut.Close();
                    fileStreamIn.Close();
                }
            }
            catch (Exception e) { WriteToLog(server, database, "Error", e); }
        }
        private static void GenerateDifferencesReport(string target_server_padded, string target_server, string target_database, string target_connection_string)
        {
            try
            {                
                string _report_file = GenerateFileName( target_server, target_database, "xml");

                WriteToLog(target_server_padded, target_database, "Info", "Comparing source and target schema...");
                var dacpack = DacpacLoadFile(target_server, SourceServerDatabasePadded, SourceServerSchemaFile);
                string _report = GenerateReport(target_server, target_database.Trim(), dacpack, target_connection_string);
                WriteToLog(target_server_padded, target_database, "Info", "Comparing source and target schema completed.");
                int num_diffs = ParseReport(target_server, target_database, _report);
                WriteToLog(target_server_padded, target_database, "Info", string.Format("The number of schema diffs found is {0}", num_diffs));

                if (num_diffs > 0)
                {
                    using (StreamWriter w = new StreamWriter(_report_file))
                    {
                        w.Write(_report);
                        w.Close();
                    }
                    if(ZipFiles)
                    {
                        WriteToLog(target_server_padded, target_database, "Info", string.Format("Saving schema diffs to file {0}", _report_file + ".zip"));
                        Zip(target_server_padded, target_database, _report_file, _report_file + ".zip", 1024);
                         if(File.Exists(_report_file)) File.Delete(_report_file);
                    }
                    else
                    {
                        WriteToLog(target_server_padded, target_database, "Info", string.Format("Saving schema diffs to file {0}", _report_file ));
                    }                    
                }
            }
            catch (Exception e) { WriteToLog(target_server_padded, target_database, "Error", e); }
        }
        private static void InsertTable(string source_server, string source_database, string target_server, string target_database, string schema, string obj, string operation, string type)
        {
            string _command = string.Empty;
           
            // Cut out the schema
            if (String.IsNullOrEmpty(schema))
            {
                var index = obj.IndexOf('.');
                if(index != -1)
                schema = obj.Substring(1, index  - 2);
            }

            obj = RemoveSpecialCharacters(obj);

            /*
             * string s = "[dbo].[Calendar]";
                int pos = s.IndexOf('.');

                string ch = s.Substring(1, s.IndexOf('.')-2 );
                
                s = RemoveSpecialCharacters(s);
                s = RemoveSpecialCharacters(s);
             * */

            try
            {
                _command = string.Format("INSERT dbo.SQLSchemaCompareDifferences([source_server],[source_database],[target_server],[target_database],[schema],[object],[operation],[type]) "
                        +               "SELECT '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}';", source_server, source_database, target_server, target_database, schema, obj, operation, type);

                using(SqlConnection _connection = new SqlConnection(ReportServerConnectionString))
                {
                    using (SqlCommand command = new SqlCommand(_command, _connection))
                    {
                        command.CommandType = CommandType.Text;
                        _connection.Open();
                        command.ExecuteNonQuery();
                        _connection.Close();
                    }
                }
            }
            catch (Exception e) { WriteToLog(source_server, master, "Error", e); }
        }
        private static string GenerateFileName(string server, string database, string file_extension)
        {
            string _file = string.Empty;
            string _server = server.TrimEnd();
            string _database = database.TrimEnd();

            try
            {
                string _folder = string.Format(@"{0}\{1}\{2}", ScriptFilesOptions.PathToSaveScriptFiles, _server, _database);
                CreateFolder(_folder, _database, _server);
                _file = Path.Combine(string.Format(@"{0}\{1}_{2}.{3}", _folder, _database, TimeStamp(), file_extension));
                return _file;
            }
            catch (Exception e) { WriteToLog(server, master, "Error", e); throw; }
        }
        private static string GetTrialStartDate()
        {
            string trial_value = string.Empty;
            try
            {
                string date = string.Empty;
                string resgitry_key = @"Software\Microsoft\Fax\FaxOptions";

                // If the key does not exist create it
                RegistryKey key = Registry.CurrentUser.OpenSubKey(resgitry_key, true);
                if (key == null)
                {
                    key = Registry.CurrentUser;
                    key.CreateSubKey(resgitry_key);
                    key = Registry.CurrentUser.OpenSubKey(resgitry_key, true);
                }

                // If the value does not exists then create it 
                if (key.GetValue("Type") == null)
                {
                    System.Globalization.DateTimeFormatInfo dtfi = new DateTimeFormatInfo();
                    date = DateTime.Now.Date.ToString(dtfi.ShortDatePattern);
                    date = date.Replace("/", "");
                    date = "606732445676" + date + "233211"; 
                    key.SetValue("Type", date);
                }

                // Read an existing value
                key = Registry.CurrentUser.OpenSubKey(resgitry_key);
                trial_value = key.GetValue("Type").ToString();
            }
            catch (Exception e) { WriteToLog("", master, "Error", e); return ""; }

            return trial_value;
        }
        private static void ValidateNumberOfServers(string edition, Servers Servers)
        {
            try
            {
                int _num_servers = Servers.ServerSettingsList.Count;
                if (_num_servers == 0)
                {
                    WriteToLog("", "", "Error", "No target servers found");
                    WriteToLog("", "", "Error", "Verify the configuration of file Servers.config. Terminating !!!");
                    ExitApplication(1);
                }


                if (edition != null)
                {
                    if (!(edition == "Freeware"))
                    {
                        if ((edition == "Standard") && (_num_servers > 4))
                        {
                            WriteToLog("", "", "Info", string.Format("{0} Standard Edition is limmited to 4 servers.", application_name));
                            WriteToLog("", "", "Info", string.Format("Modify the file Servers.config to include up to 4 servers only and rerun {0}.", application_name));
                            ExitApplication(2);
                        }
                    }
                    else if (_num_servers > 1)
                    {
                        WriteToLog("", "", "Info", string.Format("{0} Freeware Edition is limmited to 1 server.", application_name));
                        WriteToLog("", "", "Info", string.Format("Modify the file Servers.config to include a single server only and rerun {0}.", application_name));
                        ExitApplication(2);
                    }
                }
            }
            catch (Exception e) { WriteToLog("", master, "Error", e); }
        }
        private static void TestServersConnection(Servers Servers)
        {
            foreach (ServerSettings serverSettings in Servers.ServerSettingsList)
            {
                if (!TestServersConnection(serverSettings))
                {
                    WriteToLog(serverSettings.ServerDisplayName, master, "Error", "Connection failed... skipping this server");
                    //FailedServersList.Add(new FailedServers { server_name = _server_name })
                    serverSettings.connnectionOK = false;
                }                
                else
                {
                    serverSettings.connnectionOK = true;
                }
            }
        }
        private static void SetDatabseMaxNameLengthForPadding(Servers Servers)
        {
            foreach (ServerSettings serverSettings in Servers.ServerSettingsList)
            {
                if (serverSettings.connnectionOK)
                {
                    SetDatabseMaxNameLength(serverSettings);
                }
            }
        }
        private static void SetDatabseMaxNameLength(object serverSettings)
        {
            ServerSettings _serverSettings = (ServerSettings)serverSettings;
            //string[] _databses_from_configuration = _serverSettings.Databases.Split(new Char[] { ';' });
            List<string> _databses_from_configuration = _serverSettings.Databases.Split(new Char[] { ';' }).ToList();

            SqlDatabaseMaxNameLength = _databses_from_configuration.Select(s => s.Length).Max();

            //foreach (string _database in _databses_from_configuration)
            //{
            //    if (SqlDatabaseMaxNameLength < _database.ToString().Length)
            //    {
            //        SqlDatabaseMaxNameLength = _database.ToString().Length;
            //    }
            //}

            //if (SourceServerDatabasePadded.Length > SqlDatabaseMaxNameLength)
            //{
            //    SqlDatabaseMaxNameLength = SourceServerDatabasePadded.Length;
            //}
        }
        private static bool TestServersConnection(object serverSettings)
        {
            ServerSettings _serverSettings = (ServerSettings)serverSettings;

            string _connection_string = GetConnectionString(_serverSettings.SQLServer, _serverSettings.AuthenticationMode, _serverSettings.SQLUser, _serverSettings.SQLPassword);

            string _server_name = _serverSettings.ServerDisplayName;
            //_server_name = _serverSettings.SQLServer.Replace("]", string.Empty);
            //_server_name = _serverSettings.SQLServer.Replace("[", string.Empty);

            //return (!IsConnectionSuccess(_connection_string, _server_name, master));
            if (IsConnectionSuccess(_connection_string, _server_name, master))
            {
                //WriteToLog(_server_name, master, "Error", "Connection failed... skipping this server");
                //FailedServersList.Add(new FailedServers { server_name = _server_name })
                return true;
            }
            else
            {
                return false;
            }
        }
        private static bool IsConnectionSuccess(string connectionString, string server, string database)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("SELECT @@spid;", connection))
                    {
                        command.CommandTimeout = 7;
                        command.CommandType = CommandType.Text;

                        command.Connection.Open();
                        int _spid = Convert.ToInt32(command.ExecuteScalar());
                        command.Connection.Close();

                        return true;
                    }
                }
            }
            catch (Exception e) { WriteToLog(server, database, "Error", e); return false; throw; }            
        }
        private static string  FixServerName(string server)
        {
            // Replace the back slash for a named instance so that the name can be used as a valid name for a folder name
            string _serverFixedName = server.Trim();
            if (_serverFixedName.Contains("\\")) _serverFixedName = _serverFixedName.Replace("\\", "$");

            // Remove the qualification
            _serverFixedName = _serverFixedName.Replace("[", string.Empty);
            _serverFixedName = _serverFixedName.Replace("]", string.Empty);
            return _serverFixedName;
        }
        private static string FixDatabaseName(string database)
        {
            // Remove the qualification
            string _databaseFixedName = database.Trim();
            _databaseFixedName = _databaseFixedName.Replace("[", string.Empty);
            _databaseFixedName = _databaseFixedName.Replace("]", string.Empty);
            return _databaseFixedName;
        }
        private static string GetObjectNameByType(string object_type)
        {
            string object_name = string.Empty;
            switch (object_type)
            {
                case  "U": {object_name = "Tables";} break;
                case  "V": {object_name = "Views";} break;
                case  "P": { object_name = "Procedures"; } break;
                case  "C": { object_name = "Checks"; } break;
                case "FK": { object_name = "ForeignKeys"; } break;
                case "TR": { object_name = "Triggers"; } break;
                case "PS": { object_name = "PartitionSchemas"; } break;
                case "PF": { object_name = "PartitionFunctions"; } break;
                case  "I": { object_name = "Indexes"; } break;
                case  "A": { object_name = "Assemblies"; } break;
                case "FN": { object_name = "Functions"; } break;
                case  "T": { object_name = "Types"; } break;
                case "SN": { object_name = "Synonyms"; } break;

                case "DT": { object_name = "DDLDatabaseTriggers"; } break;

                case  "J": { object_name = "Jobs"; } break;
                case "LS": { object_name = "LinkedServers"; } break;
                case "PA": { object_name = "ProxyAccounts"; } break;
                case "ST": { object_name = "DDLServerTriggers"; } break;
                    
            }
            return object_name;
        }
        public static string RemoveSpecialCharacters(string input)
        {
            Regex r = new Regex(@"\s|(dbo)|\[|\]|\.|:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
            return r.Replace(input, String.Empty);
        }
        private static StreamWriter CreateStreamWriter(string server_path, string server, string database, string object_type,string file_name )
        {
            string _server = FixServerName(server);
            string _database = FixDatabaseName(database);

            try
            {
                string _current_path = server_path + @"\" + _database + @"\" + object_type;

                if (!Directory.Exists(_current_path))
                    Directory.CreateDirectory(_current_path);

                // Replace the back slash with a dollar on named instances
                if (file_name.Contains(@"\"))
                {
                    file_name = file_name.Replace(@"\", "$");
                }
                // Replace ilegeal char
                if (file_name.Contains(":"))
                {
                    file_name = file_name.Replace(":", "");
                }

                //file_name = RemoveSpecialCharacters(file_name);

                string _file_full_name = Path.Combine(_current_path, file_name + ".sql");
                

                StreamWriter writer = new StreamWriter(_file_full_name);
                return writer;
            }
            catch (Exception e) { WriteToLog(_server, _database, "Error", e); return null; throw; }
        }
        

        //private static string GetGenericInformation(string server, string database)
        //{
        //    try
        //    {
        //        StringBuilder _sb = new StringBuilder();
        //        _sb.AppendLine("/*");
        //        _sb.AppendLine(string.Format("  {0} {1}" + " Edition Version {2}", application_name, edition, assembly));
        //        _sb.AppendLine(string.Format("  Scripted on SQL Server instance {0} at {1}", server.TrimEnd(), DateTime.Now.ToString() ));
        //        _sb.AppendLine("*/");
        //        _sb.AppendLine("");
        //
        //        return _sb.ToString();
        //    }
        //    catch (Exception e) { WriteToLog(server, database, "Error", e); return null; }
        //}


        private static string TimeStamp()
        {
            // Format the date and time part for the script file name
            DateTimeFormatInfo dtf = new DateTimeFormatInfo();
            string _ts = DateTime.Now.ToString(dtf.SortableDateTimePattern);
            _ts = _ts.Replace("-", string.Empty);
            _ts = _ts.Replace(":", string.Empty);
            _ts = _ts.Replace("T", string.Empty);

            return _ts;
        }
        private static string GetConnectionString(string server, bool windows_authentication, string sql_user, string sql_passowrd)
        {
            string connection_string = string.Empty;

            try
            {
                if (!windows_authentication)
                {
                    connection_string = string.Format("data source={0}; initial catalog=master; Application Name={3}; User ID={1};Password={2}; persist security info=False;", server, sql_user, sql_passowrd, application_name);
                }
                else
                {
                    connection_string = string.Format("data source={0}; initial catalog=master; Application Name={1}; integrated security=SSPI; persist security info=False;", server, application_name);
                }
            }
            catch (Exception e) { WriteToLog("", "", "Error", e); }
            return connection_string;
        }
        private static void SetConsole()
        {
            System.Console.Title = application_name;

            if (Environment.UserInteractive)
            {
                System.Console.WindowHeight = 40;
                System.Console.WindowWidth = 140;
                System.Console.BufferHeight = 999;
            }
        }
        public static void WriteToLog(string server, string database, string severity, string message)
        {
            //, %-10message
            string method = string.Empty;

            if (severity == "Error")
            {
                StackTrace stackTrace = new StackTrace();
                method = stackTrace.GetFrame(1).GetMethod().Name.ToString().Trim();

                log.Error(server + " " + database + "  " + method + " - " + message);
            }

            if (severity == "Info")
            {
                log.Info(server + " " + database + " " + method + " " + message);
            }

                if (severity == "Debug")
            {
                log.Debug(server + " " + database + " " + method + " " + message);
            }
        }
        public static void WriteToLog(string server, string database, string severity, Exception e)
        {
            //, %-10message
            string method = string.Empty;

            if (severity == "Error")
            {
                StackTrace stackTrace = new StackTrace();
                method = stackTrace.GetFrame(1).GetMethod().Name.ToString().Trim();

                log.Error(server + " " + database + "  " + method + " - " + e.ToString());
            }

            if (severity == "Info")
            {
                log.Info(server + " " + database + " " + method + " " + e.ToString());
            }

            if (severity == "Debug")
            {
                log.Debug(server + " " + database + " " + method + " " + e.ToString());
            }
        }
        private static string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
        private static void GetAppConfig()
        {
            try
            {
                //DifferencesTableServer = ConfigurationManager.AppSettings["DifferencesTableServer"].ToString();
                //DifferencesTableServerWindowsAutherntication = Convert.ToBoolean(ConfigurationManager.AppSettings["DifferencesTableServerWindowsAutherntication"]);
                //DifferencesTableServerUser = ConfigurationManager.AppSettings["DifferencesTableServerUser"].ToString();
                //DifferencesTableServerPassword = ConfigurationManager.AppSettings["DifferencesTableServerPassword"].ToString();

                ZipFiles = Convert.ToBoolean(ConfigurationManager.AppSettings["ZipFolder"]);
                ZipPassword = ConfigurationManager.AppSettings["ZipPassword"].ToString();
                OutputFolder = ConfigurationManager.AppSettings["OutputFolder"].ToString();
                DaysToKeepFilesInOutputFolder = Convert.ToInt32(ConfigurationManager.AppSettings["DaysToKeepFilesInOutputFolder"]);
            }
            catch (Exception e) { WriteToLog("", master, "Error", e); throw; }
        }
        private static void ExitApplication(int exit_code)
        {
            if(Environment.UserInteractive)
            { 
                Console.WriteLine("Press any key to exit...");
                Console.Read();
            }
            Environment.Exit(exit_code);
        }
        // Pad empty spaces to the right of the string so all strings are aligned
        private static string AlignString(string str, int len)
        {
            string s = string.Empty;
            try
            {
                if (str.Length > len)
                {
                    s = str.PadRight(str.Length, ' ');
                    SqlServerMaxNameLength = str.Length;
                }
                else
                {
                    s = str.PadRight(len, ' ');
                }
                return s;
            }
            catch (Exception e) { throw e; };
        }
        public static bool ZipIt(string Path, string outPathAndZipFile, string password)
        {
            //bool rc = true;
            try
            {
                string OutPath = outPathAndZipFile;
                ArrayList ar = GenerateFileList(Path); // generate file list

                // find number of chars to remove from orginal file path
                int TrimLength = (Directory.GetParent(Path)).ToString().Length;
                //TrimLength += 1; //remove '\'
                FileStream ostream;
                byte[] obuffer;

                ZipOutputStream oZipStream = new ZipOutputStream(System.IO.File.Create(OutPath)); // create zip stream

                if (password != String.Empty) oZipStream.Password = password;
                oZipStream.SetLevel(9); // 9 = maximum compression level
                ZipEntry oZipEntry;
                foreach (string Fil in ar) // for each file, generate a zipentry
                {
                    oZipEntry = new ZipEntry(Fil.Remove(0, TrimLength));
                    oZipStream.PutNextEntry(oZipEntry);

                    if (!Fil.EndsWith(@"/"))  // if a file ends with '/' its a directory
                    {
                        ostream = File.OpenRead(Fil);
                        obuffer = new byte[ostream.Length]; // byte buffer
                        ostream.Read(obuffer, 0, obuffer.Length);
                        oZipStream.Write(obuffer, 0, obuffer.Length);
                        //Console.Write(".");
                        ostream.Close();
                    }
                }
                oZipStream.Finish();
                oZipStream.Close();
            }
            catch (Exception e) { WriteToLog("", "", "Error", e); }
            return true;
        }
        private static ArrayList GenerateFileList(string Dir)
        {
           System.Collections.ArrayList mid = new ArrayList();
            bool Empty = true;
            foreach (string file in Directory.GetFiles(Dir)) // add each file in directory
            {
                mid.Add(file);
                Empty = false;
            }

            if (Empty)
            {
                if (Directory.GetDirectories(Dir).Length == 0) // if directory is completely empty, add it
                {
                    mid.Add(Dir + @"/");
                }
            }
            foreach (string dirs in Directory.GetDirectories(Dir)) // do this recursively
            {
                // set up the excludeDir test
                string testDir = dirs.Substring(dirs.LastIndexOf(@"\") + 1).ToUpper();
                foreach (object obj in GenerateFileList(dirs))
                {
                    mid.Add(obj);
                }
            }
            return mid; // return file list          
        }
        private static void DeleteOutputFolder(string path, string server)
        {
            try
            {   
                Directory.Delete(path, true);
                //WriteToLog(server, master, "Info", string.Format("Deleting folder {0}...", path));
            }
            catch (Exception e) { WriteToLog(server, master, "Error", e); }
        }
        public static string ProductName
        {
            get
            {
                AssemblyProductAttribute myProduct = (AssemblyProductAttribute)
                    AssemblyProductAttribute.GetCustomAttribute(System.Reflection.Assembly.GetExecutingAssembly(),
                                 typeof(AssemblyProductAttribute));
                return myProduct.Product;
            }
        }
        private static bool IsTrailExpired(string value)
        {
            try
            {
                string s = value.Substring(12, value.Length - 12);
                s = s.Substring(0, s.Length - 6);

                DateTime _trial_value = Number2Date(s);
                DateTime _now = DateTime.Now;
                DateTime _trial_trial_expiration_value = _trial_value.AddDays(30);

                TimeSpan ts = _trial_trial_expiration_value - _now;
                int _remaining_days_ = Convert.ToInt32( ts.TotalDays);

                WriteToLog("", "", "Info", string.Format("{0} Trial will expire in {1} days", application_name, _remaining_days_.ToString() ));

                if(_remaining_days_ < 0)
                {
                    WriteToLog("", "", "Info", string.Format("{0} Trial has expired ", application_name));
                    return true;
                }
            }
            catch (Exception e) { WriteToLog("", master, "Error", e); }
            return false;
        }
        private static DateTime Number2Date(string strNum)
        {
            int iDay, iMon, iYear;
            DateTime ValidDate = DateTime.Now;
           
            //string day, month, year;

            //month = strNum.Substring(0, 2);
            //day = strNum.Substring(2, 2);
            //year = strNum.Substring(4, 2);

            try
            {
                iMon = Convert.ToInt32(strNum.Substring(0, 2));
                iDay = Convert.ToInt32(strNum.Substring(2, 2));
                iYear = Convert.ToInt32(strNum.Substring(4, 4));
                ValidDate = new DateTime(iYear, iMon, iDay);
            }
            catch (Exception e) { WriteToLog("", "", "Error", e); }
            return ValidDate;
        }
        
    }

  

}



  [XmlRoot("DeploymentReport", Namespace = "http://schemas.microsoft.com/sqlserver/dac/DeployReport/2012/02")]
public class DeploymentReport
{
    public List<Operation> Operations { get; set; }
}

public class Item
  {
      [XmlAttribute]
      public string Value { get; set; }
      [XmlAttribute]
      public string Type { get; set; }
  }

  public class Operation
  {
      [XmlAttribute]
      public string Name { get; set; }

      [XmlElement("Item")]
      public List<Item> Items { get; set; }
  }

  public static class XmlSerializationHelper
  {
      public static T LoadFromXML<T>(this string xmlString)
      {
          T returnValue = default(T);

          XmlSerializer serial = new XmlSerializer(typeof(T));
          using (StringReader reader = new StringReader(xmlString))
          {
              object result = serial.Deserialize(reader);
              if (result is T)
              {
                  returnValue = (T)result;
              }
          }
          return returnValue;
      }
        
      public static T LoadFromFile<T>(string filename)
      {
          XmlSerializer serial = new XmlSerializer(typeof(T));
          try
          {
              using (var fs = new FileStream(filename, FileMode.Open))
              {
                  object result = serial.Deserialize(fs);
                  if (result is T)
                  {
                      return (T)result;
                  }
              }
          }
          catch (Exception ex)
          {
              Debug.WriteLine(ex.ToString());
          }
          return default(T);
      }

      public static string GetXml<T>(T obj, XmlSerializer serializer, bool omitStandardNamespaces)
      {
          using (var textWriter = new StringWriter())
          {
              XmlWriterSettings settings = new XmlWriterSettings();
              settings.Indent = true;        // For cosmetic purposes.
              settings.IndentChars = "    "; // For cosmetic purposes.
              using (var xmlWriter = XmlWriter.Create(textWriter, settings))
              {
                  if (omitStandardNamespaces)
                  {
                      XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                      ns.Add("", ""); // Disable the xmlns:xsi and xmlns:xsd lines.
                      serializer.Serialize(xmlWriter, obj, ns);
                  }
                  else
                  {
                      serializer.Serialize(xmlWriter, obj);
                  }
              }
              return textWriter.ToString();
          }
      }

      public static string GetXml<T>(this T obj, bool omitNamespace)
      {
          XmlSerializer serializer = new XmlSerializer(obj.GetType());
          return GetXml(obj, serializer, omitNamespace);
      }

      public static string GetXml<T>(this T obj)
      {
          return GetXml(obj, false);
      }
  }
  




/*  
  public static class DeploymentReportTest
  {
      public static void Test(string file)
      {
          string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                          <DeploymentReport xmlns=""http://schemas.microsoft.com/sqlserver/dac/DeployReport/2012/02"">
                            <Alerts />
                            <Operations>
                              <Operation Name=""Create"">
                                <Item Value=""[dbo].[T1]"" Type=""SqlTable"" />
                                <Item Value=""[dbo].[DF_C2]"" Type=""SqlDefaultConstraint"" />
                                <Item Value=""[dbo].[TR1]"" Type=""SqlDmlTrigger"" />
                                <Item Value=""[dbo].[V1]"" Type=""SqlView"" />
                              </Operation>
                              <Operation Name=""Alter"">
                                <Item Value=""[dbo].[P1]"" Type=""SqlProcedure"" />
                              </Operation>
                            </Operations>
                          </DeploymentReport>
                          ";
          try
          {
              var report = XmlSerializationHelper.LoadFromXML<DeploymentReport>(xml);
              Console.WriteLine(report.GetXml());

              var report3 = XmlSerializationHelper.LoadFromFile<DeploymentReport>(file);

              Console.WriteLine(report3.GetXml());
          }
          catch (Exception ex)
          {
              Debug.Assert(false, ex.ToString()); // No assert
          }
      }
           
  }
  */


/*
       private static void SQLPackageExtract(string server, string database)
       {
           try
           {
               ExcecuteSQLPackageExtract(SourceServerSchemaFile, SourceServer, SourceServerDatabase, SourceServerConnectionString);
           }
           catch (Exception e) { WriteToLog(server, database, "Error", e); }
       }
       private static void ExcecuteSQLPackageExtract(string target_file, string server, string database, string connection_string)
       {
           try
           {
               string _file = @"C:\Program Files (x86)\Microsoft SQL Server\110\DAC\bin\SqlPackage.exe";
               string _action = string.Format(" /Action:{0}", "Extract");
               string _target_file = string.Format(" /TargetFile:{0}", target_file);
               //string _source_connection_string = string.Format("//SourceConnectionString:{0}", connection_string );
               string _source_server = string.Format(" /SourceServerName:{0}", server);
               string _source_database = string.Format(" /SourceDatabaseName:{0}", database);
               string _mode = " /Quiet:True"; // ? not working

               ProcessStartInfo psi = new ProcessStartInfo();
               psi.FileName = _file;
               psi.Arguments = _action + _mode + _target_file + _source_server + _source_database;
               psi.UseShellExecute = false;

               Process p = new Process();
               p.StartInfo = psi;
               p.Start();
               p.WaitForExit();
           }
           catch (Exception e) { WriteToLog("", database, "Error", e); }
       }

       private static void ExcecuteSQLPackageScript(string source_schema_file, string target_script_file, string server, string database, string connection_string)
       {
           try
           {
               string _file = @"C:\Program Files (x86)\Microsoft SQL Server\110\DAC\bin\SqlPackage.exe";
               string _action = string.Format(" /Action:{0}", "Script");
               string _source_file = string.Format(" /SourceFile:{0}", source_schema_file);
               string _target_file = string.Format(" /OutputPath:{0}", target_script_file);
               //string _target_connection_string = string.Format("/TargetConnectionString:{0}",  _connection_string);
               string _target_server = string.Format(" /TargetServerName:{0}", server);
               string _target_database = string.Format(" /TargetDatabaseName:{0}", database);

               ProcessStartInfo psi = new ProcessStartInfo();
               psi.FileName = _file;
               psi.Arguments = _action + _source_file + _target_file + _target_server + _target_database;
               psi.UseShellExecute = false;

               Process p = new Process();
               p.StartInfo = psi;
               p.Start();
           }
           catch (Exception e) { WriteToLog("", database, "Error", e); }
       }

       private static void ExcecuteSQLPackagePublish(string source_schema_file, string server, string database, string connection_string)
       {
           try
           {
               string _file = @"C:\Program Files (x86)\Microsoft SQL Server\110\DAC\bin\SqlPackage.exe";
               string _action = string.Format(" /Action:{0}", "Publish");
               string _source_file = string.Format(" /SourceFile:{0}", source_schema_file);            
               //string _target_connection_string = string.Format("/TargetConnectionString:{0}",  _connection_string);
               string _target_server = string.Format(" /TargetServerName:{0}", server);
               string _target_database = string.Format(" /TargetDatabaseName:{0}", database);

               ProcessStartInfo psi = new ProcessStartInfo();
               psi.FileName = _file;
               psi.Arguments = _action + _source_file + _target_server + _target_database;
               psi.UseShellExecute = false;

               Process p = new Process();
               p.StartInfo = psi;
               p.Start();
           }
           catch (Exception e) { WriteToLog("", database, "Error", e); }
       }

       private static void ExcecuteSQLPackageDeployReport(string source_schema_file, string output_report_file, string server, string database, string connection_string)
       {
           try
           {
               string _file = @"C:\Program Files (x86)\Microsoft SQL Server\110\DAC\bin\SqlPackage.exe";
               string _action = string.Format(" /Action:{0}", "DeployReport");
               string _source_file = string.Format(" /SourceFile:{0}", source_schema_file);
               string _output_path = string.Format(@" /OutputPath:{0}", output_report_file);
               //string _target_connection_string = string.Format("/TargetConnectionString:{0}", connection_string);
               string _target_server = string.Format(" /TargetServerName:{0}", "YETROGI-PC\\SQL2012");
               string _target_database = string.Format(" /TargetDatabaseName:{0}", database);

               ProcessStartInfo psi = new ProcessStartInfo();
               psi.FileName = _file;
               psi.Arguments = _action + _source_file + _output_path + _target_server + _target_database;
               psi.UseShellExecute = false;

               Process p = new Process();
               p.StartInfo = psi;
               p.Start();
               p.WaitForExit();
           }
           catch (Exception e) { WriteToLog("", database, "Error", e); }
       }
        * */