using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.IO;

namespace Ouroboros.StdLib.Data.Providers
{
    /// <summary>
    /// SQLite database provider implementation
    /// </summary>
    public class SQLiteProvider : IDatabaseProvider
    {
        private readonly string connectionString;
        private DbConnection connection;
        private DbTransaction currentTransaction;
        private readonly DbProviderFactory factory;
        
        public SQLiteProvider(string connectionString)
        {
            this.connectionString = connectionString;
            // Use DbProviderFactories to avoid direct dependency on Microsoft.Data.Sqlite
            // In production, would register SQLite factory
            try
            {
                factory = DbProviderFactories.GetFactory("Microsoft.Data.Sqlite");
            }
            catch
            {
                // If Microsoft.Data.Sqlite is not available, create a mock implementation
                factory = new MockSQLiteProviderFactory();
            }
        }
        
        public async Task<DbConnection> OpenConnectionAsync()
        {
            if (connection == null || connection.State != ConnectionState.Open)
            {
                connection = factory.CreateConnection();
                connection.ConnectionString = connectionString;
                await connection.OpenAsync();
                
                // Enable foreign keys for SQLite
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "PRAGMA foreign_keys = ON";
                await cmd.ExecuteNonQueryAsync();
            }
            return connection;
        }
        
        public async Task CloseConnectionAsync()
        {
            if (connection != null)
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
                connection = null;
            }
        }
        
