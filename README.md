# CLR-Trigger-Csharp
CLR Trigger MSSQL C# dll


1)enable CLR 
EXEC sp_configure 'clr enabled', 1;
GO
    reconfigure
GO
2)Change strict security
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;

3)visual studio assembly 
alter database 'databasename'  set trustworthy on;

GO
CREATE ASSEMBLY CLRTrigger
FROM 'path of the dll file'
WITH PERMISSION_SET = UNSAFE
GO 
4) Create Trigger

CREATE TRIGGER 'TriggerName'
ON 'databasename'
FOR INSERT, UPDATE, DELETE
AS
EXTERNAL NAME CLRTrigger.Triggers.SqlTrigger1;