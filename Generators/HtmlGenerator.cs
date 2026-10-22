using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using MiniFramework.Attributes;
using MiniFramework.Core;

namespace MiniFramework.Generators;

public sealed record FormOption(string Value, string Label);

public class HtmlGenerator
{
    public string MapInputType(Type propertyType)
    {
        Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
            type == typeof(byte) || type == typeof(float) || type == typeof(double) ||
            type == typeof(decimal))
            return "number";
        if (type == typeof(bool))
            return "checkbox";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return "datetime-local";
        if (type == typeof(DateOnly))
            return "date";
        if (type == typeof(TimeOnly) || type == typeof(TimeSpan))
            return "time";
        if (type == typeof(string) || type == typeof(char) || type == typeof(Guid))
            return "text";

        throw new NotSupportedException(
            $"Tipo não suportado no formulário: {propertyType.Name}");
    }

    public string GenerateForm(
        Type modelType,
        IReadOnlyDictionary<string, IReadOnlyList<FormOption>>? dynamicOptions = null,
        IReadOnlyDictionary<string, string>? values = null,
        IReadOnlyDictionary<string, string>? errors = null)
    {
        PropertyInfo[] properties = new ModelReader().ReadModel(modelType);
        string formName = modelType.Name.ToLowerInvariant();
        string title = WebUtility.HtmlEncode(modelType.Name);
        StringBuilder fields = new();

        foreach (PropertyInfo property in properties)
        {
            if (Attribute.IsDefined(property, typeof(PrimaryKeyAttribute)))
                continue;

            string name = WebUtility.HtmlEncode(property.Name);
            string value = values?.GetValueOrDefault(property.Name) ?? "";
            bool nullable = Nullable.GetUnderlyingType(property.PropertyType) != null;
            Type valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            bool required = !nullable && valueType != typeof(bool);
            ForeignKeyAttribute? foreignKey = property.GetCustomAttribute<ForeignKeyAttribute>();
            bool isEnum = valueType.IsEnum;
            bool isRadio = isEnum && Attribute.IsDefined(property, typeof(RadioAttribute));
            IReadOnlyList<FormOption>? options = null;

            if (foreignKey != null)
            {
                dynamicOptions?.TryGetValue(property.Name, out options);
                options ??= Array.Empty<FormOption>();
            }
            else if (isEnum)
            {
                options = Enum.GetNames(valueType)
                    .Select(option => new FormOption(option, option))
                    .ToArray();
            }

            fields.AppendLine(" <div class=\"campo\">");
            fields.AppendLine($"  <label>{name}{(foreignKey == null ? "" : $" <small>(Id de {WebUtility.HtmlEncode(foreignKey.ReferencedModel.Name)})</small>")}</label>");

            if (errors?.TryGetValue(property.Name, out string? error) == true)
                fields.AppendLine($"  <span class=\"erro\">{WebUtility.HtmlEncode(error)}</span>");

            if (foreignKey != null || (isEnum && !isRadio))
            {
                fields.AppendLine($"  <select id=\"{name}\" name=\"{name}\"{(required ? " required" : "")}>");
                fields.AppendLine("   <option value=\"\">Selecione...</option>");
                foreach (FormOption option in options!)
                {
                    string selected = option.Value == value ? " selected" : "";
                    fields.AppendLine($"   <option value=\"{WebUtility.HtmlEncode(option.Value)}\"{selected}>{WebUtility.HtmlEncode(option.Label)}</option>");
                }
                fields.AppendLine("  </select>");
            }
            else if (isRadio)
            {
                foreach (FormOption option in options!)
                {
                    string checkedAttribute = option.Value == value ? " checked" : "";
                    fields.AppendLine($"  <label class=\"opcao\"><input type=\"radio\" name=\"{name}\" value=\"{WebUtility.HtmlEncode(option.Value)}\"{checkedAttribute}{(required ? " required" : "")}> {WebUtility.HtmlEncode(option.Label)}</label>");
                }
            }
            else
            {
                string inputType = MapInputType(property.PropertyType);
                string extra = inputType == "number" && IsDecimal(property.PropertyType) ? " step=\"any\"" : "";
                string requiredAttribute = required ? " required" : "";
                string checkedAttribute = inputType == "checkbox" && value.Equals("true", StringComparison.OrdinalIgnoreCase) ? " checked" : "";
                string formattedValue = FormatInputValue(property.PropertyType, value);
                string valueAttribute = inputType == "checkbox" ? "" : $" value=\"{WebUtility.HtmlEncode(formattedValue)}\"";
                fields.AppendLine($"  <input type=\"{inputType}\" id=\"{name}\" name=\"{name}\"{valueAttribute}{extra}{requiredAttribute}{checkedAttribute}>");
            }

            fields.AppendLine(" </div>");
        }

        string errorSummary = errors?.Count > 0
            ? $"<div class=\"erro resumo\">{WebUtility.HtmlEncode(string.Join(" ", errors.Values.Distinct()))}</div>"
            : "";

        return $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>Cadastro de {title}</title>
  <style>
    body {{ font-family: Arial, sans-serif; background: #f4f4f4; color: #222; }}
    form {{ max-width: 460px; margin: 40px auto; padding: 24px; background: #fff;
           border-radius: 8px; box-shadow: 0 2px 6px rgba(0,0,0,.15); }}
    .campo {{ margin-bottom: 14px; }}
    label {{ display: block; margin-bottom: 4px; font-weight: bold; }}
    input:not([type=radio]):not([type=checkbox]), select {{ width: 100%; padding: 8px; box-sizing: border-box; }}
    .opcao {{ font-weight: normal; }}
    .erro {{ display: block; color: #a40000; margin: 4px 0; }}
    .resumo {{ max-width: 460px; margin: 20px auto 0; }}
    button {{ padding: 10px 20px; cursor: pointer; }}
  </style>
</head>
<body>
  {errorSummary}
  <form id=""form-{formName}"" method=""post"" action=""/{formName}"">
    <h2>Cadastro de {title}</h2>
{fields}    <button type=""submit"">Salvar</button>
    <a href=""/"">Voltar</a>
  </form>
</body>
</html>";
    }

    private static bool IsDecimal(Type propertyType)
    {
        Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        return type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }

    private static string FormatInputValue(Type propertyType, string value)
    {
        Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if ((type == typeof(DateTime) || type == typeof(DateTimeOffset)) &&
            DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dateTime))
            return dateTime.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        return value;
    }
}
