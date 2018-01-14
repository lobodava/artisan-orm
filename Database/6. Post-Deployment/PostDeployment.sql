/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

:R  "1. Users.sql"

:R  "2. Roles.sql"

:R  "3. UserRoles.sql"

:R  "4. RecordTypes.sql"

:R  "5. Records.sql"

:R  "6. Sequence reset.sql"

exec dbo.GenerateFolders;


