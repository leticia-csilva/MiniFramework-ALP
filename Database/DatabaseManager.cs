using Microsoft.Data.Sqlite;

namespace MiniFramework.Database;

public class DatabaseManager
{
    private readonly string connectionString;using MySqlConnector;

namespace MiniFramework.Database;

/// <summary>Executa comandos no MySQL. A string de conexão deve apontar para um banco existente.</summary>
public sealed class DatabaseManager
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
        while (reader.Read()) Console.WriteLine(reader.GetString(0));
    }
}


    public DatabaseManager(string databaseFile)
    {
        connectionString = $"Data Source={databaseFile}";
    }

    public void ExecuteSql(string sql)
    {
        using SqliteConnection connection =
            new SqliteConnection(connectionString);

        connection.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        command.CommandText = sql;
        command.ExecuteNonQuery();
    }


    // Verifica se as tabelas realmente foram criadas no banco
    public void ShowTables()
    {
        using SqliteConnection connection =
            new SqliteConnection(connectionString);

        connection.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;";

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            Console.WriteLine(reader.GetString(0));
        }
    }
}   
