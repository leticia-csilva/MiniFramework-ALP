using System.Globalization;
using System.Reflection;
using MySqlConnector;
using MiniFramework.Attributes;
using MiniFramework.Generators;

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

    public IReadOnlyList<FormOption> GetOptions(Type modelType)
    {
        PropertyInfo key = GetPrimaryKey(modelType);
        PropertyInfo display = modelType.GetProperties()
            .FirstOrDefault(property =>
                property != key && property.PropertyType == typeof(string))
            ?? key;

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand(
            $"SELECT {QuoteIdentifier(key.Name)}, {QuoteIdentifier(display.Name)} " +
            $"FROM {QuoteIdentifier(modelType.Name)} ORDER BY {QuoteIdentifier(display.Name)};",
            connection);
        using var reader = command.ExecuteReader();

        List<FormOption> options = new();
        while (reader.Read())
        {
            options.Add(new FormOption(
                Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture) ?? ""));
        }

        return options;
    }

    public bool ReferencedValueExists(Type modelType, object value)
    {
        PropertyInfo key = GetPrimaryKey(modelType);
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand(
            $"SELECT EXISTS(SELECT 1 FROM {QuoteIdentifier(modelType.Name)} " +
            $"WHERE {QuoteIdentifier(key.Name)} = @value);",
            connection);
        command.Parameters.AddWithValue("@value", ToDatabaseValue(value));
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
    }

    public void InsertModel(object model)
    {
        Type modelType = model.GetType();
        PropertyInfo[] properties = modelType.GetProperties()
            .Where(property => !Attribute.IsDefined(property, typeof(PrimaryKeyAttribute)))
            .ToArray();

        if (properties.Length == 0)
            throw new InvalidOperationException($"O modelo {modelType.Name} não possui campos para inserir.");

        string columns = string.Join(", ", properties.Select(property => QuoteIdentifier(property.Name)));
        string parameters = string.Join(", ", properties.Select((_, index) => $"@p{index}"));

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        using var command = new MySqlCommand(
            $"INSERT INTO {QuoteIdentifier(modelType.Name)} ({columns}) VALUES ({parameters});",
            connection);

        for (int index = 0; index < properties.Length; index++)
        {
            object? value = properties[index].GetValue(model);
            command.Parameters.AddWithValue(
                $"@p{index}", value == null ? DBNull.Value : ToDatabaseValue(value));
        }

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

        return $"`{identifier}`";
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
            Enum enumValue => enumValue.ToString(),
            _ => value
        };
    }
}