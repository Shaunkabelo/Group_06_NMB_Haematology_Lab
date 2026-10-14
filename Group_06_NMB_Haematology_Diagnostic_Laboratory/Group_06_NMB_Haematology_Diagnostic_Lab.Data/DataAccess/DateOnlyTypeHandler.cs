using System.Data;
using Dapper;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;

public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => value switch
    {
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        DateOnly date => date,
        _ => throw new DataException($"Cannot convert {value.GetType().Name} to DateOnly.")
    };

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
}
