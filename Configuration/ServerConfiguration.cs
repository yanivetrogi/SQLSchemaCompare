using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Microsoft.SqlServer.Dac;

namespace SQLSchemaCompare
{
    public class Servers
    {       
        List<ServerSettings> _serverSettings = new List<ServerSettings>(32);
        
        public List<ServerSettings> ServerSettingsList
        {
            get { return _serverSettings; }
            set { _serverSettings = value; }
        }

        public Servers()
        {            
        }


        public void Save()
        {
            XmlSerializer s = new XmlSerializer(typeof(List<ServerSettings>));
            using (TextWriter w = new StreamWriter(@"Servers.config"))
            {
                s.Serialize(w, _serverSettings);
                w.Close();
            }
        }

        public void Load()
        {
            string _full_file_name = Path.Combine( Path.Combine(new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName, "Configuration"), "Servers.config");

            // Target servers
            XmlSerializer s = new XmlSerializer(typeof(List<ServerSettings>));
            using (TextReader r = new StreamReader(_full_file_name))
            {
                _serverSettings = (List<ServerSettings>)s.Deserialize(r);
                r.Close();
            }


            System.Xml.XPath.XPathDocument doc = new System.Xml.XPath.XPathDocument(_full_file_name);

            // ReportServer. This is the server where the database table is located
            foreach (System.Xml.XPath.XPathNavigator n in doc.CreateNavigator().Select("ArrayOfServerSettings/ReportServer"))
            {
                ReportServer.ReportServerInstance = n.SelectSingleNode("ReportServerInstance").Value;
                ReportServer.ReportServerWindowsAutherntication = Convert.ToBoolean(n.SelectSingleNode("ReportServerWindowsAutherntication").Value);
                ReportServer.ReportServerUser = n.SelectSingleNode("ReportServerUser").Value;
                ReportServer.ReportServerPassword = n.SelectSingleNode("ReportServerPassword").Value;
            }

            // Source server
            foreach (System.Xml.XPath.XPathNavigator n in doc.CreateNavigator().Select("ArrayOfServerSettings/SourceServerSettings"))
            {
                SourceSettings.SourceServer = n.SelectSingleNode("SourceServer").Value;
                SourceSettings.SourceWindowsAutherntication = Convert.ToBoolean(n.SelectSingleNode("SourceWindowsAutherntication").Value);
                SourceSettings.SourceUser = n.SelectSingleNode("SourceUser").Value;
                SourceSettings.SourcePassword = n.SelectSingleNode("SourcePassword").Value;
                SourceSettings.SourceDatabase = n.SelectSingleNode("SourceDatabase").Value;

                SourceSettings.UseExistingSourceFile = Convert.ToBoolean(n.SelectSingleNode("UseExistingSourceFile").Value);
                SourceSettings.SourceFileFullPath = n.SelectSingleNode("SourceFileFullPath").Value;
                SourceSettings.Action = n.SelectSingleNode("Action").Value;
            }

            // ScriptFilesOptions
            foreach (System.Xml.XPath.XPathNavigator n in doc.CreateNavigator().Select("ArrayOfServerSettings/ScriptFilesOptions"))
            {
                ScriptFilesOptions.PathToSaveScriptFiles = n.SelectSingleNode("PathToSaveScriptFiles").Value;
                ScriptFilesOptions.ZipFiles = Convert.ToBoolean(n.SelectSingleNode("ZipFiles").Value);
                ScriptFilesOptions.ZipPassword = n.SelectSingleNode("ZipPassword").Value;
                ScriptFilesOptions.PathToSaveScriptFiles = n.SelectSingleNode("PathToSaveScriptFiles").Value;

            }



            foreach (System.Xml.XPath.XPathNavigator n in doc.CreateNavigator().Select("ArrayOfServerSettings/PackageOptions"))
            {
                if (n.SelectSingleNode("ExportData") != null)
                {
                    PackageOptions.ExportData = Convert.ToBoolean(n.SelectSingleNode("ExportData").Value);

                    if (PackageOptions.ExportData)
                    {
                        if (n.SelectSingleNode("TablesToExportData") != null)
                        {
                            string[] tables = n.SelectSingleNode("TablesToExportData").Value.Split(new Char[] { ';' });

                            foreach (string table in tables)
                            {
                                // The input string is in the form of schema.table
                                if (table.Contains("."))
                                {
                                    // Cut out the schema and table from the input string
                                    string _schema = table.Substring(0, table.IndexOf('.')).Trim();
                                    string _table = table.Substring(table.IndexOf('.') + 1, table.Length - table.IndexOf('.') - 1).Trim();

                                    // Populate the list to be passed to Program.GeneratePackage method
                                    //Program._tablesToExtractDataList.Add(new TablesToExtractData { schema = _schema, table = _table });
                                    Program._tablesToExportData.Add(new Tuple<string, string>(_schema, _table));
                                }
                                else
                                {
                                    // The input string is not in the form of schema.table
                                    Program.WriteToLog(Program.SourceServerPadded, Program.SourceServerDatabasePadded, "Error", String.Format
                                        ("The given input value for 'TablesToExportData' is not valid: {0}. A valid value should be in the form of schema.table", table.Trim()));
                                }
                            }
                        }


                    }
                }


                 // private static List<TablesToExtractData> _tablesToExtractDataList;

                if (n.SelectSingleNode("ExtractApplicationScopedObjectsOnly") != null)
                {
                    PackageOptions.ExtractApplicationScopedObjectsOnly = Convert.ToBoolean(n.SelectSingleNode("ExtractApplicationScopedObjectsOnly").Value);
                }
                else
                {
                    PackageOptions.ExtractApplicationScopedObjectsOnly = false;
                }

                if (n.SelectSingleNode("ExtractReferencedServerScopedElements") != null)
                {
                    PackageOptions.ExtractReferencedServerScopedElements = Convert.ToBoolean(n.SelectSingleNode("ExtractReferencedServerScopedElements").Value);
                }
                else
                {
                    PackageOptions.ExtractReferencedServerScopedElements = false;
                }

                if (n.SelectSingleNode("IgnoreExtendedProperties") != null)
                {
                    PackageOptions.IgnoreExtendedProperties = Convert.ToBoolean(n.SelectSingleNode("IgnoreExtendedProperties").Value);
                }
                else
                {
                    PackageOptions.IgnoreExtendedProperties = false;
                }

                if (n.SelectSingleNode("IgnorePermissions") != null)
                {
                    PackageOptions.IgnorePermissions = Convert.ToBoolean(n.SelectSingleNode("IgnorePermissions").Value);
                }
                else
                {
                    PackageOptions.IgnorePermissions = false;
                }

                if (n.SelectSingleNode("IgnoreUserLoginMappings") != null)
                {
                    PackageOptions.IgnoreUserLoginMappings = Convert.ToBoolean(n.SelectSingleNode("IgnoreUserLoginMappings").Value);
                }
                else
                {
                    PackageOptions.IgnoreUserLoginMappings = false;
                }

                if (n.SelectSingleNode("VerifyExtraction") != null)
                {
                    PackageOptions.VerifyExtraction = Convert.ToBoolean(n.SelectSingleNode("VerifyExtraction").Value);
                }
                else
                {
                    PackageOptions.VerifyExtraction = false;
                }

            }

            // Deploy options
            foreach (System.Xml.XPath.XPathNavigator child in doc.CreateNavigator().Select("ArrayOfServerSettings/DeployOptions"))
            {
                if (child.SelectSingleNode("IgnoreLoginSids") != null)
                {
                    DeployOptions.IgnoreLoginSids = Convert.ToBoolean(child.SelectSingleNode("IgnoreLoginSids").Value);
                }
                else
                {
                    DeployOptions.IgnoreLoginSids = false;
                }

                if (child.SelectSingleNode("IgnorePermissions") != null)
                {
                    DeployOptions.IgnorePermissions = Convert.ToBoolean(child.SelectSingleNode("IgnorePermissions").Value);
                }
                else
                {
                    DeployOptions.IgnorePermissions = false;
                }

                if (child.SelectSingleNode("IgnoreRoleMembership") != null)
                {
                    DeployOptions.IgnoreRoleMembership = Convert.ToBoolean(child.SelectSingleNode("IgnoreRoleMembership").Value);
                }
                else
                {
                    DeployOptions.IgnoreRoleMembership = false;
                }

                if (child.SelectSingleNode("DropObjectsNotInSource") != null)
                {
                    DeployOptions.DropObjectsNotInSource = Convert.ToBoolean(child.SelectSingleNode("DropObjectsNotInSource").Value);
                }
                else
                {
                    DeployOptions.DropObjectsNotInSource = false;
                }

                if (child.SelectSingleNode("DropRoleMembersNotInSource") != null)
                {
                    DeployOptions.DropRoleMembersNotInSource = Convert.ToBoolean(child.SelectSingleNode("DropRoleMembersNotInSource").Value);
                }
                else
                {
                    DeployOptions.DropRoleMembersNotInSource = false;
                }

                if (child.SelectSingleNode("UnmodifiableObjectWarnings") != null)
                {
                    DeployOptions.UnmodifiableObjectWarnings = Convert.ToBoolean(child.SelectSingleNode("UnmodifiableObjectWarnings").Value);
                }
                else
                {
                    DeployOptions.UnmodifiableObjectWarnings = false;
                }

                if (child.SelectSingleNode("VerifyDeployment") != null)
                {
                    DeployOptions.VerifyDeployment = Convert.ToBoolean(child.SelectSingleNode("VerifyDeployment").Value);
                }
                else
                {
                    DeployOptions.VerifyDeployment = false;
                }

                if (child.SelectSingleNode("CommandTimeout") != null)
                {
                    DeployOptions.CommandTimeout = Convert.ToInt32(child.SelectSingleNode("CommandTimeout").Value);
                }
                else
                {
                    DeployOptions.CommandTimeout = 30;
                }

                if (child.SelectSingleNode("IgnoreNotForReplication") != null)
                {
                    DeployOptions.IgnoreNotForReplication = Convert.ToBoolean(child.SelectSingleNode("IgnoreNotForReplication").Value);
                }
                else
                {
                    DeployOptions.IgnoreNotForReplication = false;
                }

                if (child.SelectSingleNode("DoNotAlterReplicatedObjects") != null)
                {
                    DeployOptions.DoNotAlterReplicatedObjects = Convert.ToBoolean(child.SelectSingleNode("DoNotAlterReplicatedObjects").Value);
                }
                else
                {
                    DeployOptions.DoNotAlterReplicatedObjects = false;
                }

                if (child.SelectSingleNode("IgnoreKeywordCasing") != null)
                {
                    DeployOptions.IgnoreKeywordCasing = Convert.ToBoolean(child.SelectSingleNode("IgnoreKeywordCasing").Value);
                }
                else
                {
                    DeployOptions.IgnoreKeywordCasing = false;
                }

                if (child.SelectSingleNode("DropIndexesNotInSource") != null)
                {
                    DeployOptions.DropIndexesNotInSource = Convert.ToBoolean(child.SelectSingleNode("DropIndexesNotInSource").Value);
                }
                else
                {
                    DeployOptions.DropIndexesNotInSource = false;
                }

                if (child.SelectSingleNode("IgnoreIdentitySeed") != null)
                {
                    DeployOptions.IgnoreIdentitySeed = Convert.ToBoolean(child.SelectSingleNode("IgnoreIdentitySeed").Value);
                }
                else
                {
                    DeployOptions.IgnoreIdentitySeed = false;
                }

                if (child.SelectSingleNode("IgnoreIncrement") != null)
                {
                    DeployOptions.IgnoreIncrement = Convert.ToBoolean(child.SelectSingleNode("IgnoreIncrement").Value);
                }
                else
                {
                    DeployOptions.IgnoreIncrement = false;
                }

                if (child.SelectSingleNode("IncludeTransactionalScripts".Trim()) != null)
                {
                    DeployOptions.IncludeTransactionalScripts  = Convert.ToBoolean(child.SelectSingleNode("IncludeTransactionalScripts".Trim()).Value);
                }
                else
                {
                    DeployOptions.IncludeTransactionalScripts  = false;
                }



                if (child.SelectSingleNode("CommentOutSetVarDeclarations".Trim()) != null)
                {
                    DeployOptions.CommentOutSetVarDeclarations = Convert.ToBoolean(child.SelectSingleNode("CommentOutSetVarDeclarations".Trim()).Value);
                }
                else
                {
                    DeployOptions.CommentOutSetVarDeclarations = false;
                }

                if (child.SelectSingleNode("IgnoreLockHintsOnIndexes".Trim()) != null)
                {
                    DeployOptions.IgnoreLockHintsOnIndexes = Convert.ToBoolean(child.SelectSingleNode("IgnoreLockHintsOnIndexes".Trim()).Value);
                }
                else
                {
                    DeployOptions.IgnoreLockHintsOnIndexes = false;
                }

                if (child.SelectSingleNode("DatabaseLockTimeout".Trim()) != null)
                {
                    DeployOptions.DatabaseLockTimeout = Convert.ToInt32(child.SelectSingleNode("DatabaseLockTimeout").Value);
                }
                else
                {
                    DeployOptions.DatabaseLockTimeout = 60;
                }

                if (child.SelectSingleNode("IgnoreIndexOptions".Trim()) != null)
                {
                    DeployOptions.IgnoreIndexOptions = Convert.ToBoolean(child.SelectSingleNode("IgnoreIndexOptions".Trim()).Value);
                }
                else
                {
                    DeployOptions.IgnoreIndexOptions = false;
                }

                if (child.SelectSingleNode("IgnoreTableOptions".Trim()) != null)
                {
                    DeployOptions.IgnoreTableOptions = Convert.ToBoolean(child.SelectSingleNode("IgnoreTableOptions".Trim()).Value);
                }
                else
                {
                    DeployOptions.IgnoreTableOptions = false;
                }

                if (child.SelectSingleNode("IgnoreWithNocheckOnForeignKeys".Trim()) != null)
                {
                    DeployOptions.IgnoreWithNocheckOnForeignKeys = Convert.ToBoolean(child.SelectSingleNode("IgnoreWithNocheckOnForeignKeys".Trim()).Value);
                }
                else
                {
                    DeployOptions.IgnoreWithNocheckOnForeignKeys = false;
                }

                if (child.SelectSingleNode("IgnoreWithNocheckOnCheckConstraints".Trim()) != null)
                {
                    DeployOptions.IgnoreWithNocheckOnCheckConstraints = Convert.ToBoolean(child.SelectSingleNode("IgnoreWithNocheckOnCheckConstraints".Trim()).Value);
                }
                else
                {
                    DeployOptions.IgnoreWithNocheckOnCheckConstraints = false;
                }

                if (child.SelectSingleNode("BlockOnPossibleDataLoss".Trim()) != null)
                {
                    DeployOptions.BlockOnPossibleDataLoss = Convert.ToBoolean(child.SelectSingleNode("BlockOnPossibleDataLoss".Trim()).Value);
                }
                else
                {
                    DeployOptions.BlockOnPossibleDataLoss = false;
                }

                if (child.SelectSingleNode("IgnoreUserSettingsObjects".Trim()) != null)
                {
                    DeployOptions.IgnoreUserSettingsObjects = Convert.ToBoolean(child.SelectSingleNode("IgnoreUserSettingsObjects".Trim()).Value);
                }
                else
                {
                    DeployOptions.IgnoreUserSettingsObjects = false;
                }

                if (child.SelectSingleNode("IgnoreWhitespace".Trim()) != null)
                {
                    DeployOptions.IgnoreWhitespace = Convert.ToBoolean(child.SelectSingleNode("IgnoreWhitespace".Trim()).Value);
                }
                else
                {
                    DeployOptions.IgnoreWhitespace = false;
                }



                if (child.SelectSingleNode("ExcludeObjectTypes") != null)
                {
                    string[] excludeObjectTypesArr = child.SelectSingleNode("ExcludeObjectTypes").Value.Split(';');

                    var _objectTypes = new List<Microsoft.SqlServer.Dac.ObjectType>();

                    foreach (string sObjectType in excludeObjectTypesArr)
                    {
                        if (Enum.TryParse<ObjectType>(sObjectType, out ObjectType objectType))
                            _objectTypes.Add(objectType);
                    }

                    DeployOptions.ExcludeObjectTypes = _objectTypes.ToArray();
                }



            }

        }

    }
}
