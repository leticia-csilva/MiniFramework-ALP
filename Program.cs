using MiniFramework.Generators;
using MiniFramework.Models;

SqlGenerator generator = new SqlGenerator();

Console.WriteLine(generator.GenerateCreateTable(typeof(Curso)));
Console.WriteLine();
Console.WriteLine(generator.GenerateCreateTable(typeof(Aluno)));