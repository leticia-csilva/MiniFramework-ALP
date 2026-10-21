using System.Reflection;
using System.Text.RegularExpressions;
using MiniFramework.Attributes;
using MiniFramework.Core;

namespace MiniFramework.Generators;

/// <summary>Gera DDL MySQL a partir das propriedades públicas de um modelo C#.</summary>
public sealed class SqlGenerator
{
    private static string Identifier(string name)
    {
        if (!Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$"))
            throw new ArgumentException($"Identificador SQL inválido: {name}");
        return $"`{name}`";
    }

    public string MapType(Type propertyType)
    {
        Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (type.IsEnum || type == typeof(string) || type == typeof(char) || type == typeof(Guid) ||
            type == typeof(DateTime) || type == typeof(DateOnly) || type == typeof(TimeOnly) ||
            type == typeof(DateTimeOffset) || type == typeof(TimeSpan)) return "VARCHAR(255)";
        if (type == typeof(bool) || type == typeof(byte)) return "TINYINT(1)";
        if (type == typeof(short)) return "SMALLINT";
        if (type == typeof(int)) return "INT";
        if (type == typeof(long)) return "BIGINT";
        if (type == typeof(float)) return "FLOAT";
        if (type == typeof(double)) return "DOUBLE";
        if (type == typeof(decimal)) return "DECIMAL(18,2)";
        if (type == typeof(byte[])) return "BLOB";
        throw new NotSupportedException($"Tipo não suportado: {propertyType.Name}");
    }

    public string GenerateCreateTable(Type modelType)
    {
        if (modelType == null || !modelType.IsClass) throw new ArgumentException("O modelo deve ser uma classe.", nameof(modelType));
        PropertyInfo[] properties = new ModelReader().ReadModel(modelType);
        if (properties.Length == 0) throw new InvalidOperationException($"O modelo {modelType.Name} não possui propriedades públicas.");

        var definitions = new List<string>();
        var foreignKeys = new List<string>();
        foreach (var property in properties)
        {
            string columnName = Identifier(property.Name);
            string definition = $"{columnName} {MapType(property.PropertyType)}";
            if (property.GetCustomAttribute<PrimaryKeyAttribute>() != null)
            {
                definition += " NOT NULL";
                if ((Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType) == typeof(int) ||
                    (Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType) == typeof(long))
                    definition += " AUTO_INCREMENT";
                definition += " PRIMARY KEY";
            }
            else if (Nullable.GetUnderlyingType(property.PropertyType) == null &&
                     (Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType).IsValueType)
                definition += " NOT NULL";
            definitions.Add(definition);

            var fk = property.GetCustomAttribute<ForeignKeyAttribute>();
            if (fk != null)
            {
                PropertyInfo? key = fk.ReferencedModel.GetProperties()
                    .FirstOrDefault(x => x.GetCustomAttribute<PrimaryKeyAttribute>() != null);
                if (key == null) throw new InvalidOperationException($"A classe {fk.ReferencedModel.Name} não possui chave primária.");
                if (MapType(property.PropertyType) != MapType(key.PropertyType))
                    throw new InvalidOperationException($"A chave estrangeira {property.Name} deve ter o mesmo tipo da chave {key.Name}.");
                string constraint = Identifier($"FK_{modelType.Name}_{property.Name}");
                foreignKeys.Add($"CONSTRAINT {constraint} FOREIGN KEY ({columnName}) REFERENCES {Identifier(fk.ReferencedModel.Name)} ({Identifier(key.Name)})");
            }
        }
        definitions.AddRange(foreignKeys);
        return $"CREATE TABLE IF NOT EXISTS {Identifier(modelType.Name)} (\n  {string.Join(",\n  ", definitions)}\n) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
    }
}
