create function dbo.GetHidCode
(
	@Hid hierarchyid
)
returns varchar(1784)
as
begin

	return replace(convert(varchar(1784), cast(@Hid as varbinary(892)), 2), '0x', '');

end;
