using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ouroboros.Stdlib.Data
{
    /// <summary>
    /// Database connection and query interface
    /// </summary>
    public class Database : IDisposable
    {
        private readonly DbConnection connection;
        private readonly DatabaseProvider provider;
        private DbTransaction? currentTransaction;
        private bool disposed;

        public bool IsConnected => connection.State == ConnectionState.Open;
        public DatabaseProvider Provider => provider;

        public Database(string connectionString, DatabaseProvider provider)
        {
            this.provider = provider;
            connection = CreateConnection(connectionString, provider);
        }

        /// <summary>
        /// Open database connection
        /// </summary>
        public async Task OpenAsync()
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
        }

        /// <summary>
        /// Close database connection
        /// </summary>
        public async Task CloseAsync()
        {
            if (connection.State != ConnectionState.Closed)
            {
                await connection.CloseAsync();
            }
        }

        /// <summary>
        /// Execute a query and return results
        /// </summary>
        public async Task<List<T>> QueryAsync<T>(string sql, object? parameters = null) where T : new()
        {
            using var command = CreateCommand(sql, parameters);
            using var reader = await command.ExecuteReaderAsync();
            
            return await MapResultsAsync<T>(reader);
        }

        /// <summary>
        /// Execute a query and return dynamic results
        /// </summary>
        public async Task<List<dynamic>> QueryAsync(string sql, object? parameters = null)
        {
            using var command = CreateCommand(sql, parameters);
            using var reader = await command.ExecuteReaderAsync();
            
            return await MapDynamicResultsAsync(reader);
        }

        /// <summary>
        /// Execute a query and return first result
        /// </summary>
        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null) where T : new()
        {
            var results = await QueryAsync<T>(sql, parameters);
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Execute a scalar query
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
        {
            using var command = CreateCommand(sql, parameters);
            var result = await command.ExecuteScalarAsync();
            
            if (result == null || result == DBNull.Value)
                return default;
                
            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Execute a non-query command
        /// </summary>
        public async Task<int> ExecuteAsync(string sql, object? parameters = null)
        {
            using var command = CreateCommand(sql, parameters);
            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Begin a transaction
        /// </summary>
        public async Task<DatabaseTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            currentTransaction = await connection.BeginTransactionAsync(isolationLevel);
            return new DatabaseTransaction(this, currentTransaction);
        }

        /// <summary>
        /// Create a command builder
        /// </summary>
        public CommandBuilder CreateCommandBuilder()
        {
            return new CommandBuilder(this);
        }

        /// <summary>
        /// Create a query builder
        /// </summary>
        public QueryBuilder Select(params string[] columns)
        {
            return new QueryBuilder(this).Select(columns);
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

        private DbConnection CreateConnection(string connectionString, DatabaseProvider provider)
        {
            return provider switch
            {
                DatabaseProvider.SqlServer => new System.Data.SqlClient.SqlConnection(connectionString),
                DatabaseProvider.PostgreSQL => throw new NotImplementedException("PostgreSQL provider not implemented"),
                DatabaseProvider.MySQL => throw new NotImplementedException("MySQL provider not implemented"),
                DatabaseProvider.SQLite => throw new NotImplementedException("SQLite provider not implemented"),
                _ => throw new NotSupportedException($"Provider {provider} is not supported")
            };
        }

        private DbCommand CreateCommand(string sql, object? parameters)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = currentTransaction;

            if (parameters != null)
            {
                AddParameters(command, parameters);
            }

            return command;
        }

        private void AddParameters(DbCommand command, object parameters)
        {
            var properties = parameters.GetType().GetProperties();
            
            foreach (var property in properties)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{property.Name}";
                parameter.Value = property.GetValue(parameters) ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        private async Task<List<T>> MapResultsAsync<T>(DbDataReader reader) where T : new()
        {
            var results = new List<T>();
            var properties = typeof(T).GetProperties();

            while (await reader.ReadAsync())
            {
                var obj = new T();
                
                foreach (var property in properties)
                {
                    try
                    {
                        var ordinal = reader.GetOrdinal(property.Name);
                        if (!reader.IsDBNull(ordinal))
                        {
                            var value = reader.GetValue(ordinal);
                            property.SetValue(obj, Convert.ChangeType(value, property.PropertyType));
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        // Column doesn't exist in result set
                    }
                }
                
                results.Add(obj);
            }

            return results;
        }

        private async Task<List<dynamic>> MapDynamicResultsAsync(DbDataReader reader)
        {
            var results = new List<dynamic>();

            while (await reader.ReadAsync())
            {
                var obj = new ExpandoObject() as IDictionary<string, object>;
                
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    obj[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                
                results.Add(obj);
            }

            return results;
        }

        internal void ClearTransaction()
        {
            currentTransaction = null;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                currentTransaction?.Dispose();
                connection?.Dispose();
                disposed = true;
            }
        }
    }

    /// <summary>
    /// Database provider enumeration
    /// </summary>
    public enum DatabaseProvider
    {
        SqlServer,
        PostgreSQL,
        MySQL,
        SQLite
    }

    /// <summary>
    /// Database transaction wrapper
    /// </summary>
    public class DatabaseTransaction : IDisposable
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
                await transaction.CommitAsync();
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
                await transaction.RollbackAsync();
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
            database.ClearTransaction();
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
            return database.QueryAsync<T>(ToSql(), parameters);
        }

        public Task<List<dynamic>> QueryAsync()
        {
            return database.QueryAsync(ToSql(), parameters);
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
            var parameters = string.Join(", ", values.Keys.Select(k => $"@{k}"));
            var sql = $"INSERT INTO {table} ({columns}) VALUES ({parameters})";
            
            return await database.ExecuteAsync(sql, values);
        }

        public async Task<T> ExecuteScalarAsync<T>()
        {
            var columns = string.Join(", ", values.Keys);
            var parameters = string.Join(", ", values.Keys.Select(k => $"@{k}"));
            var sql = $"INSERT INTO {table} ({columns}) VALUES ({parameters}); SELECT SCOPE_IDENTITY();";
            
            return await database.ExecuteScalarAsync<T>(sql, values) ?? throw new InvalidOperationException("No identity returned");
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
        private readonly Dictionary<string, object> parameters = new();
        private int parameterIndex = 0;

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
                var paramName = $"wp{parameterIndex++}";
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
            var setClauses = setValues.Select(kv => $"{kv.Key} = @{kv.Key}");
            var sql = $"UPDATE {table} SET {string.Join(", ", setClauses)}";
            
            if (whereConditions.Any())
            {
                sql += $" WHERE {string.Join(" AND ", whereConditions)}";
            }
            
            // Merge all parameters
            foreach (var kv in setValues)
            {
                parameters[kv.Key] = kv.Value;
            }
            
            return await database.ExecuteAsync(sql, parameters);
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
            
            return await database.ExecuteAsync(sql, parameters);
        }
    }
} 