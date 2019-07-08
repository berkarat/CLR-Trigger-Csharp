EXEC sp_configure 'clr enabled', 1;
GO
    reconfigure
GO

EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;


--AFTER CREATE C# dll
alter database 'databasename'  set trustworthy on;

GO
CREATE ASSEMBLY CLRTrigger
FROM 'path of the dll file'
WITH PERMISSION_SET = UNSAFE
GO 


--CREATE TRIGGER 
CREATE TRIGGER 'TriggerName'
ON 'databasename'
FOR INSERT, UPDATE, DELETE
AS
EXTERNAL NAME CLRTrigger.Triggers.SqlTrigger1;		  --TriggerName C# dll