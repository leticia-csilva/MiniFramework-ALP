using MiniFramework.Database;
using MiniFramework.Generators;
using MiniFramework.Models;

SqlGenerator generator = new SqlGenerator();
HtmlGenerator htmlGenerator = new HtmlGenerator();

DatabaseManager database = new DatabaseManager("miniframework.db");

// Gera os comandos SQL automaticamente
string cursoSql = generator.GenerateCreateTable(typeof(Curso));
string alunoSql = generator.GenerateCreateTable(typeof(Aluno));

// Exibe os comandos gerados
Console.WriteLine(cursoSql);
Console.WriteLine();
Console.WriteLine(alunoSql);

// Executa os comandos no SQLite
database.ExecuteSql(cursoSql);
database.ExecuteSql(alunoSql);

Console.WriteLine("\nTabelas criadas com sucesso!");
Console.WriteLine("\nTabelas existentes no banco:");
database.ShowTables();

// Gera os formularios HTML a partir dos mesmos modelos
Directory.CreateDirectory("formularios");
File.WriteAllText("formularios/curso.html", htmlGenerator.GenerateForm(typeof(Curso)));
File.WriteAllText("formularios/aluno.html", htmlGenerator.GenerateForm(typeof(Aluno)));

Console.WriteLine("\nFormularios gerados em ./formularios (curso.html e aluno.html)");
