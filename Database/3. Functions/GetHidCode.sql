create function dbo.GetHidCode
(
	@Hid hierarchyid
)
returns varchar(1000)
as
begin

	return replace(convert(varchar(1000), cast(@Hid as varbinary(892)), 2), '0x', '');

end;
