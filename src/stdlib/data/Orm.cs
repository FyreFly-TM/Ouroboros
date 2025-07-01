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
            // Running async code synchronously can cause deadlocks
            // This is intentionally not implemented to force async patterns
            throw new NotImplementedException("Use SaveChangesAsync instead");
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
            // Synchronous database operations can cause thread pool starvation
            // We strongly recommend using async methods instead
            throw new NotImplementedException("Synchronous execution not supported - use ToListAsync");
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
            throw new NotImplementedException("Synchronous execution not supported");
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotImplementedException("Synchronous execution not supported");
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
            throw new NotImplementedException("Use async methods instead");
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
} 