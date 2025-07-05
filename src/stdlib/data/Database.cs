using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ouro.StdLib.Data
{
    /// <summary>
    /// High-level database abstraction supporting multiple providers
    /// </summary>
    public class Database : IDisposable, IAsyncDisposable
    {
        private readonly IDatabaseProvider provider;
        private readonly string connectionString;
        private bool disposed;

        public DatabaseProvider ProviderType { get; }

        public Database(string connectionString, DatabaseProvider providerType)
        {
            this.connectionString = connectionString;
            this.ProviderType = providerType;
            this.provider = CreateProvider(connectionString, providerType);
        }

        private static IDatabaseProvider CreateProvider(string connectionString, DatabaseProvider providerType)
        {
            return providerType switch
            {
                DatabaseProvider.PostgreSQL => new Providers.PostgreSQLProvider(connectionString),
                DatabaseProvider.MySQL => new Providers.MySQLProvider(connectionString),
                DatabaseProvider.SQLite => new Providers.SQLiteProvider(connectionString),
                _ => throw new ArgumentException($"Unsupported database provider: {providerType}")
            };
        }

        /// <summary>
        /// Open database connection
        /// </summary>
        public async Task<DbConnection> OpenAsync()
        {
            return await provider.OpenConnectionAsync();
        }

        /// <summary>
        /// Close database connection
        /// </summary>
        public async Task CloseAsync()
        {
            await provider.CloseConnectionAsync();
        }

        /// <summary>
        /// Execute a query and return typed results
        /// </summary>
        public async Task<List<T>> QueryAsync<T>(string sql, params object[] parameters) where T : new()
        {
            return await provider.ExecuteQueryAsync<T>(sql, parameters);
        }

        /// <summary>
        /// Execute a query and return dynamic results
        /// </summary>
        public async Task<List<dynamic>> QueryAsync(string sql, params object[] parameters)
        {
            var results = await provider.ExecuteQueryAsync(sql, parameters);
            return results.Select(static dict => 
            {
                var expando = new ExpandoObject() as IDictionary<string, object>;
                foreach (var kvp in dict)
                {
                    expando[kvp.Key] = kvp.Value;
                }
                return (dynamic)expando;
            }).ToList();
        }

        /// <summary>
        /// Execute a query and return the first result or default
        /// </summary>
        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, params object[] parameters) where T : new()
        {
            var results = await QueryAsync<T>(sql, parameters);
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Execute a scalar query
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(string sql, params object[] parameters)
        {
            var result = await provider.ExecuteScalarAsync(sql, parameters);
            if (result == null || result == DBNull.Value)
                return default;
            
            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Execute a non-query command
        /// </summary>
        public async Task<int> ExecuteAsync(string sql, params object[] parameters)
        {
            return await provider.ExecuteNonQueryAsync(sql, parameters);
        }

        /// <summary>
        /// Begin a database transaction
        /// </summary>
        public async Task<DatabaseTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var transaction = await provider.BeginTransactionAsync(isolationLevel);
            return new DatabaseTransaction(this, transaction);
        }

        /// <summary>
        /// Bulk insert multiple items
        /// </summary>
        public async Task<int> BulkInsertAsync<T>(string tableName, IEnumerable<T> items) where T : class
        {
            return await provider.BulkInsertAsync(tableName, items);
        }

        /// <summary>
        /// Check if a table exists
        /// </summary>
        public async Task<bool> TableExistsAsync(string tableName, string? schema = null)
        {
            return await provider.TableExistsAsync(tableName, schema ?? string.Empty);
        }

        /// <summary>
        /// Create a table
        /// </summary>
        public async Task CreateTableAsync(string tableName, Dictionary<string, string> columns, string? primaryKey = null)
        {
            await provider.CreateTableAsync(tableName, columns, primaryKey ?? string.Empty);
        }

        /// <summary>
        /// Get all table names
        /// </summary>
        public async Task<List<string>> GetTableNamesAsync(string? schema = null)
        {
            return await provider.GetTableNamesAsync(schema ?? string.Empty);
        }

        /// <summary>
        /// Get table column information
        /// </summary>
        public async Task<List<ColumnInfo>> GetTableColumnsAsync(string tableName, string? schema = null)
        {
            return await provider.GetTableColumnsAsync(tableName, schema ?? string.Empty);
        }

        /// <summary>
        /// Create a fluent query builder
        /// </summary>
        public QueryBuilder Select(params string[] columns)
        {
            var builder = new QueryBuilder(this);
            return builder.Select(columns);
        }

        /// <summary>
        /// Create an insert builder
        /// </summary>
        public InsertBuilder InsertInto(string table)
        {
            return new InsertBuilder(this, table);
        }

        /// <summary>
        /// Create an update builder
        /// </summary>
        public UpdateBuilder Update(string table)
        {
            return new UpdateBuilder(this, table);
        }

        /// <summary>
        /// Create a delete builder
        /// </summary>
        public DeleteBuilder DeleteFrom(string table)
        {
            return new DeleteBuilder(this, table);
        }

        internal async Task CommitTransactionAsync()
        {
            await provider.CommitTransactionAsync();
        }

        internal async Task RollbackTransactionAsync()
        {
            await provider.RollbackTransactionAsync();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                provider?.Dispose();
                disposed = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                if (provider != null)
                    await provider.DisposeAsync();
                disposed = true;
            }
        }
    }

    /// <summary>
    /// Database provider enumeration
    /// </summary>
    public enum DatabaseProvider
    {
        PostgreSQL,
        MySQL,
        SQLite
    }

    /// <summary>
    /// Database transaction wrapper
    /// </summary>
    public class DatabaseTransaction : IDisposable, IAsyncDisposable
    {
        private readonly Database database;
        private readonly DbTransaction transaction;
        private bool completed;

        internal DatabaseTransaction(Database database, DbTransaction transaction)
        {
            this.database = database;
            this.transaction = transaction;
        }

        /// <summary>
        /// Commit the transaction
        /// </summary>
        public async Task CommitAsync()
        {
            if (!completed)
            {
                await database.CommitTransactionAsync();
                completed = true;
            }
        }

        /// <summary>
        /// Rollback the transaction
        /// </summary>
        public async Task RollbackAsync()
        {
            if (!completed)
            {
                await database.RollbackTransactionAsync();
                completed = true;
            }
        }

        public void Dispose()
        {
            if (!completed)
            {
                transaction.Rollback();
            }
            transaction.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (!completed)
            {
                await transaction.RollbackAsync();
            }
            await transaction.DisposeAsync();
        }
    }

    /// <summary>
    /// SQL command builder
    /// </summary>
    public class CommandBuilder
    {
        private readonly Database database;
        private readonly StringBuilder sql = new();
        private readonly Dictionary<string, object> parameters = new();

        internal CommandBuilder(Database database)
        {
            this.database = database;
        }

        public CommandBuilder Append(string text)
        {
            sql.Append(text);
            return this;
        }

        public CommandBuilder AppendLine(string text)
        {
            sql.AppendLine(text);
            return this;
        }

        public CommandBuilder AddParameter(string name, object value)
        {
            parameters[name] = value;
            return this;
        }

        public Task<List<T>> QueryAsync<T>() where T : new()
        {
            return database.QueryAsync<T>(sql.ToString(), parameters);
        }

        public Task<int> ExecuteAsync()
        {
            return database.ExecuteAsync(sql.ToString(), parameters);
        }
    }

    /// <summary>
    /// Fluent query builder
    /// </summary>
    public class QueryBuilder
    {
        private readonly Database database;
        private readonly List<string> selectColumns = new();
        private string? fromTable;
        private readonly List<string> joins = new();
        private readonly List<string> whereConditions = new();
        private readonly List<string> groupByColumns = new();
        private readonly List<string> havingConditions = new();
        private readonly List<string> orderByColumns = new();
        private int? limitCount;
        private int? offsetCount;
        private readonly Dictionary<string, object> parameters = new();
        private int parameterIndex = 0;

        internal QueryBuilder(Database database)
        {
            this.database = database;
        }

        public QueryBuilder Select(params string[] columns)
        {
            selectColumns.AddRange(columns);
            return this;
        }

        public QueryBuilder From(string table)
        {
            fromTable = table;
            return this;
        }

        public QueryBuilder InnerJoin(string table, string condition)
        {
            joins.Add($"INNER JOIN {table} ON {condition}");
            return this;
        }

        public QueryBuilder LeftJoin(string table, string condition)
        {
            joins.Add($"LEFT JOIN {table} ON {condition}");
            return this;
        }

        public QueryBuilder Where(string condition, object? value = null)
        {
            if (value != null)
            {
                var paramName = $"p{parameterIndex++}";
                whereConditions.Add(condition.Replace("?", $"@{paramName}"));
                parameters[paramName] = value;
            }
            else
            {
                whereConditions.Add(condition);
            }
            return this;
        }

        public QueryBuilder GroupBy(params string[] columns)
        {
            groupByColumns.AddRange(columns);
            return this;
        }

        public QueryBuilder Having(string condition)
        {
            havingConditions.Add(condition);
            return this;
        }

        public QueryBuilder OrderBy(string column, bool descending = false)
        {
            orderByColumns.Add($"{column} {(descending ? "DESC" : "ASC")}");
            return this;
        }

        public QueryBuilder Limit(int count)
        {
            limitCount = count;
            return this;
        }

        public QueryBuilder Offset(int count)
        {
            offsetCount = count;
            return this;
        }

        public string ToSql()
        {
            var sql = new StringBuilder();
            
            // SELECT
            sql.Append("SELECT ");
            sql.Append(selectColumns.Any() ? string.Join(", ", selectColumns) : "*");
            
            // FROM
            if (!string.IsNullOrEmpty(fromTable))
            {
                sql.Append($" FROM {fromTable}");
            }
            
            // JOINS
            foreach (var join in joins)
            {
                sql.Append($" {join}");
            }
            
            // WHERE
            if (whereConditions.Any())
            {
                sql.Append($" WHERE {string.Join(" AND ", whereConditions)}");
            }
            
            // GROUP BY
            if (groupByColumns.Any())
            {
                sql.Append($" GROUP BY {string.Join(", ", groupByColumns)}");
            }
            
            // HAVING
            if (havingConditions.Any())
            {
                sql.Append($" HAVING {string.Join(" AND ", havingConditions)}");
            }
            
            // ORDER BY
            if (orderByColumns.Any())
            {
                sql.Append($" ORDER BY {string.Join(", ", orderByColumns)}");
            }
            
            // LIMIT/OFFSET
            if (limitCount.HasValue)
            {
                sql.Append($" LIMIT {limitCount}");
            }
            
            if (offsetCount.HasValue)
            {
                sql.Append($" OFFSET {offsetCount}");
            }

            return sql.ToString();
        }

        public Task<List<T>> QueryAsync<T>() where T : new()
        {
            var paramArray = parameters.Values.ToArray();
            return database.QueryAsync<T>(ToSql(), paramArray);
        }

        public Task<List<dynamic>> QueryAsync()
        {
            var paramArray = parameters.Values.ToArray();
            return database.QueryAsync(ToSql(), paramArray);
        }
    }

    /// <summary>
    /// Insert builder
    /// </summary>
    public class InsertBuilder
    {
        private readonly Database database;
        private readonly string table;
        private readonly Dictionary<string, object> values = new();

        internal InsertBuilder(Database database, string table)
        {
            this.database = database;
            this.table = table;
        }

        public InsertBuilder Value(string column, object value)
        {
            values[column] = value;
            return this;
        }

        public InsertBuilder Values(object valueObject)
        {
            var properties = valueObject.GetType().GetProperties();
            foreach (var property in properties)
            {
                values[property.Name] = property.GetValue(valueObject)!;
            }
            return this;
        }

        public async Task<int> ExecuteAsync()
        {
            var columns = string.Join(", ", values.Keys);
            var paramNames = values.Keys.Select(static (k, i) => $"@{i}").ToList();
            var sql = $"INSERT INTO {table} ({columns}) VALUES ({string.Join(", ", paramNames)})";
            
            return await database.ExecuteAsync(sql, values.Values.ToArray());
        }

        public async Task<T> ExecuteScalarAsync<T>()
        {
            var columns = string.Join(", ", values.Keys);
            var paramNames = values.Keys.Select(static (k, i) => $"@{i}").ToList();
            
            // Provider-specific returning clause
            string sql = database.ProviderType switch
            {
                DatabaseProvider.PostgreSQL => $"INSERT INTO {table} ({columns}) VALUES ({string.Join(", ", paramNames)}) RETURNING id",
                DatabaseProvider.MySQL => $"INSERT INTO {table} ({columns}) VALUES ({string.Join(", ", paramNames)}); SELECT LAST_INSERT_ID()",
                DatabaseProvider.SQLite => $"INSERT INTO {table} ({columns}) VALUES ({string.Join(", ", paramNames)}); SELECT last_insert_rowid()",
                _ => throw new NotSupportedException($"Provider {database.ProviderType} does not support returning inserted ID")
            };
            
            return await database.ExecuteScalarAsync<T>(sql, values.Values.ToArray()) ?? throw new InvalidOperationException("No identity returned");
        }
    }

    /// <summary>
    /// Update builder
    /// </summary>
    public class UpdateBuilder
    {
        private readonly Database database;
        private readonly string table;
        private readonly Dictionary<string, object> setValues = new();
        private readonly List<string> whereConditions = new();
        private readonly List<object> whereParams = new();

        internal UpdateBuilder(Database database, string table)
        {
            this.database = database;
            this.table = table;
        }

        public UpdateBuilder Set(string column, object value)
        {
            setValues[column] = value;
            return this;
        }

        public UpdateBuilder Where(string condition, object? value = null)
        {
            if (value != null)
            {
                var paramIndex = setValues.Count + whereParams.Count;
                whereConditions.Add(condition.Replace("?", $"@{paramIndex}"));
                whereParams.Add(value);
            }
            else
            {
                whereConditions.Add(condition);
            }
            return this;
        }

        public async Task<int> ExecuteAsync()
        {
            var setClauses = setValues.Select(static (kv, i) => $"{kv.Key} = @{i}");
            var sql = $"UPDATE {table} SET {string.Join(", ", setClauses)}";
            
            if (whereConditions.Any())
            {
                sql += $" WHERE {string.Join(" AND ", whereConditions)}";
            }
            
            var allParams = setValues.Values.Concat(whereParams).ToArray();
            return await database.ExecuteAsync(sql, allParams);
        }
    }

    /// <summary>
    /// Delete builder
    /// </summary>
    public class DeleteBuilder
    {
        private readonly Database database;
        private readonly string table;
        private readonly List<string> whereConditions = new();
        private readonly Dictionary<string, object> parameters = new();
        private int parameterIndex = 0;

        internal DeleteBuilder(Database database, string table)
        {
            this.database = database;
            this.table = table;
        }

        public DeleteBuilder Where(string condition, object? value = null)
        {
            if (value != null)
            {
                var paramName = $"p{parameterIndex++}";
                whereConditions.Add(condition.Replace("?", $"@{paramName}"));
                parameters[paramName] = value;
            }
            else
            {
                whereConditions.Add(condition);
            }
            return this;
        }

        public async Task<int> ExecuteAsync()
        {
            var sql = $"DELETE FROM {table}";
            
            if (whereConditions.Any())
            {
                sql += $" WHERE {string.Join(" AND ", whereConditions)}";
            }
            
            return await database.ExecuteAsync(sql, parameters.ToArray());
        }
    }

} 