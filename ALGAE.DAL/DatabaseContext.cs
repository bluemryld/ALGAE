using System;
using System.Data;

using Microsoft.Data.Sqlite;

namespace Algae.DAL
{
    public class DatabaseContext : IDisposable
    {
        private readonly string _connectionString;
        private SqliteConnection? _connection;

        public DatabaseContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Create a new SQLite connection (caller is responsible for opening/closing)
        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }
        
        // Open or get the existing SQLite connection
        public SqliteConnection GetConnection()
        {
            if (_connection == null)
            {
                _connection = new SqliteConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }

        // Create a command for executing queries
        public IDbCommand CreateCommand(string query, SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = query;
            return command;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}