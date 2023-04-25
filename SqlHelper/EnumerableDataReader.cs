using System.Data;
using System.Data.Common;
using System.Collections;
using System.Reflection;

public class EnumerableDataReader<T> : DbDataReader
{
    private IEnumerator<T> _enumerator;
    private bool _closed;

    public EnumerableDataReader(IEnumerable<T> enumerable)
    {
        _enumerator = enumerable.GetEnumerator();
    }

    public override object this[int ordinal] => GetValue(ordinal);

    public override object this[string name] => GetValue(GetOrdinal(name));

    public override void Close()
    {
        _enumerator?.Dispose();
        _enumerator = null;
        _closed = true;
    }

    public override bool GetBoolean(int ordinal)
    {
        return (bool)GetValue(ordinal);
    }

    public override byte GetByte(int ordinal)
    {
        return (byte)GetValue(ordinal);
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
    {
        byte[] bytes = (byte[])GetValue(ordinal);
        int bytesToCopy = Math.Min(length, bytes.Length - (int)dataOffset);
        Buffer.BlockCopy(bytes, (int)dataOffset, buffer, bufferOffset, bytesToCopy);
        return bytesToCopy;
    }

    public override char GetChar(int ordinal)
    {
        return (char)GetValue(ordinal);
    }

    public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
    {
        string str = (string)GetValue(ordinal);
        int charsToCopy = Math.Min(length, str.Length - (int)dataOffset);
        str.CopyTo((int)dataOffset, buffer, bufferOffset, charsToCopy);
        return charsToCopy;
    }

    public override string GetDataTypeName(int ordinal)
    {
        return typeof(T).GetProperties()[ordinal].PropertyType.Name;
    }

    public override DateTime GetDateTime(int ordinal)
    {
        return (DateTime)GetValue(ordinal);
    }

    public override decimal GetDecimal(int ordinal)
    {
        return (decimal)GetValue(ordinal);
    }

    public override double GetDouble(int ordinal)
    {
        return (double)GetValue(ordinal);
    }

    public override Type GetFieldType(int ordinal)
    {
        return typeof(T).GetProperties()[ordinal].PropertyType;
    }

    public override float GetFloat(int ordinal)
    {
        return (float)GetValue(ordinal);
    }

    public override Guid GetGuid(int ordinal)
    {
        return (Guid)GetValue(ordinal);
    }

    public override short GetInt16(int ordinal)
    {
        return (short)GetValue(ordinal);
    }

    public override int GetInt32(int ordinal)
    {
        return (int)GetValue(ordinal);
    }

    public override long GetInt64(int ordinal)
    {
        return (long)GetValue(ordinal);
    }

    public override string GetName(int ordinal)
    {
        return typeof(T).GetProperties()[ordinal].Name;
    }

    public override int GetOrdinal(string name)
    {
        return Array.IndexOf(typeof(T).GetProperties(), typeof(T).GetProperty(name));
    }

    public override DataTable GetSchemaTable()
    {
        DataTable schemaTable = new DataTable();
        schemaTable.Columns.Add("ColumnName", typeof(string));
        schemaTable.Columns.Add("DataType", typeof(Type));
        schemaTable.Columns.Add("ColumnOrdinal", typeof(int));
        for (int i = 0; i < FieldCount; i++)
        {
            schemaTable.Rows.Add(GetName(i), GetFieldType(i), i);
        }
        return schemaTable;
    }

    public override string GetString(int ordinal)
    {
        return (string)GetValue(ordinal);
    }

    public override object GetValue(int ordinal)
    {
        PropertyInfo property = typeof(T).GetProperties()[ordinal];
        return property.GetValue(_enumerator.Current);
    }

    public override int GetValues(object[] values)
    {
        int count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }
        return count;
    }

    public override bool IsDBNull(int ordinal)
    {
        return GetValue(ordinal) == DBNull.Value;
    }

    public override bool NextResult()
    {
        return false;
    }

    public override bool Read()
    {
        if (_closed)
        {
            throw new InvalidOperationException("DataReader is closed.");
        }
        return _enumerator.MoveNext();
    }

    public override IEnumerator GetEnumerator()
    {
        return _enumerator;
    }

    public override int FieldCount => typeof(T).GetProperties().Length;

    public override int Depth => throw new NotSupportedException();

    public override bool HasRows => true;

    public override bool IsClosed => _closed;

    public override int RecordsAffected => throw new NotSupportedException();
}