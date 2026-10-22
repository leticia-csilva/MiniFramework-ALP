using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MiniFramework.Attributes;
using MiniFramework.Database;
using MiniFramework.Generators;
using MiniFramework.Models;

// Defina a variável de ambiente MINIFRAMEWORK_MYSQL com a conexão de um banco MySQL já criado.
// Exemplo PowerShell: $env:MINIFRAMEWORK_MYSQL="Server=localhost;Port=3306;Database=miniframework;User ID=root;Password=sua_senha;"
string connectionString = Environment.GetEnvironmentVariable("MINIFRAMEWORK_MYSQL") ?? string.Empty;
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Configure a variável de ambiente MINIFRAMEWORK_MYSQL antes de executar.");
    Console.Error.WriteLine("O banco informado na conexão deve existir e o usuário precisa ter permissão para criar tabelas.");
    return;
}

Type[] models = [typeof(Curso), typeof(Aluno)];
var sqlGenerator = new SqlGenerator();
var htmlGenerator = new HtmlGenerator();
var database = new DatabaseManager(connectionString);

foreach (Type model in models)
{
    string sql = sqlGenerator.GenerateCreateTable(model);
    Console.WriteLine($"--- SQL para {model.Name} ---\n{sql}\n");
    database.ExecuteSql(sql);
}

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () =>
{
    string links = string.Join(Environment.NewLine, models.Select(model =>
    {
        string route = model.Name.ToLowerInvariant();
        return $"<li><a href=\"/{route}\">Cadastrar {WebUtility.HtmlEncode(model.Name)}</a></li>";
    }));

    return Results.Content($"""
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head><meta charset="UTF-8"><title>MiniFramework</title></head>
        <body>
          <h1>MiniFramework</h1>
          <p>Escolha um formulário para cadastrar dados no MySQL.</p>
          <ul>{links}</ul>
        </body>
        </html>
        """, "text/html; charset=utf-8");
});

foreach (Type modelType in models)
{
    string route = modelType.Name.ToLowerInvariant();

    app.MapGet($"/{route}", () => Results.Content(
        htmlGenerator.GenerateForm(modelType, GetDynamicOptions(modelType)),
        "text/html; charset=utf-8"));

    app.MapPost($"/{route}", async (HttpRequest request) =>
    {
        IFormCollection form = await request.ReadFormAsync();
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        Dictionary<string, string> errors = new(StringComparer.Ordinal);
        object model = Activator.CreateInstance(modelType)
            ?? throw new InvalidOperationException($"Não foi possível criar {modelType.Name}.");

        foreach (PropertyInfo property in modelType.GetProperties())
        {
            if (Attribute.IsDefined(property, typeof(PrimaryKeyAttribute)))
                continue;

            string rawValue = form[property.Name].ToString();
            values[property.Name] = rawValue;
            Type propertyType = property.PropertyType;
            Type valueType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            bool nullable = Nullable.GetUnderlyingType(propertyType) != null;

            if (valueType == typeof(bool))
            {
                property.SetValue(model, form.ContainsKey(property.Name));
                continue;
            }

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                if (nullable)
                    property.SetValue(model, null);
                else
                    errors[property.Name] = "Este campo é obrigatório.";
                continue;
            }

            if (!TryConvertValue(rawValue, valueType, out object? convertedValue))
            {
                errors[property.Name] = "Informe um valor válido.";
                continue;
            }

            ForeignKeyAttribute? foreignKey = property.GetCustomAttribute<ForeignKeyAttribute>();
            if (foreignKey != null &&
                !database.ReferencedValueExists(foreignKey.ReferencedModel, convertedValue!))
            {
                errors[property.Name] = "Selecione um registro existente.";
                continue;
            }

            property.SetValue(model, convertedValue);
        }

        if (errors.Count > 0)
        {
            return Results.Content(
                htmlGenerator.GenerateForm(modelType, GetDynamicOptions(modelType), values, errors),
                "text/html; charset=utf-8",
                Encoding.UTF8,
                StatusCodes.Status400BadRequest);
        }

        database.InsertModel(model);
        return Results.Content($"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8"><title>Cadastro salvo</title></head>
            <body>
              <h1>Cadastro salvo!</h1>
              <p>O registro de {WebUtility.HtmlEncode(modelType.Name)} foi gravado no MySQL.</p>
              <p><a href="/{route}">Cadastrar outro</a> | <a href="/">Início</a></p>
            </body>
            </html>
            """, "text/html; charset=utf-8");
    });
}

Console.WriteLine("Tabelas presentes no banco:");
database.ShowTables();
app.Run();

Dictionary<string, IReadOnlyList<FormOption>> GetDynamicOptions(Type modelType)
{
    Dictionary<string, IReadOnlyList<FormOption>> options = new(StringComparer.Ordinal);
    foreach (PropertyInfo property in modelType.GetProperties())
    {
        ForeignKeyAttribute? foreignKey = property.GetCustomAttribute<ForeignKeyAttribute>();
        if (foreignKey != null)
            options[property.Name] = database.GetOptions(foreignKey.ReferencedModel);
    }

    return options;
}

static bool TryConvertValue(string value, Type type, out object? result)
{
    const NumberStyles numberStyles = NumberStyles.Number;
    const NumberStyles floatingStyles = NumberStyles.Float | NumberStyles.AllowThousands;
    result = null;

    if (type == typeof(string))
    {
        result = value;
        return true;
    }
    if (type == typeof(char) && value.Length == 1)
    {
        result = value[0];
        return true;
    }
    if (type == typeof(Guid) && Guid.TryParse(value, out Guid guid))
    {
        result = guid;
        return true;
    }
    if (type == typeof(bool) && bool.TryParse(value, out bool boolean))
    {
        result = boolean;
        return true;
    }
    if (type == typeof(DateTime) &&
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dateTime))
    {
        result = dateTime;
        return true;
    }
    if (type == typeof(DateTimeOffset) &&
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset dateTimeOffset))
    {
        result = dateTimeOffset;
        return true;
    }
    if (type == typeof(DateOnly) &&
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly dateOnly))
    {
        result = dateOnly;
        return true;
    }
    if (type == typeof(TimeOnly) &&
        TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly timeOnly))
    {
        result = timeOnly;
        return true;
    }
    if (type == typeof(TimeSpan) && TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan timeSpan))
    {
        result = timeSpan;
        return true;
    }
    if (type == typeof(byte) && byte.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out byte byteValue))
    {
        result = byteValue;
        return true;
    }
    if (type == typeof(short) && short.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out short shortValue))
    {
        result = shortValue;
        return true;
    }
    if (type == typeof(int) && int.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out int intValue))
    {
        result = intValue;
        return true;
    }
    if (type == typeof(long) && long.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out long longValue))
    {
        result = longValue;
        return true;
    }
    if (type == typeof(float) && float.TryParse(value, floatingStyles, CultureInfo.InvariantCulture, out float floatValue))
    {
        result = floatValue;
        return true;
    }
    if (type == typeof(double) && double.TryParse(value, floatingStyles, CultureInfo.InvariantCulture, out double doubleValue))
    {
        result = doubleValue;
        return true;
    }
    if (type == typeof(decimal) && decimal.TryParse(value, floatingStyles, CultureInfo.InvariantCulture, out decimal decimalValue))
    {
        result = decimalValue;
        return true;
    }
    if (type.IsEnum && Enum.TryParse(type, value, ignoreCase: false, out object? enumValue) &&
        Enum.IsDefined(type, enumValue))
    {
        result = enumValue;
        return true;
    }

    return false;
}
