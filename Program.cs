using MiniFramework.Models;

Aluno aluno = new Aluno
{
    Id = 1,
    RA = "2612345",
    Nome = "Ana Oliveira",
    Idade = 20,
    Email = "ana.oliveira@email.com"
};

Console.WriteLine($"ID: {aluno.Id}");
Console.WriteLine($"RA: {aluno.RA}");
Console.WriteLine($"Nome: {aluno.Nome}");
Console.WriteLine($"Idade: {aluno.Idade}");
Console.WriteLine($"Email: {aluno.Email}");
