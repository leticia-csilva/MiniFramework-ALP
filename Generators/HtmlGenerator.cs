namespace MiniFramework.Generators;
using System.Net;
using System.Reflection;
using System.Text;
using MiniFramework.Core;
using MiniFramework.Attributes;

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
            $"Tipo não suportado no formulário: {propertyType.Name}"
        );
    }

    //number precisa de step="any" por causa do decimal
    private bool IsDecimal(Type propertyType)
    {
        Type type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        return type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }

    private string GenerateEnumField(PropertyInfo property, Type enumType, string name)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(" <div class=\"campo\">");

        if (Attribute.IsDefined(property, typeof(RadioAttribute)))
        {
            sb.AppendLine($"  <span class=\"rotulo\">{name}</span>");
            foreach (string option in Enum.GetNames(enumType))
            {
                string value = WebUtility.HtmlEncode(option);
                sb.AppendLine($"  <label class=\"opcao\"><input type=\"radio\" name=\"{name}\" value=\"{value}\" required> {value}</label>");
            }
        }
        else
        {
            sb.AppendLine($"  <label for=\"{name}\">{name}</label>");
            sb.AppendLine($"  <select id=\"{name}\" name=\"{name}\" required>");
            sb.AppendLine("   <option value=\"\">Selecione...</option>");
            foreach (string option in Enum.GetNames(enumType))
            {
                string value = WebUtility.HtmlEncode(option);
                sb.AppendLine($"   <option value=\"{value}\">{value}</option>");
            }
            sb.AppendLine("  </select>");
        }

        sb.AppendLine(" </div>");
        return sb.ToString();
    }

    public string GenerateForm(Type modelType)
    {
        ModelReader reader = new ModelReader();
        PropertyInfo[] properties = reader.ReadModel(modelType);
        string formName = modelType.Name.ToLower();
        StringBuilder fields = new StringBuilder();
        foreach (PropertyInfo property in properties)
        {
            //usuário não precisa preencher a chave primária
            if (Attribute.IsDefined(property, typeof(PrimaryKeyAttribute)))
                continue;

            string name = WebUtility.HtmlEncode(property.Name);

            // enum nao e input simples: vira select ou radio
            Type realType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (realType.IsEnum)
            {
                fields.Append(GenerateEnumField(property, realType, name));
                continue;
            }

            string inputType = MapInputType(property.PropertyType);
            string extra = IsDecimal(property.PropertyType) ? " step=\"any\"" : "";
            bool nullable = Nullable.GetUnderlyingType(property.PropertyType) != null;
            string required = (!nullable && inputType != "checkbox") ? " required" : "";
            string label = name;
            ForeignKeyAttribute? foreignKey = property.GetCustomAttribute<ForeignKeyAttribute>();
            if (foreignKey != null)
            {
                string referenced = WebUtility.HtmlEncode(foreignKey.ReferencedModel.Name);
                label = $"{name} <small>(Id de {referenced})</small>";
                extra += " min=\"1\"";
            }
            fields.AppendLine(" <div class=\"campo\">");
            fields.AppendLine($" <label for=\"{name}\">{label}</label>");
            fields.AppendLine($"  <input type=\"{inputType}\" id=\"{name}\" name=\"{name}\"{extra}{required}>");
            fields.AppendLine(" </div>");
        }
        string title = WebUtility.HtmlEncode(modelType.Name);

        return $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>Cadastro de {title}</title>
  <style>
    body {{ font-family: Arial, sans-serif; background: #f4f4f4; }}
    form {{ max-width: 420px; margin: 40px auto; padding: 24px; background: #fff;
           border-radius: 8px; box-shadow: 0 2px 6px rgba(0,0,0,.15); }}
    .campo {{ margin-bottom: 14px; }}
    label {{ display: block; margin-bottom: 4px; font-weight: bold; }}
    input[type=text], input[type=number], input[type=date], input[type=time],
    input[type=datetime-local], select {{ width: 100%; padding: 8px; box-sizing: border-box; }}
    .rotulo {{ display: block; margin-bottom: 4px; font-weight: bold; }}
    label.opcao {{ display: inline-block; font-weight: normal; margin-right: 12px; }}
    button {{ padding: 10px 20px; cursor: pointer; }}
  </style>
</head>
<body>
  <form id=""form-{formName}"" method=""post"" action=""/{formName}"">
    <h2>Cadastro de {title}</h2>
{fields}    <button type=""submit"">Salvar</button>
  </form>
</body>
</html>
";
    }
}
