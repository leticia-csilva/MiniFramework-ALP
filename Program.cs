using System;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using MiniFramework.Database;
using MiniFramework.Generators;
using MiniFramework.Models;

Console.WriteLine("Iniciando o Mini-Framework...");

// 1. Criação do Banco de Dados
SqlGenerator generator = new SqlGenerator();
DatabaseManager database = new DatabaseManager("miniframework.db");

string cursoSql = generator.GenerateCreateTable(typeof(Curso));
string alunoSql = generator.GenerateCreateTable(typeof(Aluno));

database.ExecuteSql(cursoSql);
database.ExecuteSql(alunoSql);

// Cadastra cursos base para a chave estrangeira do Aluno funcionar
database.ExecuteSql("INSERT OR IGNORE INTO Curso (Id, Nome) VALUES (1, 'Ciência da Computação');");
database.ExecuteSql("INSERT OR IGNORE INTO Curso (Id, Nome) VALUES (2, 'Sistemas de Informação');");

Console.WriteLine("Tabelas criadas com sucesso!");

// 2. Configurando o Servidor Web Local
using HttpListener listener = new HttpListener();
string url = "http://localhost:8080/";
listener.Prefixes.Add(url);
listener.Start();

Console.WriteLine($"\nServidor rodando! Escutando requisições na porta 8080...");

// 3. Tenta abrir o navegador automaticamente (Linux/Debian)
try 
{
    Process.Start(new ProcessStartInfo 
    { 
        FileName = "xdg-open", 
        Arguments = url + "aluno", 
        UseShellExecute = false 
    });
} 
catch 
{
    Console.WriteLine("Abra manualmente no navegador: http://localhost:8080/aluno");
}

HtmlGenerator htmlGen = new HtmlGenerator();

// 4. Loop infinito para manter o servidor no ar e processar o HTML
while (true)
{
    HttpListenerContext context = listener.GetContext();
    HttpListenerRequest request = context.Request;
    HttpListenerResponse response = context.Response;

    string path = request.Url?.AbsolutePath ?? "/";

    if (request.HttpMethod == "GET")
    {
        // Se acessar localhost:8080 certinho na rota do Aluno
        if (path == "/aluno")
        {
            // Usa o seu HtmlGenerator para cuspir a página
            string html = htmlGen.GenerateForm(typeof(Aluno));
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(html);
            
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        else
        {
            // Se acessar a raiz (localhost:8080), redireciona pra rota certa pra não dar erro 404
            response.Redirect("/aluno");
        }
    }
    // Rota POST acionada quando você clica no botão "Salvar" do seu form HTML
    else if (request.HttpMethod == "POST" && path == "/aluno")
    {
        using StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding);
        string dadosPost = reader.ReadToEnd();
        
        // Quebra a string "Nome=Joao&Idade=20" em pedaços e decodifica caracteres especiais
        var dicionario = new Dictionary<string, string>();
        foreach (var par in dadosPost.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = par.Split('=');
            if (kv.Length == 2)
            {
                string chave = WebUtility.UrlDecode(kv[0]);
                string valor = WebUtility.UrlDecode(kv[1]).Replace("+", " ");
                dicionario[chave] = valor;
            }
        }

        // Monta o INSERT dinamicamente!
        string colunas = string.Join(", ", dicionario.Keys);
        string valores = string.Join(", ", dicionario.Values.Select(v => $"'{v}'"));
        string insertSql = $"INSERT INTO Aluno ({colunas}) VALUES ({valores});";
        
        try
        {
            database.ExecuteSql(insertSql);
            Console.WriteLine($"\n[SALVO NO BANCO]: {insertSql}");
            
            string sucessoHtml = @"
                <!DOCTYPE html>
                <html lang='pt-BR'>
                <head><meta charset='UTF-8'><style>body{font-family: Arial; padding: 20px;}</style></head>
                <body>
                    <h2 style='color: green;'>Salvo com sucesso!</h2>
                    <a href='/aluno' style='padding: 10px; background: #007bff; color: white; text-decoration: none; border-radius: 4px;'>Voltar para o formulário</a>
                </body>
                </html>";
            
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(sucessoHtml);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            string erroHtml = $"<h2>Erro no banco SQLite:</h2><p>{ex.Message}</p><a href='/aluno'>Voltar</a>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(erroHtml);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
    }
    
    response.OutputStream.Close();
}