using System.Globalization;
using System.Reflection;
using Microsoft.Data.Sqlite;
using MiniFramework.Attributes;
using MiniFramework.Generators;

namespace MiniFramework.Database;

public class DatabaseManager
{
    private readonly string connectionString;

    public DatabaseManager(string databaseFile)
    {
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databaseFile,
            ForeignKeys = true
        }.ToString();
    }

    public void ExecuteSql(string sql)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public void EnsureModel(Type modelType, string createTableSql)
    {
        ExecuteSql(createTableSql);

        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteIdentifier(modelType.Name)});";

        HashSet<string> existingColumns = new(StringComparer.OrdinalIgnoreCase);
        using (SqliteDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
                existingColumns.Add(reader.GetString(1));
        }

        foreach (PropertyInfo property in modelType.GetProperties())
        {
            if (existingColumns.Contains(property.Name))
                continue;

            if (Attribute.IsDefined(property, typeof(ForeignKeyAttribute)))
                throw new InvalidOperationException(
                    $"A coluna de chave estrangeira {modelType.Name}.{property.Name} não existe no banco. " +
                    "Crie a tabela em um banco novo ou faça a migração dessa relação manualmente.");

            string sqlType = new SqlGenerator().MapType(property.PropertyType);
            bool nullable = Nullable.GetUnderlyingType(property.PropertyType) != null ||
                !property.PropertyType.IsValueType;
            string defaultValue = sqlType switch
            {
                "TEXT" => "''",
                "BLOB" => "X''",
                _ => "0"
            };
            string declaration = nullable
                ? sqlType
                : $"{sqlType} NOT NULL DEFAULT {defaultValue}";

            command.CommandText =
                $"ALTER TABLE {QuoteIdentifier(modelType.Name)} " +
                $"ADD COLUMN {QuoteIdentifier(property.Name)} {declaration};";
            command.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<FormOption> GetOptions(Type modelType)
    {
        PropertyInfo key = GetPrimaryKey(modelType);
        PropertyInfo display = modelType.GetProperties()
            .FirstOrDefault(property =>
                property != key && property.PropertyType == typeof(string))
            ?? key;

        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"SELECT {QuoteIdentifier(key.Name)}, {QuoteIdentifier(display.Name)} " +
            $"FROM {QuoteIdentifier(modelType.Name)} ORDER BY {QuoteIdentifier(display.Name)};";

        using SqliteDataReader reader = command.ExecuteReader();
        List<FormOption> options = new();
        while (reader.Read())
        {
            options.Add(new FormOption(
                Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture) ?? ""));
        }

        return options;
    }

    public bool ReferencedValueExists(Type modelType, object? value)
    {
        if (value == null)
            return false;

        PropertyInfo key = GetPrimaryKey(modelType);
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"SELECT EXISTS(SELECT 1 FROM {QuoteIdentifier(modelType.Name)} " +
            $"WHERE {QuoteIdentifier(key.Name)} = @value);";
        command.Parameters.AddWithValue("@value", ToDatabaseValue(value));
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
    }

    public void InsertModel(object model)
    {
        Type modelType = model.GetType();
        PropertyInfo[] properties = modelType.GetProperties()
            .Where(property => !Attribute.IsDefined(property, typeof(PrimaryKeyAttribute)))
            .ToArray();

        if (properties.Length == 0)
            throw new InvalidOperationException($"O modelo {modelType.Name} não possui campos para inserir.");

        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        string columns = string.Join(", ", properties.Select(property => QuoteIdentifier(property.Name)));
        string parameters = string.Join(", ", properties.Select((_, index) => $"@p{index}"));
        command.CommandText =
            $"INSERT INTO {QuoteIdentifier(modelType.Name)} ({columns}) VALUES ({parameters});";

        for (int index = 0; index < properties.Length; index++)
        {
            object? value = properties[index].GetValue(model);
            command.Parameters.AddWithValue($"@p{index}", value == null ? DBNull.Value : ToDatabaseValue(value));
        }

        command.ExecuteNonQuery();
    }

    public void ShowTables()
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
            Console.WriteLine(reader.GetString(0));
    }

    private SqliteConnection OpenConnection()
    {
        SqliteConnection connection = new(connectionString);
        connection.Open();
        return connection;
    }

    private static PropertyInfo GetPrimaryKey(Type modelType)
    {
        return modelType.GetProperties()
            .FirstOrDefault(property => Attribute.IsDefined(property, typeof(PrimaryKeyAttribute)))
            ?? throw new InvalidOperationException($"O modelo {modelType.Name} não possui chave primária.");
    }

    private static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) ||
            identifier.Any(character => !char.IsLetterOrDigit(character) && character != '_'))
            throw new InvalidOperationException($"Identificador SQL inválido: {identifier}");

        return $"\"{identifier}\"";
    }

    private static object ToDatabaseValue(object value)
    {
        return value switch
        {
            bool boolean => boolean ? 1 : 0,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly timeOnly => timeOnly.ToString("O", CultureInfo.InvariantCulture),
            TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString(),
            _ => value
        };
    }
}
