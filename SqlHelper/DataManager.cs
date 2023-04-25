using System.Data;
using System.Reflection;
using System.Text;

using Microsoft.Data.SqlClient;

using SqlHelper.Attributes;

namespace SqlHelper
{
    /// <summary>
    /// The whole abstraction of Sql Database. Allows to write and read data.
    /// Pass connection string to be able to work with manager.
    /// </summary>
    public class DataManager
    {
        private readonly SqlConnection _sqlConnection;

        private string _tableName { get; set; } = string.Empty;
        private string? _identityColumn { get; set; } = null;
        
        public DataManager(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString), $"SqlConnection string is null");

            _sqlConnection = new SqlConnection(connectionString);
        }
        
        #region Writer
        /// <summary>
        /// Inserts one model to database using SqlTableAttribute.
        /// </summary>
        /// <returns>
        /// Returns pasted Id from database.
        /// </returns>
        public int? InsertOne<T>(T data) where T : class
        {
            data = data ?? throw new ArgumentNullException($"{nameof(data)} is null.");

            // Data must contain TableNameAttribute and can have IdentityColumnAttribute
            Type type = data.GetType();
            SetAttributes(type);

            // Create dictionary of properties and values from transferred model
            Dictionary<string, dynamic> insertDict = ToDictionary(data, _identityColumn);

            // Build SqlCommand
            SqlCommand sqlInsertCommand = BuildInsertCommand(insertDict);

            int? id = null;
            // Execution
            _sqlConnection.Open();

            if (!string.IsNullOrWhiteSpace(_identityColumn))
            {
                id = Convert.ToInt32(sqlInsertCommand.ExecuteScalar());
            }
            else
            {
                sqlInsertCommand.ExecuteNonQuery();
            }

            // Close connection
            _sqlConnection.Close();

            // Reset TableName, IdentityColumn
            ResetAttributes();

            return id;
        }

        /// <summary>
        /// Inserts IEnumerable to database using SqlTableAttribute. Identity Column can be null. IEnumerable can be lists or arrays.
        /// </summary>
        public void InsertEnumerable<T>(IEnumerable<T> data) where T : class
        {
            data = data ?? throw new ArgumentNullException($"{nameof(data)} is null.");

            // Data must contain TableNameAttribute and can have IdentityColumnAttribute
            T firstElement = data.First();
            Type type = firstElement.GetType();
            SetAttributes(type);

            // Paste data with bulkCopy
            _sqlConnection.Open();
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(_sqlConnection))
            using (var reader = new EnumerableDataReader<T>(data))
            {
                bulkCopy.DestinationTableName = _tableName;
                bulkCopy.WriteToServer(reader);
            }
            _sqlConnection.Close();

            ResetAttributes();
        }
        #endregion

        #region Reader
        /// <summary>
        /// Finds first model from database. Send anonymous class with TableName property and properties with values you are searching for. 
        /// </summary>
        /// <returns>
        /// Returns instance of T that represents model.
        /// </returns>
        public T? FindOne<T>(object searchClass) where T : class
        {
            searchClass = searchClass ?? throw new ArgumentNullException($"{nameof(searchClass)} is null.");

            // Set _tableName property
            SetTableNameFromProperty(searchClass);
            string skipTableName = searchClass.GetType().GetProperty("TableName")!.Name;

            // Create dictionary from search class. Skip TableName.
            Dictionary<string, dynamic> searchDict = ToDictionary(searchClass, skipTableName);

            // Create command
            SqlCommand sqlSelectCommand = BuildSelectCommand(searchDict);

            // Return T 
            T? findedModel = ReturnInstance<T>(sqlSelectCommand);

            ResetAttributes();
            return findedModel;
        }

        public List<T>? FindAll<T>(object searchClass) where T : class
        {
            searchClass = searchClass ?? throw new ArgumentNullException($"{nameof(searchClass)} is null.");
            // Set _tableName from class.
            SetTableNameFromProperty(searchClass);
            string skipTableName = searchClass.GetType().GetProperty("TableName")!.Name;

            // Create dictionary from search class. Skip TableName.
            Dictionary<string, dynamic> searchDict = ToDictionary(searchClass, skipTableName);

            // Create command
            SqlCommand sqlSelectCommand = BuildSelectCommand(searchDict);

            // Return list of Ts
            List<T>? resultList = ReturnInstancesList<T>(sqlSelectCommand);

            ResetAttributes();
            return resultList;
        }
        #endregion

        /// <summary>
        /// Sets _tableName and _identityColumn properties if exists.
        /// </summary>
        private void SetAttributes(Type type)
        {
            // Obtain TableName attribute value. It cannot be null.
            string? tableName = type.GetCustomAttribute<TableNameAttribute>()?.TableName;
            _tableName = tableName ?? throw new ArgumentNullException($"{nameof(type)}.{nameof(TableNameAttribute)} is null");

            // Obtain IdentityColumn property name. It can be null.
            PropertyInfo? identityProperty = type.GetProperties().SingleOrDefault(p => p.GetCustomAttribute<IdentityColumnAttribute>() is not null);
            _identityColumn = identityProperty?.Name;
        }

        /// <summary>
        /// Set TableName property from passed class.
        /// </summary>
        private void SetTableNameFromProperty(object searchClass)
        {
            Type type = searchClass.GetType();
            PropertyInfo tableNameProperty = type.GetProperty("TableName");

            if (tableNameProperty != null && tableNameProperty.PropertyType == typeof(string))
                _tableName = (string)tableNameProperty.GetValue(searchClass);
            else
                throw new ArgumentNullException("Type does not have a TableName property.");
        }

        /// <summary>
        /// Creates dictionary from passed data. It can be class that have been sent by Reader to find data or by writer to insert data.
        /// </summary>
        private Dictionary<string, dynamic> ToDictionary<T>(T data, string skipString) where T : class
        {
            PropertyInfo[] properties = data.GetType().GetProperties();
            Dictionary<string, dynamic> propertiesDictionary = new Dictionary<string, dynamic>();

            foreach (PropertyInfo property in properties)
            {
                // Skip Identity Column or TableName insertion to dictionary
                if (property.Name == skipString)
                    continue;

                string propertyName = property.Name;
                dynamic? propertyValue = property.GetValue(data);

                propertiesDictionary.Add(propertyName, propertyValue);
            }

            return propertiesDictionary;
        }

        /// <summary>
        /// Creates INSERT INTO SqlCommand.
        /// </summary>
        private SqlCommand BuildInsertCommand(Dictionary<string, dynamic> insertDict)
        {
            if (insertDict.Count == 0) throw new ArgumentNullException($"{nameof(insertDict)} is null");

            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = _sqlConnection;

            StringBuilder stringBuilder = new StringBuilder();

            // Build column list
            List<string> columns = insertDict
                .Select(kvp => kvp.Key)
                .ToList();

            stringBuilder.Append("INSERT INTO ")
                .Append(_tableName)
                .Append(" (")
                .Append(string.Join(",", columns))
                .Append(") VALUES (");

            // Build parameters list
            int index = 0;
            foreach (KeyValuePair<string, dynamic> kvp in insertDict)
            {
                string parameterName = $"@p{index++}";
                stringBuilder.Append(parameterName).Append(",");
                sqlCommand.Parameters.AddWithValue(parameterName, kvp.Value);
            }

            // Remove last comma
            stringBuilder.Remove(stringBuilder.Length - 1, 1);

            stringBuilder.Append(")");

            // Return Id from passed data if IdentityColumnAttribute exists
            if (!string.IsNullOrWhiteSpace(_identityColumn))
                stringBuilder.Append("; SELECT SCOPE_IDENTITY()");

            sqlCommand.CommandText = stringBuilder.ToString();
            return sqlCommand;
        }

        /// <summary>
        /// Creates SELECT * FROM SqlCommand.
        /// </summary>
        private SqlCommand BuildSelectCommand(Dictionary<string, dynamic> searchDict)
        {
            if (searchDict.Count == 0) throw new ArgumentNullException($"{nameof(searchDict)} is null");

            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = _sqlConnection;

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append($"SELECT * FROM {_tableName} WHERE ");

            int index = 0;
            string[] conditions = searchDict
                .Select(kvp => {
                    string parameterName = $"p{index++}";
                    sqlCommand.Parameters.AddWithValue(parameterName, kvp.Value);
                    return $"{kvp.Key} = @{parameterName}";
                })
                .ToArray();

            stringBuilder.AppendJoin(" AND ", conditions);
            stringBuilder.Append(";");

            sqlCommand.CommandText = stringBuilder.ToString();
            return sqlCommand;
        }

        /// <summary>
        /// Returns first finded row of data.
        /// </summary>
        private T ReturnInstance<T>(SqlCommand selectCommand) where T : class
        {
            _sqlConnection.Open();
            using (SqlDataReader reader = selectCommand.ExecuteReader())
            {
                if (!reader.HasRows)
                    return null;

                T result = Activator.CreateInstance<T>();

                if (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string propertyName = reader.GetName(i);
                        PropertyInfo property = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

                        if (property != null && property.CanWrite)
                        {
                            object value = reader.GetValue(i);
                            property.SetValue(result, value);
                        }
                    }
                }

                _sqlConnection.Close();
                return result;
            }
        }


        /// <summary>
        /// Returns list of finded rows of data.
        /// </summary>
        private List<T>? ReturnInstancesList<T>(SqlCommand selectCommand)
        {
            List<T> resultList = new List<T>();
            _sqlConnection.Open();
            using (SqlDataReader reader = selectCommand.ExecuteReader())
            {
                if (!reader.HasRows)
                    return null;

                while (reader.Read())
                {
                    T instance = Activator.CreateInstance<T>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string propertyName = reader.GetName(i);
                        PropertyInfo property = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

                        if (property != null && property.CanWrite)
                        {
                            object value = reader.GetValue(i);
                            property.SetValue(instance, value);
                        }
                    }

                    resultList.Add(instance);
                }

                _sqlConnection.Close();

                return resultList;
            }

        }

        /// <summary>
        /// Set _tableName and _identityColumn to null.
        /// </summary>
        private void ResetAttributes()
        {
            _tableName = string.Empty;
            _identityColumn = null;
        }
    }
}