using Microsoft.Data.Sqlite;

namespace MiniFramework.Database;

public class DatabaseManager
{
    private readonly string connectionString;

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


    // Verifica se as tabelas realmente forma criadas no banco
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