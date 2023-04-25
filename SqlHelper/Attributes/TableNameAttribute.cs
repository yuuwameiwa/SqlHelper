namespace SqlHelper.Attributes
{
    /// <summary>
    /// Attribute for models that identifying Table Name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableNameAttribute : Attribute
    {
        private readonly string _tableName;

        public TableNameAttribute(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentNullException($"{nameof(tableName)} is null");

            _tableName = tableName;
        }

        public string TableName => _tableName;
    }
}