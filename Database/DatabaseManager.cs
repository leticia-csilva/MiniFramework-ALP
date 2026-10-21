using System;
using MySqlConnector;

namespace MiniFramework.Database;

public class DatabaseManager
{
    private readonly string _connectionString;

    public DatabaseManager(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Informe a string de conexão MySQL.", nameof(connectionString));
            
        _connectionString = connectionString;
    }

    public void ExecuteSql(string sql)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    public void ShowTables()
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand("SHOW TABLES;", connection);
        using var reader = command.ExecuteReader();
        
        while (reader.Read()) 
        {
            Console.WriteLine(reader.GetString(0));
        }
    }
}