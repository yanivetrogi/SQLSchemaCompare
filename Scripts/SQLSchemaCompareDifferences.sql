USE [master]
GO


/*

/* Get all differences */
USE master; SET NOCOUNT ON;
SELECT  
	 [id]
	,[insert_time]
	,[source_server]
    ,[source_database]
    ,[target_server]
    ,[target_database]
    ,[schema]
    ,[object]
    ,[operation]
    ,[type]
FROM [dbo].[SQLSchemaCompareDifferences] WHERE 1=1
AND type NOT IN ('SqlUser', 'SqlRole')
ORDER BY id;

/* Get all differences grouped by type */
SELECT  
	COUNT(*) cnt
    ,[type]
FROM [dbo].[SQLSchemaCompareDifferences] WHERE 1=1
AND type NOT IN ('SqlUser', 'SqlRole')
GROUP BY [type]
ORDER BY cnt;


*/


/****** Object:  Table [dbo].[SQLSchemaCompareDifferences]    Script Date: 12/17/2014 6:39:24 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SQLSchemaCompareDifferences]
(
	[id] [int] IDENTITY(1,1) NOT NULL,
	[insert_time] [datetime] NOT NULL CONSTRAINT [DF_insert_time]  DEFAULT (GETDATE()),
	[source_server] [sysname] COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[source_database] [sysname] COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[target_server] [sysname] COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[target_database] [sysname] COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[schema] [sysname] COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[object] [sysname] COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[operation] [sysname] COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[type] [sysname] COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_SQLSchemaCompareDifferences] PRIMARY KEY NONCLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Index [IXC_SQLSchemaCompareDifferences__insert_time]    Script Date: 12/17/2014 6:39:24 AM ******/
CREATE CLUSTERED INDEX [IXC_SQLSchemaCompareDifferences__insert_time] ON [dbo].[SQLSchemaCompareDifferences]
(
	[insert_time] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


