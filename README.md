# MiniFramework

Mini-framework em C# para gerar tabelas MySQL e formulários HTML a partir dos
modelos, com uma aplicação web simples para demonstrar cadastros.

## Requisitos e execução

- SDK .NET 10
- Servidor MySQL acessível e um banco já criado
- Usuário MySQL com permissão para criar tabelas e inserir registros

Configure a variável de ambiente `MINIFRAMEWORK_MYSQL` com a connection string
do banco e execute na raiz do projeto:

```sh
dotnet run --urls http://localhost:5000
```

Exemplo de configuração sem colocar credenciais no código:

```sh
export MINIFRAMEWORK_MYSQL='Server=localhost;Port=3306;Database=miniframework;User ID=seu_usuario;Password=sua_senha'
```

Abra `http://localhost:5000`. Cadastre um curso antes de cadastrar um aluno:
o campo `CursoId` apresenta os cursos já existentes no MySQL.

## Funcionalidades

- Gera tabelas MySQL com chaves primárias e estrangeiras a partir dos modelos.
- Gera campos de texto, números, datas, horários e booleanos.
- Gera menus suspensos para enums e chaves estrangeiras, e radio buttons para
  propriedades enum marcadas com `[Radio]`.
- Valida campos obrigatórios, valores enum e a existência do registro
  referenciado antes de inserir.
- Preserva os dados preenchidos quando uma validação falha.
- Executa inserções parametrizadas através do `MySqlConnector`.

Os modelos disponíveis e suas rotas são registrados em `Program.cs`. Ao adicionar
um modelo, inclua-o na lista `models`, defina uma propriedade `[PrimaryKey]` e
crie as relações/enumerações necessárias.
