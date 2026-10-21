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

var sqlGenerator = new SqlGenerator();
var htmlGenerator = new HtmlGenerator();
var database = new DatabaseManager(connectionString);

// A tabela referenciada deve ser criada antes da tabela que contém a chave estrangeira.
foreach (Type model in new[] { typeof(Curso), typeof(Aluno) })
{
    string sql = sqlGenerator.GenerateCreateTable(model);
    Console.WriteLine($"--- SQL para {model.Name} ---\n{sql}\n");
    database.ExecuteSql(sql);
}

Directory.CreateDirectory("formularios");
File.WriteAllText(Path.Combine("formularios", "curso.html"), htmlGenerator.GenerateForm(typeof(Curso)));
File.WriteAllText(Path.Combine("formularios", "aluno.html"), htmlGenerator.GenerateForm(typeof(Aluno)));
Console.WriteLine("Formulários gerados em ./formularios.");
Console.WriteLine("Tabelas presentes no banco:");
database.ShowTables();
