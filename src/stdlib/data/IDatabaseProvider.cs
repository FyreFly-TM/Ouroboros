using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Ouro.StdLib.Data
{
    /// <summary>
    /// Common interface for all database providers
    /// </summary>
    public interface IDatabaseProvider : IDisposable, IAsyncDisposable
    {
        // Connection management
        Task<DbConnection> OpenConnectionAsync();
        Task CloseConnectionAsync();
        
        // Transaction management
        Task<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        
        // Command execution
        Task<int> ExecuteNonQueryAsync(string sql, params object[] parameters);
        Task<object> ExecuteScalarAsync(string sql, params object[] parameters);
        Task<List<T>> ExecuteQueryAsync<T>(string sql, params object[] parameters) where T : new();
        Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sql, params object[] parameters);
        
        // Bulk operations
        Task<int> BulkInsertAsync<T>(string tableName, IEnumerable<T> items) where T : class;
        
        // Schema operations
        Task<bool> TableExistsAsync(string tableName, string schema = null);
        Task CreateTableAsync(string tableName, Dictionary<string, string> columns, string primaryKey = null);
        Task<List<string>> GetTableNamesAsync(string schema = null);
        Task<List<ColumnInfo>> GetTableColumnsAsync(string tableName, string schema = null);
    }
    
    /// <summary>
    /// Column metadata information
    /// </summary>
    public class ColumnInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
        public bool IsNullable { get; set; } = false;
        public string DefaultValue { get; set; } = string.Empty;
        public bool IsPrimaryKey { get; set; } = false;
        public string ColumnType { get; set; } = string.Empty;
    }
} 