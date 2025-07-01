using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Ouroboros.StdLib.Data.Mocks
{
    // Mock factory base
    internal abstract class MockDbProviderFactory : DbProviderFactory
    {
        public override DbCommand CreateCommand() => new MockDbCommand();
        public override DbConnection CreateConnection() => CreateMockConnection();
        public override DbParameter CreateParameter() => new MockDbParameter();
        
        protected abstract MockDbConnection CreateMockConnection();
    }
    
    // Minimal mock implementations for testing
    internal abstract class MockDbConnection : DbConnection
    {
        public override string ConnectionString { get; set; }
        public abstract override string Database { get; }
        public abstract override string DataSource { get; }
        public abstract override string ServerVersion { get; }
        public override ConnectionState State => ConnectionState.Open;
        
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new MockDbTransaction(this);
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
        private readonly DbConnection connection;
        
        public MockDbTransaction(DbConnection connection)
        {
            this.connection = connection;
        }
        
        protected override DbConnection DbConnection => connection;
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
        public override global::System.Collections.IEnumerator GetEnumerator() => parameters.GetEnumerator();
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
        protected virtual string DefaultDataType => "TEXT";
        
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
        public override string GetDataTypeName(int ordinal) => DefaultDataType;
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
        public override global::System.Collections.IEnumerator GetEnumerator() => new List<object>().GetEnumerator();
    }
    
    // Provider-specific implementations
    internal class MockPostgreSQLProviderFactory : MockDbProviderFactory
    {
        protected override MockDbConnection CreateMockConnection() => new MockPostgreSQLConnection();
    }
    
    internal class MockPostgreSQLConnection : MockDbConnection
    {
        public override string Database => "postgres";
        public override string DataSource => "localhost";
        public override string ServerVersion => "13.0";
    }
    
    internal class MockMySQLProviderFactory : MockDbProviderFactory
    {
        protected override MockDbConnection CreateMockConnection() => new MockMySQLConnection();
    }
    
    internal class MockMySQLConnection : MockDbConnection
    {
        public override string Database => "mysql";
        public override string DataSource => "localhost";
        public override string ServerVersion => "8.0.0";
    }
    
    internal class MockSQLiteProviderFactory : MockDbProviderFactory
    {
        protected override MockDbConnection CreateMockConnection() => new MockSQLiteConnection();
    }
    
    internal class MockSQLiteConnection : MockDbConnection
    {
        public override string Database => "main";
        public override string DataSource => ":memory:";
        public override string ServerVersion => "3.0.0";
    }
} 