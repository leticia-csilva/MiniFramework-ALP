# MiniFramework

Mini-framework em C# para gerar tabelas SQLite e formulários HTML a partir de
modelos, com uma pequena aplicação web de demonstração.

## Executar

Requer o SDK .NET 10. Na raiz do projeto:

```sh
dotnet run
```

Abra no navegador o endereço local exibido pelo ASP.NET Core. A página inicial
oferece os cadastros de `Curso` e `Aluno`. Cadastre um curso antes de cadastrar
um aluno, pois o campo `CursoId` é uma seleção preenchida com os cursos já
gravados.

Os dados ficam no arquivo `miniframework.db` no diretório de execução. Defina
`MINIFRAMEWORK_DB` para usar outro caminho. Ao
iniciar, o programa gera as tabelas para os modelos registrados em `Program.cs`.
Colunas simples novas são adicionadas a tabelas já existentes; relações novas
em tabelas antigas exigem migração manual ou um banco novo.

## Recursos demonstrados

- `[PrimaryKey]` identifica a chave primária, omitida do formulário.
- `[ForeignKey(typeof(Curso))]` gera uma restrição SQL e uma seleção dos cursos
  existentes.
- `[SelectOptions(...)]` gera um menu suspenso com opções definidas no modelo.
- `[RadioOptions(...)]` gera botões de opção.
- Campos comuns são escolhidos pelo tipo da propriedade, incluindo texto,
  números, datas, horários e booleanos.
- Os dados enviados são convertidos e validados no servidor antes da inserção.
  As instruções de inserção usam parâmetros SQLite.

Os endpoints de cadastro são criados para os tipos listados em `models` no
`Program.cs`. Para incluir outro modelo, registre seu tipo nessa lista e defina
nele o atributo `[PrimaryKey]`.