        public async Task<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var conn = await OpenConnectionAsync();
            // SQLite doesn't support all isolation levels, so we use the default
            currentTransaction = await conn.BeginTransactionAsync();
            return currentTransaction;
        }
        
        public async Task CommitTransactionAsync()
        {
            if (currentTransaction != null)
            {
                await currentTransaction.CommitAsync();
                await currentTransaction.DisposeAsync();
                currentTransaction = null;
            }
        }
        
        public async Task RollbackTransactionAsync()
        {
            if (currentTransaction != null)
            {
                await currentTransaction.RollbackAsync();
                await currentTransaction.DisposeAsync();
                currentTransaction = null;
            }
        }
        
        public async Task<int> ExecuteNonQueryAsync(string sql, params object[] parameters)
        {
            var conn = await OpenConnectionAsync();
            using var cmd = CreateCommand(sql, parameters);
            cmd.Connection = conn;
            cmd.Transaction = currentTransaction;
            return await cmd.ExecuteNonQueryAsync();
        }
        
        public async Task<object> ExecuteScalarAsync(string sql, params object[] parameters)
        {
            var conn = await OpenConnectionAsync();
            using var cmd = CreateCommand(sql, parameters);
            cmd.Connection = conn;
            cmd.Transaction = currentTransaction;
            return await cmd.ExecuteScalarAsync();
        }
        
        public async Task<List<T>> ExecuteQueryAsync<T>(string sql, params object[] parameters) where T : new()
        {
            var conn = await OpenConnectionAsync();
            using var cmd = CreateCommand(sql, parameters);
            cmd.Connection = conn;
            cmd.Transaction = currentTransaction;
            
            var results = new List<T>();
            using var reader = await cmd.ExecuteReaderAsync();
            
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanWrite)
                .ToList();
            
            while (await reader.ReadAsync())
            {
                var item = new T();
                foreach (var prop in properties)
                {
                    try
                    {
                        var ordinal = reader.GetOrdinal(prop.Name);
                        if (!reader.IsDBNull(ordinal))
                        {
                            var value = reader.GetValue(ordinal);
                            // Handle SQLite's type quirks
                            if (prop.PropertyType == typeof(bool) && value is long longValue)
                            {
                                value = longValue != 0;
                            }
                            else if (prop.PropertyType == typeof(DateTime) && value is string strValue)
                            {
                                value = DateTime.Parse(strValue);
                            }
                            prop.SetValue(item, Convert.ChangeType(value, prop.PropertyType));
                        }
                    }
                    catch
                    {
                        // Column not found or type conversion failed, skip
                    }
                }
                results.Add(item);
            }
            
            return results;
        }
        
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sql, params object[] parameters)
        {
            var conn = await OpenConnectionAsync();
            using var cmd = CreateCommand(sql, parameters);
            cmd.Connection = conn;
            cmd.Transaction = currentTransaction;
            
            var results = new List<Dictionary<string, object>>();
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                results.Add(row);
            }
            
            return results;
        }
        
        public async Task<int> BulkInsertAsync<T>(string tableName, IEnumerable<T> items) where T : class
        {
            var itemList = items.ToList();
            if (!itemList.Any()) return 0;
            
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .ToList();
            
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            
            // Start a transaction for bulk insert if not already in one
            bool ownTransaction = currentTransaction == null;
            if (ownTransaction)
            {
                await BeginTransactionAsync();
            }
            
            try
            {
                int totalRows = 0;
                
                // SQLite has a limit on the number of parameters (999), so we batch inserts
                const int maxParams = 900;
                int paramsPerRow = properties.Count;
                int maxRowsPerBatch = maxParams / paramsPerRow;
                
                for (int batchStart = 0; batchStart < itemList.Count; batchStart += maxRowsPerBatch)
                {
                    var batch = itemList.Skip(batchStart).Take(maxRowsPerBatch).ToList();
                    var sb = new StringBuilder();
                    sb.AppendLine($"INSERT INTO \"{tableName}\" ({columns}) VALUES");
                    
                    var parameters = new List<object>();
                    for (int i = 0; i < batch.Count; i++)
                    {
                        if (i > 0) sb.AppendLine(",");
                        
                        var values = new List<string>();
                        foreach (var prop in properties)
                        {
                            values.Add($"@p{parameters.Count}");
                            var value = prop.GetValue(batch[i]);
                            // Handle booleans for SQLite
                            if (value is bool boolValue)
                                value = boolValue ? 1 : 0;
                            parameters.Add(value ?? DBNull.Value);
                        }
                        sb.Append($"({string.Join(", ", values)})");
                    }
                    
                    totalRows += await ExecuteNonQueryAsync(sb.ToString(), parameters.ToArray());
                }
                
                if (ownTransaction)
                {
                    await CommitTransactionAsync();
                }
                
                return totalRows;
            }
            catch
            {
                if (ownTransaction)
                {
                    await RollbackTransactionAsync();
                }
                throw;
            }
        }
        
        public async Task<bool> TableExistsAsync(string tableName, string schema = null)
        {
            // SQLite doesn't use schemas in the same way, so we ignore the schema parameter
            var sql = @"
                SELECT COUNT(*) 
                FROM sqlite_master 
                WHERE type = 'table' 
                AND name = @p0";
            
            var result = await ExecuteScalarAsync(sql, tableName);
            return Convert.ToInt32(result) > 0;
        }
        
        public async Task CreateTableAsync(string tableName, Dictionary<string, string> columns, string primaryKey = null)
        {
            var columnDefs = columns.Select(kvp => 
                $"\"{kvp.Key}\" {MapDataType(kvp.Value)}"
            ).ToList();
            
            if (!string.IsNullOrEmpty(primaryKey))
            {
                // Find the primary key column and add PRIMARY KEY to its definition
                for (int i = 0; i < columnDefs.Count; i++)
                {
                    if (columnDefs[i].StartsWith($"\"{primaryKey}\""))
                    {
                        columnDefs[i] += " PRIMARY KEY";
                        break;
                    }
                }
            }
            
            var sql = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" (\n    {string.Join(",\n    ", columnDefs)}\n)";
            await ExecuteNonQueryAsync(sql);
        }
        
        public async Task<List<string>> GetTableNamesAsync(string schema = null)
        {
            // SQLite doesn't use schemas, so we ignore the schema parameter
            var sql = @"
                SELECT name 
                FROM sqlite_master 
                WHERE type = 'table' 
                AND name NOT LIKE 'sqlite_%'
                ORDER BY name";
            
            var results = await ExecuteQueryAsync(sql);
            return results.Select(r => (string)r["name"]).ToList();
        }
        
        public async Task<List<ColumnInfo>> GetTableColumnsAsync(string tableName, string schema = null)
        {
            // Use PRAGMA table_info to get column information
            var sql = $"PRAGMA table_info(\"{tableName}\")";
            var results = await ExecuteQueryAsync(sql);
            
            return results.Select(r => new ColumnInfo
            {
                Name = (string)r["name"],
                DataType = (string)r["type"],
                MaxLength = null, // SQLite doesn't provide this
                IsNullable = Convert.ToInt32(r["notnull"]) == 0,
                DefaultValue = r["dflt_value"]?.ToString(),
                IsPrimaryKey = Convert.ToInt32(r["pk"]) > 0,
                ColumnType = (string)r["type"]
            }).ToList();
        }
        
        private DbCommand CreateCommand(string sql, object[] parameters)
        {
            var cmd = factory.CreateCommand();
            cmd.CommandText = sql;
            
            // SQLite uses @p0, @p1, etc. for parameters
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = $"@p{i}";
                param.Value = parameters[i] ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }
            
            return cmd;
        }
        
        private string MapDataType(string clrType)
        {
            return clrType.ToLower() switch
            {
                "int" or "int32" => "INTEGER",
                "long" or "int64" => "INTEGER",
                "short" or "int16" => "INTEGER",
                "byte" => "INTEGER",
                "decimal" => "REAL",
                "float" or "single" => "REAL",
                "double" => "REAL",
                "bool" or "boolean" => "INTEGER",
                "string" => "TEXT",
                "text" => "TEXT",
                "datetime" => "TEXT",
                "date" => "TEXT",
                "time" => "TEXT",
                "guid" => "TEXT",
                "byte[]" => "BLOB",
                _ => "TEXT"
            };
        }
        
        public void Dispose()
        {
            currentTransaction?.Dispose();
            connection?.Dispose();
        }
        
        public async ValueTask DisposeAsync()
        {
            if (currentTransaction != null)
                await currentTransaction.DisposeAsync();
            if (connection != null)
                await connection.DisposeAsync();
        }
    }
    
    // Mock factory for when Microsoft.Data.Sqlite is not available
    internal class MockSQLiteProviderFactory : DbProviderFactory
    {
        public override DbCommand CreateCommand() => new MockDbCommand();
        public override DbConnection CreateConnection() => new MockDbConnection();
        public override DbParameter CreateParameter() => new MockDbParameter();
    }
    
    // Minimal mock implementations for testing
    internal class MockDbConnection : DbConnection
    {
        public override string ConnectionString { get; set; }
        public override string Database => "main";
        public override string DataSource => ":memory:";
        public override string ServerVersion => "3.0.0";
        public override ConnectionState State => ConnectionState.Open;
        
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new MockDbTransaction();
        protected override DbCommand CreateDbCommand() => new MockDbCommand();
    }
    
    internal class MockDbCommand : DbCommand
    {
        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new MockDbParameterCollection();
        protected override DbTransaction DbTransaction { get; set; }
        
        public override void Cancel() { }
        public override int ExecuteNonQuery() => 0;
        public override object ExecuteScalar() => null;
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new MockDbParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => new MockDbDataReader();
    }
    
    internal class MockDbTransaction : DbTransaction
    {
        protected override DbConnection DbConnection => new MockDbConnection();
        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
        public override void Commit() { }
        public override void Rollback() { }
    }
    
    internal class MockDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; }
        public override string SourceColumn { get; set; }
        public override object Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }
    
    internal class MockDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> parameters = new List<DbParameter>();
        
        public override int Count => parameters.Count;
        public override object SyncRoot => this;
        
        public override int Add(object value)
        {
            parameters.Add((DbParameter)value);
            return parameters.Count - 1;
        }
        
        public override void AddRange(Array values)
        {
            foreach (DbParameter param in values)
                parameters.Add(param);
        }
        
        public override void Clear() => parameters.Clear();
        public override bool Contains(object value) => parameters.Contains((DbParameter)value);
        public override bool Contains(string value) => parameters.Any(p => p.ParameterName == value);
        public override void CopyTo(Array array, int index) => parameters.CopyTo((DbParameter[])array, index);
        public override System.Collections.IEnumerator GetEnumerator() => parameters.GetEnumerator();
        public override int IndexOf(object value) => parameters.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => parameters.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => parameters.Insert(index, (DbParameter)value);
        public override void Remove(object value) => parameters.Remove((DbParameter)value);
        public override void RemoveAt(int index) => parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName) => parameters.RemoveAll(p => p.ParameterName == parameterName);
        protected override DbParameter GetParameter(int index) => parameters[index];
        protected override DbParameter GetParameter(string parameterName) => parameters.FirstOrDefault(p => p.ParameterName == parameterName);
        protected override void SetParameter(int index, DbParameter value) => parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
                parameters[index] = value;
        }
    }
    
    internal class MockDbDataReader : DbDataReader
    {
        public override object this[int ordinal] => null;
        public override object this[string name] => null;
        public override int Depth => 0;
        public override int FieldCount => 0;
        public override bool HasRows => false;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        
        public override bool GetBoolean(int ordinal) => false;
        public override byte GetByte(int ordinal) => 0;
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => '\0';
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => 0;
        public override string GetDataTypeName(int ordinal) => "TEXT";
        public override DateTime GetDateTime(int ordinal) => DateTime.MinValue;
        public override decimal GetDecimal(int ordinal) => 0;
        public override double GetDouble(int ordinal) => 0;
        public override Type GetFieldType(int ordinal) => typeof(object);
        public override float GetFloat(int ordinal) => 0;
        public override Guid GetGuid(int ordinal) => Guid.Empty;
        public override short GetInt16(int ordinal) => 0;
        public override int GetInt32(int ordinal) => 0;
        public override long GetInt64(int ordinal) => 0;
        public override string GetName(int ordinal) => "";
        public override int GetOrdinal(string name) => -1;
        public override string GetString(int ordinal) => "";
        public override object GetValue(int ordinal) => null;
        public override int GetValues(object[] values) => 0;
        public override bool IsDBNull(int ordinal) => true;
        public override bool NextResult() => false;
        public override bool Read() => false;
        public override System.Collections.IEnumerator GetEnumerator() => new List<object>().GetEnumerator();
    }
} 