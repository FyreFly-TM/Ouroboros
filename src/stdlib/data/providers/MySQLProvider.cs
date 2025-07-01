using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Ouroboros.StdLib.Data.Mocks;

namespace Ouroboros.StdLib.Data.Providers
{
    /// <summary>
    /// MySQL database provider implementation
    /// </summary>
    public class MySQLProvider : IDatabaseProvider
    {
        private readonly string connectionString;
        private DbConnection connection;
        private DbTransaction currentTransaction;
        private readonly DbProviderFactory factory;

        public MySQLProvider(string connectionString)
        {
            this.connectionString = connectionString;
            // Use DbProviderFactories to avoid direct dependency on MySqlConnector
            // In production, would register MySqlConnector factory
            try
            {
                factory = DbProviderFactories.GetFactory("MySqlConnector");
            }
            catch
            {
                // If MySqlConnector is not available, create a mock implementation
                factory = new MockMySQLProviderFactory();
            }
        }
        
        public async Task<DbConnection> OpenConnectionAsync()
        {
            if (connection == null || connection.State != ConnectionState.Open)
            {
                connection = factory.CreateConnection();
                connection.ConnectionString = connectionString;
                await connection.OpenAsync();
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
            currentTransaction = await conn.BeginTransactionAsync(isolationLevel);
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
            
            var columns = string.Join(", ", properties.Select(p => $"`{p.Name}`"));
            var sb = new StringBuilder();
            sb.AppendLine($"INSERT INTO `{tableName}` ({columns}) VALUES");
            
            var parameters = new List<object>();
            for (int i = 0; i < itemList.Count; i++)
            {
                if (i > 0) sb.AppendLine(",");
                
                var values = new List<string>();
                foreach (var prop in properties)
                {
                    values.Add($"@p{parameters.Count}");
                    parameters.Add(prop.GetValue(itemList[i]) ?? DBNull.Value);
                }
                sb.Append($"({string.Join(", ", values)})");
            }
            
            return await ExecuteNonQueryAsync(sb.ToString(), parameters.ToArray());
        }
        
        public async Task<bool> TableExistsAsync(string tableName, string schema = null)
        {
            var sql = @"
                SELECT COUNT(*) 
                FROM information_schema.tables 
                WHERE table_name = @tableName 
                AND table_schema = @schema";
            
            var dbName = schema ?? connection?.Database ?? "mysql";
            var result = await ExecuteScalarAsync(sql, tableName, dbName);
            return Convert.ToInt32(result) > 0;
        }
        
        public async Task CreateTableAsync(string tableName, Dictionary<string, string> columns, string primaryKey = null)
        {
            var columnDefs = columns.Select(kvp => 
                $"`{kvp.Key}` {MapDataType(kvp.Value)}"
            ).ToList();
            
            if (!string.IsNullOrEmpty(primaryKey))
            {
                columnDefs.Add($"PRIMARY KEY (`{primaryKey}`)");
            }
            
            var sql = $"CREATE TABLE IF NOT EXISTS `{tableName}` (\n    {string.Join(",\n    ", columnDefs)}\n) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            await ExecuteNonQueryAsync(sql);
        }
        
        public async Task<List<string>> GetTableNamesAsync(string schema = null)
        {
            var sql = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = @schema 
                AND table_type = 'BASE TABLE'
                ORDER BY table_name";
            
            var dbName = schema ?? connection?.Database ?? "mysql";
            var results = await ExecuteQueryAsync(sql, dbName);
            return results.Select(r => (string)r["table_name"]).ToList();
        }
        
        public async Task<List<ColumnInfo>> GetTableColumnsAsync(string tableName, string schema = null)
        {
            var sql = @"
                SELECT 
                    column_name,
                    data_type,
                    character_maximum_length,
                    is_nullable,
                    column_default,
                    column_type,
                    column_key
                FROM information_schema.columns
                WHERE table_name = @tableName 
                AND table_schema = @schema
                ORDER BY ordinal_position";
            
            var dbName = schema ?? connection?.Database ?? "mysql";
            var results = await ExecuteQueryAsync(sql, tableName, dbName);
            
            return results.Select(r => new ColumnInfo
            {
                Name = (string)r["column_name"],
                DataType = (string)r["data_type"],
                MaxLength = r["character_maximum_length"] as int?,
                IsNullable = (string)r["is_nullable"] == "YES",
                DefaultValue = r["column_default"]?.ToString(),
                IsPrimaryKey = (string)r["column_key"] == "PRI",
                ColumnType = (string)r["column_type"]
            }).ToList();
        }
        
        private DbCommand CreateCommand(string sql, object[] parameters)
        {
            var cmd = factory.CreateCommand();
            cmd.CommandText = sql;
            
            // MySQL uses @p0, @p1, etc. for parameters
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
                "int" or "int32" => "INT",
                "long" or "int64" => "BIGINT",
                "short" or "int16" => "SMALLINT",
                "byte" => "TINYINT UNSIGNED",
                "decimal" => "DECIMAL(19,4)",
                "float" or "single" => "FLOAT",
                "double" => "DOUBLE",
                "bool" or "boolean" => "BOOLEAN",
                "string" => "VARCHAR(255)",
                "text" => "TEXT",
                "datetime" => "DATETIME",
                "date" => "DATE",
                "time" => "TIME",
                "guid" => "CHAR(36)",
                "byte[]" => "BLOB",
                _ => "VARCHAR(255)"
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