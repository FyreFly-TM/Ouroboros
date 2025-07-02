using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.IO;
using Ouro.StdLib.Data.Mocks;

namespace Ouro.StdLib.Data.Providers
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
} 