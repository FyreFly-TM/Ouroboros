using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ouroboros.StdLib.Data;

namespace Ouroboros.Stdlib.Data
{
    /// <summary>
    /// ORM context for database operations
    /// </summary>
    public class OrmContext : IDisposable
    {
        private readonly Database database;
        private readonly Dictionary<Type, TableMapping> tableMappings = new();
        internal readonly List<object> trackedEntities = new();
        internal readonly Dictionary<object, EntityState> entityStates = new();

        public Database Database => database;

        public OrmContext(Database database)
        {
            this.database = database;
        }

        /// <summary>
        /// Get a queryable set for an entity type
        /// </summary>
        public DbSet<T> Set<T>() where T : class, new()
        {
            EnsureTableMapping<T>();
            return new DbSet<T>(this);
        }

        /// <summary>
        /// Add an entity to be inserted
        /// </summary>
        public void Add<T>(T entity) where T : class
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            if (!entityStates.ContainsKey(entity))
            {
                trackedEntities.Add(entity);
                entityStates[entity] = EntityState.Added;
            }
        }

        /// <summary>
        /// Update an entity
        /// </summary>
        public void Update<T>(T entity) where T : class
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            if (!entityStates.ContainsKey(entity))
            {
                trackedEntities.Add(entity);
                entityStates[entity] = EntityState.Modified;
            }
            else if (entityStates[entity] == EntityState.Unchanged)
            {
                entityStates[entity] = EntityState.Modified;
            }
        }

        /// <summary>
        /// Delete an entity
        /// </summary>
        public void Delete<T>(T entity) where T : class
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            if (entityStates.ContainsKey(entity))
            {
                if (entityStates[entity] == EntityState.Added)
                {
                    // Remove from tracking if it was just added
                    trackedEntities.Remove(entity);
                    entityStates.Remove(entity);
                }
                else
                {
                    entityStates[entity] = EntityState.Deleted;
                }
            }
            else
            {
                trackedEntities.Add(entity);
                entityStates[entity] = EntityState.Deleted;
            }
        }

        /// <summary>
        /// Save all changes to the database
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            int affectedRows = 0;
            
            using var transaction = await database.BeginTransactionAsync();
            try
            {
                foreach (var entity in trackedEntities.ToList())
                {
                    var state = entityStates[entity];
                    var mapping = GetTableMapping(entity.GetType());
                    
                    switch (state)
                    {
                        case EntityState.Added:
                            affectedRows += await InsertEntityAsync(entity, mapping);
                            break;
                        case EntityState.Modified:
                            affectedRows += await UpdateEntityAsync(entity, mapping);
                            break;
                        case EntityState.Deleted:
                            affectedRows += await DeleteEntityAsync(entity, mapping);
                            break;
                    }
                    
                    // Update state after successful operation
                    if (state == EntityState.Deleted)
                    {
                        trackedEntities.Remove(entity);
                        entityStates.Remove(entity);
                    }
                    else
                    {
                        entityStates[entity] = EntityState.Unchanged;
                    }
                }
                
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            
            return affectedRows;
        }

        /// <summary>
        /// Save all changes to the database (synchronous)
        /// </summary>
        public int SaveChanges()
        {
            // Synchronous wrapper around async method with careful handling
            // Use ConfigureAwait(false) to avoid deadlocks
            var task = SaveChangesAsync();
            task.ConfigureAwait(false);
            return task.GetAwaiter().GetResult();
        }

        private async Task<int> InsertEntityAsync(object entity, TableMapping mapping)
        {
            var columns = new List<string>();
            var values = new Dictionary<string, object>();
            
            foreach (var column in mapping.Columns.Where(c => !c.IsIdentity))
            {
                columns.Add(column.ColumnName);
                values[column.ColumnName] = column.Property.GetValue(entity) ?? DBNull.Value;
            }
            
            var sql = $"INSERT INTO {mapping.TableName} ({string.Join(", ", columns)}) " +
                     $"VALUES ({string.Join(", ", columns.Select(c => $"@{c}"))})";
            
            var result = await database.ExecuteAsync(sql, values);
            
            // Get identity value if present
            var identityColumn = mapping.Columns.FirstOrDefault(c => c.IsIdentity);
            if (identityColumn != null)
            {
                // For SQLite, use last_insert_rowid()
                var identity = await database.ExecuteScalarAsync<long>("SELECT last_insert_rowid()");
                if (identity != null && identity != default(long))
                {
                    identityColumn.Property.SetValue(entity, Convert.ChangeType(identity, identityColumn.Property.PropertyType));
                }
            }
            
            return result;
        }

        private async Task<int> UpdateEntityAsync(object entity, TableMapping mapping)
        {
            var setClauses = new List<string>();
            var values = new Dictionary<string, object>();
            
            foreach (var column in mapping.Columns.Where(c => !c.IsPrimaryKey))
            {
                setClauses.Add($"{column.ColumnName} = @{column.ColumnName}");
                values[column.ColumnName] = column.Property.GetValue(entity) ?? DBNull.Value;
            }
            
            var whereClause = BuildPrimaryKeyWhereClause(entity, mapping, values);
            var sql = $"UPDATE {mapping.TableName} SET {string.Join(", ", setClauses)} WHERE {whereClause}";
            
            return await database.ExecuteAsync(sql, values);
        }

        private async Task<int> DeleteEntityAsync(object entity, TableMapping mapping)
        {
            var values = new Dictionary<string, object>();
            var whereClause = BuildPrimaryKeyWhereClause(entity, mapping, values);
            var sql = $"DELETE FROM {mapping.TableName} WHERE {whereClause}";
            
            return await database.ExecuteAsync(sql, values);
        }

        private string BuildPrimaryKeyWhereClause(object entity, TableMapping mapping, Dictionary<string, object> values)
        {
            var conditions = new List<string>();
            
            foreach (var pkColumn in mapping.Columns.Where(c => c.IsPrimaryKey))
            {
                var paramName = $"pk_{pkColumn.ColumnName}";
                conditions.Add($"{pkColumn.ColumnName} = @{paramName}");
                values[paramName] = pkColumn.Property.GetValue(entity) ?? throw new InvalidOperationException($"Primary key {pkColumn.ColumnName} is null");
            }
            
            return string.Join(" AND ", conditions);
        }

        internal TableMapping GetTableMapping<T>()
        {
            return GetTableMapping(typeof(T));
        }

        internal TableMapping GetTableMapping(Type type)
        {
            if (!tableMappings.TryGetValue(type, out var mapping))
            {
                mapping = CreateTableMapping(type);
                tableMappings[type] = mapping;
            }
            return mapping;
        }

        private void EnsureTableMapping<T>()
        {
            GetTableMapping<T>();
        }

        private TableMapping CreateTableMapping(Type type)
        {
            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            var tableName = tableAttr?.Name ?? type.Name + "s"; // Simple pluralization
            
            var mapping = new TableMapping
            {
                EntityType = type,
                TableName = tableName,
                Columns = new List<ColumnMapping>()
            };
            
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<NotMappedAttribute>() != null)
                    continue;
                    
                var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
                var keyAttr = property.GetCustomAttribute<KeyAttribute>();
                var identityAttr = property.GetCustomAttribute<DatabaseGeneratedAttribute>();
                
                var columnMapping = new ColumnMapping
                {
                    Property = property,
                    ColumnName = columnAttr?.Name ?? property.Name,
                    IsPrimaryKey = keyAttr != null,
                    IsIdentity = identityAttr?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity
                };
                
                mapping.Columns.Add(columnMapping);
            }
            
            return mapping;
        }

        public void Dispose()
        {
            // Context doesn't own the database connection
        }
    }

    /// <summary>
    /// Entity state enumeration
    /// </summary>
    public enum EntityState
    {
        Unchanged,
        Added,
        Modified,
        Deleted
    }

    /// <summary>
    /// Table mapping information
    /// </summary>
    internal class TableMapping
    {
        public Type EntityType { get; set; } = null!;
        public string TableName { get; set; } = "";
        public List<ColumnMapping> Columns { get; set; } = new();
    }

    /// <summary>
    /// Column mapping information
    /// </summary>
    internal class ColumnMapping
    {
        public PropertyInfo Property { get; set; } = null!;
        public string ColumnName { get; set; } = "";
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
    }

    /// <summary>
    /// Queryable database set
    /// </summary>
    public class DbSet<T> where T : class, new()
    {
        private readonly OrmContext context;
        private readonly TableMapping mapping;

        internal DbSet(OrmContext context)
        {
            this.context = context;
            this.mapping = context.GetTableMapping<T>();
        }

        /// <summary>
        /// Add an entity
        /// </summary>
        public void Add(T entity)
        {
            context.Add(entity);
        }

        /// <summary>
        /// Update an entity
        /// </summary>
        public void Update(T entity)
        {
            context.Update(entity);
        }

        /// <summary>
        /// Delete an entity
        /// </summary>
        public void Delete(T entity)
        {
            context.Delete(entity);
        }

        /// <summary>
        /// Find entity by primary key
        /// </summary>
        public async Task<T?> FindAsync(params object[] keyValues)
        {
            var pkColumns = mapping.Columns.Where(c => c.IsPrimaryKey).ToList();
            
            if (pkColumns.Count != keyValues.Length)
                throw new ArgumentException("Number of key values doesn't match primary key columns");
            
            var whereConditions = new List<string>();
            var parameters = new Dictionary<string, object>();
            
            for (int i = 0; i < pkColumns.Count; i++)
            {
                var column = pkColumns[i];
                var paramName = $"pk{i}";
                whereConditions.Add($"{column.ColumnName} = @{paramName}");
                parameters[paramName] = keyValues[i];
            }
            
            var sql = $"SELECT * FROM {mapping.TableName} WHERE {string.Join(" AND ", whereConditions)}";
            var results = await context.Database.QueryAsync<T>(sql, parameters);
            
            var entity = results.FirstOrDefault();
            if (entity != null)
            {
                context.entityStates[entity] = EntityState.Unchanged;
                context.trackedEntities.Add(entity);
            }
            
            return entity;
        }

        /// <summary>
        /// Find entity by primary key (synchronous)
        /// </summary>
        public T? Find(params object[] keyValues)
        {
            return FindAsync(keyValues).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get all entities
        /// </summary>
        public async Task<List<T>> ToListAsync()
        {
            var sql = $"SELECT * FROM {mapping.TableName}";
            var results = await context.Database.QueryAsync<T>(sql);
            
            // Track loaded entities
            foreach (var entity in results)
            {
                if (!context.entityStates.ContainsKey(entity))
                {
                    context.entityStates[entity] = EntityState.Unchanged;
                    context.trackedEntities.Add(entity);
                }
            }
            
            return results;
        }

        /// <summary>
        /// Get all entities (synchronous)
        /// </summary>
        public List<T> ToList()
        {
            // Synchronous wrapper with proper deadlock avoidance
            var task = ToListAsync();
            task.ConfigureAwait(false);
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Create a LINQ query provider
        /// </summary>
        public IQueryable<T> AsQueryable()
        {
            return new OrmQueryable<T>(new OrmQueryProvider(context, mapping));
        }

        /// <summary>
        /// Where clause helper
        /// </summary>
        public async Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate)
        {
            var visitor = new WhereExpressionVisitor();
            visitor.Visit(predicate.Body);
            
            var sql = $"SELECT * FROM {mapping.TableName} WHERE {visitor.WhereClause}";
            var results = await context.Database.QueryAsync<T>(sql, visitor.Parameters);
            
            // Track loaded entities
            foreach (var entity in results)
            {
                if (!context.entityStates.ContainsKey(entity))
                {
                    context.entityStates[entity] = EntityState.Unchanged;
                    context.trackedEntities.Add(entity);
                }
            }
            
            return results;
        }

        /// <summary>
        /// Where clause helper (synchronous)
        /// </summary>
        public List<T> Where(Expression<Func<T, bool>> predicate)
        {
            return WhereAsync(predicate).GetAwaiter().GetResult();
        }


    }

    /// <summary>
    /// LINQ query provider for ORM
    /// </summary>
    internal class OrmQueryProvider : IQueryProvider
    {
        private readonly OrmContext context;
        private readonly TableMapping mapping;

        public OrmQueryProvider(OrmContext context, TableMapping mapping)
        {
            this.context = context;
            this.mapping = mapping;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetElementType() ?? throw new InvalidOperationException();
            return (IQueryable)Activator.CreateInstance(
                typeof(OrmQueryable<>).MakeGenericType(elementType),
                this, expression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new OrmQueryable<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            // Extract method call expression and execute asynchronously
            var methodCall = expression as MethodCallExpression;
            if (methodCall != null)
            {
                var method = methodCall.Method;
                if (method.Name == "Where")
                {
                    // Execute Where clause
                    var lambda = methodCall.Arguments[1] as LambdaExpression;
                    if (lambda != null)
                    {
                        var visitor = new WhereExpressionVisitor();
                        visitor.Visit(lambda.Body);
                        
                        var sql = $"SELECT * FROM {mapping.TableName} WHERE {visitor.WhereClause}";
                        var task = context.Database.QueryAsync<object>(sql, visitor.Parameters);
                        task.ConfigureAwait(false);
                        return task.GetAwaiter().GetResult();
                    }
                }
                else if (method.Name == "Count")
                {
                    var sql = $"SELECT COUNT(*) FROM {mapping.TableName}";
                    var task = context.Database.ExecuteScalarAsync<long>(sql);
                    task.ConfigureAwait(false);
                    return task.GetAwaiter().GetResult();
                }
            }
            
            // Default: execute as SELECT *
            var defaultSql = $"SELECT * FROM {mapping.TableName}";
            var defaultTask = context.Database.QueryAsync<object>(defaultSql);
            defaultTask.ConfigureAwait(false);
            return defaultTask.GetAwaiter().GetResult();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            // Use the non-generic version and cast
            var result = Execute(expression);
            return (TResult)result;
        }
    }

    /// <summary>
    /// Queryable implementation for ORM
    /// </summary>
    internal class OrmQueryable<T> : IQueryable<T>
    {
        private readonly IQueryProvider provider;
        private readonly Expression expression;

        public OrmQueryable(IQueryProvider provider)
        {
            this.provider = provider;
            this.expression = Expression.Constant(this);
        }

        public OrmQueryable(IQueryProvider provider, Expression expression)
        {
            this.provider = provider;
            this.expression = expression;
        }

        public Type ElementType => typeof(T);
        public Expression Expression => expression;
        public IQueryProvider Provider => provider;

        public IEnumerator<T> GetEnumerator()
        {
            // Execute the query through the provider
            var result = provider.Execute<IEnumerable<T>>(expression);
            return result.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Expression visitor for WHERE clauses
    /// </summary>
    internal class WhereExpressionVisitor : ExpressionVisitor
    {
        private readonly StringBuilder whereClause = new();
        private readonly Dictionary<string, object> parameters = new();
        private int parameterIndex = 0;

        public string WhereClause => whereClause.ToString();
        public Dictionary<string, object> Parameters => parameters;

        protected override Expression VisitBinary(BinaryExpression node)
        {
            Visit(node.Left);
            
            whereClause.Append(node.NodeType switch
            {
                ExpressionType.Equal => " = ",
                ExpressionType.NotEqual => " != ",
                ExpressionType.GreaterThan => " > ",
                ExpressionType.GreaterThanOrEqual => " >= ",
                ExpressionType.LessThan => " < ",
                ExpressionType.LessThanOrEqual => " <= ",
                ExpressionType.AndAlso => " AND ",
                ExpressionType.OrElse => " OR ",
                _ => throw new NotSupportedException($"Binary operator {node.NodeType} not supported")
            });
            
            Visit(node.Right);
            
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member is PropertyInfo property)
            {
                whereClause.Append(property.Name);
            }
            else
            {
                // This is a value access
                var value = GetValue(node);
                var paramName = $"p{parameterIndex++}";
                whereClause.Append($"@{paramName}");
                parameters[paramName] = value ?? DBNull.Value;
            }
            
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var paramName = $"p{parameterIndex++}";
            whereClause.Append($"@{paramName}");
            parameters[paramName] = node.Value ?? DBNull.Value;
            return node;
        }

        private object? GetValue(Expression expression)
        {
            var lambda = Expression.Lambda(expression);
            var compiled = lambda.Compile();
            return compiled.DynamicInvoke();
        }
    }

    /// <summary>
    /// Migration support
    /// </summary>
    public abstract class Migration
    {
        public string Id { get; }
        public string Description { get; }

        protected Migration(string id, string description)
        {
            Id = id;
            Description = description;
        }

        public abstract Task UpAsync(Database database);
        public abstract Task DownAsync(Database database);
    }

    /// <summary>
    /// Migration runner
    /// </summary>
    public class MigrationRunner
    {
        private readonly Database database;
        private readonly List<Migration> migrations = new();

        public MigrationRunner(Database database)
        {
            this.database = database;
        }

        public void AddMigration(Migration migration)
        {
            migrations.Add(migration);
        }

        public async Task RunAsync()
        {
            // Ensure migrations table exists
            await EnsureMigrationsTableAsync();
            
            // Get applied migrations
            var appliedMigrations = await GetAppliedMigrationsAsync();
            
            // Run pending migrations
            foreach (var migration in migrations.Where(m => !appliedMigrations.Contains(m.Id)).OrderBy(m => m.Id))
            {
                Console.WriteLine($"Applying migration: {migration.Id} - {migration.Description}");
                
                using var transaction = await database.BeginTransactionAsync();
                try
                {
                    await migration.UpAsync(database);
                    await RecordMigrationAsync(migration);
                    await transaction.CommitAsync();
                    
                    Console.WriteLine($"Migration {migration.Id} applied successfully");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new MigrationException($"Migration {migration.Id} failed", ex);
                }
            }
        }

        private async Task EnsureMigrationsTableAsync()
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__Migrations')
                CREATE TABLE __Migrations (
                    Id NVARCHAR(255) PRIMARY KEY,
                    AppliedOn DATETIME NOT NULL
                )";
            
            await database.ExecuteAsync(sql);
        }

        private async Task<HashSet<string>> GetAppliedMigrationsAsync()
        {
            var sql = "SELECT Id FROM __Migrations";
            var results = await database.QueryAsync(sql);
            return new HashSet<string>(results.Select(r => (string)r["Id"]));
        }

        private async Task RecordMigrationAsync(Migration migration)
        {
            var sql = "INSERT INTO __Migrations (Id, AppliedOn) VALUES (@Id, @AppliedOn)";
            await database.ExecuteAsync(sql, new { Id = migration.Id, AppliedOn = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Migration exception
    /// </summary>
    public class MigrationException : Exception
    {
        public MigrationException(string message) : base(message) { }
        public MigrationException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Connection string builder and manager
    /// </summary>
    public class ConnectionStringBuilder
    {
        private readonly Dictionary<string, string> parameters = new();

        public string Server 
        { 
            get => GetValue("Server") ?? "localhost";
            set => SetValue("Server", value);
        }

        public string Database
        {
            get => GetValue("Database") ?? "";
            set => SetValue("Database", value);
        }

        public string UserId
        {
            get => GetValue("User Id") ?? "";
            set => SetValue("User Id", value);
        }

        public string Password
        {
            get => GetValue("Password") ?? "";
            set => SetValue("Password", value);
        }

        public int Port
        {
            get => int.TryParse(GetValue("Port"), out var port) ? port : 0;
            set => SetValue("Port", value.ToString());
        }

        public bool IntegratedSecurity
        {
            get => bool.TryParse(GetValue("Integrated Security"), out var result) && result;
            set => SetValue("Integrated Security", value.ToString());
        }

        public int ConnectionTimeout
        {
            get => int.TryParse(GetValue("Connection Timeout"), out var timeout) ? timeout : 30;
            set => SetValue("Connection Timeout", value.ToString());
        }

        public bool Pooling
        {
            get => !bool.TryParse(GetValue("Pooling"), out var result) || result;
            set => SetValue("Pooling", value.ToString());
        }

        public int MinPoolSize
        {
            get => int.TryParse(GetValue("Min Pool Size"), out var size) ? size : 0;
            set => SetValue("Min Pool Size", value.ToString());
        }

        public int MaxPoolSize
        {
            get => int.TryParse(GetValue("Max Pool Size"), out var size) ? size : 100;
            set => SetValue("Max Pool Size", value.ToString());
        }

        private string? GetValue(string key)
        {
            return parameters.TryGetValue(key, out var value) ? value : null;
        }

        private void SetValue(string key, string value)
        {
            parameters[key] = value;
        }

        public ConnectionStringBuilder() { }

        public ConnectionStringBuilder(string connectionString)
        {
            Parse(connectionString);
        }

        public void Parse(string connectionString)
        {
            parameters.Clear();
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                var kvp = part.Split('=', 2);
                if (kvp.Length == 2)
                {
                    parameters[kvp[0].Trim()] = kvp[1].Trim();
                }
            }
        }

        public string BuildPostgreSQL()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(Server))
                parts.Add($"Host={Server}");
            if (Port > 0 && Port != 5432)
                parts.Add($"Port={Port}");
            if (!string.IsNullOrEmpty(Database))
                parts.Add($"Database={Database}");
            if (!string.IsNullOrEmpty(UserId))
                parts.Add($"Username={UserId}");
            if (!string.IsNullOrEmpty(Password))
                parts.Add($"Password={Password}");
            if (ConnectionTimeout != 30)
                parts.Add($"Timeout={ConnectionTimeout}");
            if (!Pooling)
                parts.Add("Pooling=false");
            if (MinPoolSize > 0)
                parts.Add($"Minimum Pool Size={MinPoolSize}");
            if (MaxPoolSize != 100)
                parts.Add($"Maximum Pool Size={MaxPoolSize}");
            
            return string.Join(";", parts);
        }

        public string BuildMySQL()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(Server))
                parts.Add($"Server={Server}");
            if (Port > 0 && Port != 3306)
                parts.Add($"Port={Port}");
            if (!string.IsNullOrEmpty(Database))
                parts.Add($"Database={Database}");
            if (!string.IsNullOrEmpty(UserId))
                parts.Add($"User={UserId}");
            if (!string.IsNullOrEmpty(Password))
                parts.Add($"Password={Password}");
            if (ConnectionTimeout != 30)
                parts.Add($"Connection Timeout={ConnectionTimeout}");
            if (!Pooling)
                parts.Add("Pooling=false");
            if (MinPoolSize > 0)
                parts.Add($"Min Pool Size={MinPoolSize}");
            if (MaxPoolSize != 100)
                parts.Add($"Max Pool Size={MaxPoolSize}");
            
            parts.Add("AllowUserVariables=true");
            parts.Add("UseAffectedRows=true");
            
            return string.Join(";", parts);
        }

        public string BuildSQLite()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(Database))
                parts.Add($"Data Source={Database}");
            if (ConnectionTimeout != 30)
                parts.Add($"Default Timeout={ConnectionTimeout}");
            if (!Pooling)
                parts.Add("Pooling=false");
            
            parts.Add("Version=3");
            
            return string.Join(";", parts);
        }

        public override string ToString()
        {
            return string.Join(";", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }
    }

    /// <summary>
    /// Enhanced migration builder
    /// </summary>
    public class MigrationBuilder
    {
        private readonly List<MigrationOperation> operations = new();
        
        public void CreateTable(string name, Action<CreateTableBuilder> configure)
        {
            var builder = new CreateTableBuilder(name);
            configure(builder);
            operations.Add(builder.Build());
        }
        
        public void AlterTable(string name, Action<AlterTableBuilder> configure)
        {
            var builder = new AlterTableBuilder(name);
            configure(builder);
            operations.Add(builder.Build());
        }
        
        public void DropTable(string name)
        {
            operations.Add(new DropTableOperation { TableName = name });
        }
        
        public void CreateIndex(string name, string table, params string[] columns)
        {
            operations.Add(new CreateIndexOperation
            {
                IndexName = name,
                TableName = table,
                Columns = columns.ToList()
            });
        }
        
        public void DropIndex(string name, string table)
        {
            operations.Add(new DropIndexOperation
            {
                IndexName = name,
                TableName = table
            });
        }
        
        public void Sql(string sql)
        {
            operations.Add(new RawSqlOperation { Sql = sql });
        }
        
        public List<MigrationOperation> Build() => operations;
    }
    
    public class CreateTableBuilder
    {
        private readonly string tableName;
        private readonly List<ColumnDefinition> columns = new();
        private readonly List<string> primaryKeys = new();
        private readonly List<ForeignKeyDefinition> foreignKeys = new();
        
        public CreateTableBuilder(string tableName)
        {
            this.tableName = tableName;
        }
        
        public CreateTableBuilder Column(string name, string type, Action<ColumnBuilder>? configure = null)
        {
            var builder = new ColumnBuilder(name, type);
            configure?.Invoke(builder);
            columns.Add(builder.Build());
            return this;
        }
        
        public CreateTableBuilder PrimaryKey(params string[] columnNames)
        {
            primaryKeys.AddRange(columnNames);
            return this;
        }
        
        public CreateTableBuilder ForeignKey(string column, string referencedTable, string referencedColumn)
        {
            foreignKeys.Add(new ForeignKeyDefinition
            {
                Column = column,
                ReferencedTable = referencedTable,
                ReferencedColumn = referencedColumn
            });
            return this;
        }
        
        public CreateTableOperation Build()
        {
            return new CreateTableOperation
            {
                TableName = tableName,
                Columns = columns,
                PrimaryKeys = primaryKeys,
                ForeignKeys = foreignKeys
            };
        }
    }
    
    public class ColumnBuilder
    {
        private readonly ColumnDefinition column;
        
        public ColumnBuilder(string name, string type)
        {
            column = new ColumnDefinition { Name = name, Type = type };
        }
        
        public ColumnBuilder NotNull()
        {
            column.IsNullable = false;
            return this;
        }
        
        public ColumnBuilder DefaultValue(object value)
        {
            column.DefaultValue = value;
            return this;
        }
        
        public ColumnBuilder Identity()
        {
            column.IsIdentity = true;
            return this;
        }
        
        public ColumnBuilder Unique()
        {
            column.IsUnique = true;
            return this;
        }
        
        public ColumnDefinition Build() => column;
    }
    
    public class AlterTableBuilder
    {
        private readonly string tableName;
        private readonly List<MigrationOperation> operations = new();
        
        public AlterTableBuilder(string tableName)
        {
            this.tableName = tableName;
        }
        
        public AlterTableBuilder AddColumn(string name, string type, Action<ColumnBuilder>? configure = null)
        {
            var builder = new ColumnBuilder(name, type);
            configure?.Invoke(builder);
            operations.Add(new AddColumnOperation
            {
                TableName = tableName,
                Column = builder.Build()
            });
            return this;
        }
        
        public AlterTableBuilder DropColumn(string name)
        {
            operations.Add(new DropColumnOperation
            {
                TableName = tableName,
                ColumnName = name
            });
            return this;
        }
        
        public AlterTableBuilder RenameColumn(string oldName, string newName)
        {
            operations.Add(new RenameColumnOperation
            {
                TableName = tableName,
                OldName = oldName,
                NewName = newName
            });
            return this;
        }
        
        public AlterTableOperation Build()
        {
            return new AlterTableOperation
            {
                TableName = tableName,
                Operations = operations
            };
        }
    }
    
    // Migration operation classes
    public abstract class MigrationOperation { }
    
    public class CreateTableOperation : MigrationOperation
    {
        public string TableName { get; set; } = "";
        public List<ColumnDefinition> Columns { get; set; } = new();
        public List<string> PrimaryKeys { get; set; } = new();
        public List<ForeignKeyDefinition> ForeignKeys { get; set; } = new();
    }
    
    public class AlterTableOperation : MigrationOperation
    {
        public string TableName { get; set; } = "";
        public List<MigrationOperation> Operations { get; set; } = new();
    }
    
    public class DropTableOperation : MigrationOperation
    {
        public string TableName { get; set; } = "";
    }
    
    public class AddColumnOperation : MigrationOperation
    {
        public string TableName { get; set; } = "";
        public ColumnDefinition Column { get; set; } = new();
    }
    
    public class DropColumnOperation : MigrationOperation
    {
        public string TableName { get; set; } = "";
        public string ColumnName { get; set; } = "";
    }
    
    public class RenameColumnOperation : MigrationOperation
    {
        public string TableName { get; set; } = "";
        public string OldName { get; set; } = "";
        public string NewName { get; set; } = "";
    }
    
    public class CreateIndexOperation : MigrationOperation
    {
        public string IndexName { get; set; } = "";
        public string TableName { get; set; } = "";
        public List<string> Columns { get; set; } = new();
        public bool IsUnique { get; set; }
    }
    
    public class DropIndexOperation : MigrationOperation
    {
        public string IndexName { get; set; } = "";
        public string TableName { get; set; } = "";
    }
    
    public class RawSqlOperation : MigrationOperation
    {
        public string Sql { get; set; } = "";
    }
    
    public class ColumnDefinition
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsNullable { get; set; } = true;
        public object? DefaultValue { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsUnique { get; set; }
    }
    
    public class ForeignKeyDefinition
    {
        public string Column { get; set; } = "";
        public string ReferencedTable { get; set; } = "";
        public string ReferencedColumn { get; set; } = "";
        public string? OnDelete { get; set; }
        public string? OnUpdate { get; set; }
    }
} 