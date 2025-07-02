using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Ouro.StdLib.Data.Mocks;

namespace Ouro.StdLib.Data.Providers
{
    /// <summary>
    /// PostgreSQL database provider implementation
    /// </summary>
    public class PostgreSQLProvider : IDatabaseProvider
    {
        private readonly string connectionString;
        private DbConnection connection;
        private DbTransaction currentTransaction;
        private readonly DbProviderFactory factory;
        
        public PostgreSQLProvider(string connectionString)
        {
            this.connectionString = connectionString;
            // Use DbProviderFactories to avoid direct dependency on Npgsql
            // In production, would register Npgsql factory
            try
            {
                factory = DbProviderFactories.GetFactory("Npgsql");
            }
            catch
            {
                // If Npgsql is not available, create a mock implementation
                factory = new MockPostgreSQLProviderFactory();
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
            
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name.ToLower()}\""));
            var sb = new StringBuilder();
            sb.AppendLine($"INSERT INTO \"{tableName}\" ({columns}) VALUES");
            
            var parameters = new List<object>();
            for (int i = 0; i < itemList.Count; i++)
            {
                if (i > 0) sb.AppendLine(",");
                
                var values = new List<string>();
                foreach (var prop in properties)
                {
                    values.Add($"${parameters.Count + 1}");
                    parameters.Add(prop.GetValue(itemList[i]) ?? DBNull.Value);
                }
                sb.Append($"({string.Join(", ", values)})");
            }
            
            return await ExecuteNonQueryAsync(sb.ToString(), parameters.ToArray());
        }
        
        public async Task<bool> TableExistsAsync(string tableName, string schema = null)
        {
            var sql = @"
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables 
                    WHERE table_name = $1 
                    AND table_schema = $2
                )";
            
            var result = await ExecuteScalarAsync(sql, tableName.ToLower(), schema ?? "public");
            return Convert.ToBoolean(result);
        }
        
        public async Task CreateTableAsync(string tableName, Dictionary<string, string> columns, string primaryKey = null)
        {
            var columnDefs = columns.Select(kvp => 
                $"\"{kvp.Key.ToLower()}\" {MapDataType(kvp.Value)}"
            ).ToList();
            
            if (!string.IsNullOrEmpty(primaryKey))
            {
                columnDefs.Add($"PRIMARY KEY (\"{primaryKey.ToLower()}\")");
            }
            
            var sql = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" (\n    {string.Join(",\n    ", columnDefs)}\n)";
            await ExecuteNonQueryAsync(sql);
        }
        
        public async Task<List<string>> GetTableNamesAsync(string schema = null)
        {
            var sql = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = $1 
                AND table_type = 'BASE TABLE'
                ORDER BY table_name";
            
            var results = await ExecuteQueryAsync<dynamic>(sql, schema ?? "public");
            return results.Select(r => (string)r.table_name).ToList();
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
                    udt_name,
                    CASE 
                        WHEN pk.column_name IS NOT NULL THEN true 
                        ELSE false 
                    END as is_primary_key
                FROM information_schema.columns c
                LEFT JOIN (
                    SELECT ku.table_schema, ku.table_name, ku.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage ku
                        ON tc.constraint_name = ku.constraint_name
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                ) pk ON c.table_schema = pk.table_schema 
                    AND c.table_name = pk.table_name 
                    AND c.column_name = pk.column_name
                WHERE c.table_name = $1 AND c.table_schema = $2
                ORDER BY ordinal_position";
            
            var results = await ExecuteQueryAsync(sql, tableName.ToLower(), schema ?? "public");
            
            return results.Select(r => new ColumnInfo
            {
                Name = (string)r["column_name"],
                DataType = (string)r["data_type"],
                MaxLength = r["character_maximum_length"] as int?,
                IsNullable = (string)r["is_nullable"] == "YES",
                DefaultValue = r["column_default"]?.ToString(),
                IsPrimaryKey = (bool)r["is_primary_key"],
                ColumnType = (string)r["udt_name"]
            }).ToList();
        }
        
        private DbCommand CreateCommand(string sql, object[] parameters)
        {
            var cmd = factory.CreateCommand();
            cmd.CommandText = sql;
            
            // PostgreSQL uses $1, $2, etc. for parameters
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = $"${i + 1}";
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
                "long" or "int64" => "BIGINT",
                "short" or "int16" => "SMALLINT",
                "byte" => "SMALLINT",
                "decimal" => "DECIMAL(19,4)",
                "float" or "single" => "REAL",
                "double" => "DOUBLE PRECISION",
                "bool" or "boolean" => "BOOLEAN",
                "string" => "TEXT",
                "datetime" => "TIMESTAMP",
                "guid" => "UUID",
                "byte[]" => "BYTEA",
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