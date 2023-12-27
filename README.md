
24/12/2023

SQLSchemaCompare

General
SQLSchemaCompare is a tool designed to compare SQL Server databases schema between a source server and multiple target servers and find schema differences if exist.
SQLSchemaCompare is a .NET 4.8 console application that can be scheduled to run automatically.
The application supports comparing schema between a single source server and an unlimited number of target servers while each task is carried out in a dedicated thread for optimal performance. 
The applications support all current SQL Server versions that exists (version 2022 and backwards).

Features
•	Detect schema differences and log to database table for Monitoring puposes
•	Generate a deployment script 
•	Deploy schema changes at target server(s) 
•	Deploy data changes at target server(s) 

Monitoring
Having the differences between source and target servers logged to a table allows us to query the table and send a notification email if a change was logged since the last verification was done.
Such a query can be executed from a monitoring tool or from an SQL Server Agent job. 

Configuration
Servers.config
•	Here is where you define all the servers related information
•	There are 5 main configurations:
o	SourceServerSettings
	Defines the settings for the source server
o	PackageOptions
	Defines the source package properties
	For the complete list of available properties see this link:
http://msdn.microsoft.com/en-us/library/microsoft.sqlserver.dac.dacextractoptions.aspx

o	DeployOptions
	Defines properties related to the 3 Actions that the program supports: 
•	Report
•	Script
•	Deploy
	For the complete list of available properties see this link:
http://msdn.microsoft.com/en-us/library/microsoft.sqlserver.dac.dacdeployoptions.aspx
	List of objects at the target that can be excluded from comparison
https://learn.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.dac.objecttype?view=sql-dacfx-162





o	ServerSettings
	Defines the settings for the target servers.
	Each xml node represents a target server

o	ReportServer
	Defines connection string properties for the server holding the Report Table
