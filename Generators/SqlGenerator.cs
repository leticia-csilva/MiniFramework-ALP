namespace MiniFramework.Generators;
using System.Reflection;
using MiniFramework.Core;
using MiniFramework.Attributes;

public class SqlGenerator
{
    // Para gerar os comandos SQL, é necessário converter os tipos identificados 
    // das classes (C#) para os tipos aceitos em SQL.
    public string MapType(Type propertyType)
    {
        Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        // enum guarda o nome da opcao (mesmo valor que o formulario envia)
        if (type.IsEnum)
            return "TEXT";

        if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
            type == typeof(byte) || type == typeof(bool))
            return "INTEGER";
        
        if (type == typeof(float) || type == typeof(double))
            return "REAL";

        if (type == typeof(decimal) || type == typeof(string) || type == typeof(char) || type == typeof(Guid) || 
        type == typeof(DateTime) || type == typeof(DateOnly) || type == typeof(TimeOnly) ||
        type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
            return "TEXT"; 

        if (type == typeof(byte[]))
        return "BLOB";

        throw new NotSupportedException(
            $"Tipo não suportado: {propertyType.Name}"
        );
    }


    public string GenerateCreateTable(Type modelType)
    {
        ModelReader reader = new ModelReader();

        PropertyInfo[] properties = reader.ReadModel(modelType);

        List<string> columns = new List<string>();
        List<string> foreignKeys = new List<string>();

        foreach (PropertyInfo property in properties)
        {
            string sqlType = MapType(property.PropertyType);
            string column = $"{property.Name} {sqlType}";

            if (Attribute.IsDefined(property, typeof(PrimaryKeyAttribute)))
            {
                column += " PRIMARY KEY";
            }

            ForeignKeyAttribute? foreignKey = property.GetCustomAttribute<ForeignKeyAttribute>();

            if (foreignKey != null)
            {
                PropertyInfo? referencedKey = foreignKey.ReferencedModel.GetProperties()
                    .FirstOrDefault(p => Attribute.IsDefined(p, typeof(PrimaryKeyAttribute)));

                if (referencedKey == null)
                {
                    throw new InvalidOperationException(
                        $"A classe {foreignKey.ReferencedModel.Name} não possui chave primária."
                    );
                }

                foreignKeys.Add(
                    $"FOREIGN KEY ({property.Name}) " +
                    $"REFERENCES {foreignKey.ReferencedModel.Name}({referencedKey.Name})"
                );
            }

            columns.Add(column);
        }

        columns.AddRange(foreignKeys);

        string columnsSql = string.Join(",\n    ", columns);

        return $"CREATE TABLE IF NOT EXISTS {modelType.Name} (\n    {columnsSql}\n);";
    }
}
