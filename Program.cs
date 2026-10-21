using MiniFramework.Database;
using MiniFramework.Generators;
using MiniFramework.Models;

SqlGenerator generator = new SqlGenerator();

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

