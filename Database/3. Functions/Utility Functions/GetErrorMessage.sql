create function dbo.GetErrorMessage
(		
)
returns nvarchar(4000)
as
begin
	return N'An error occurred in the stored procedure ' + error_procedure() + ' '
		+ N'on line ' + cast(error_line() as varchar(10)) + '. '
		+ N'Error number: ' + cast(error_number() as varchar(10)) + '. '
		+ N'Error message: ' + error_message() + '.';	
end;
