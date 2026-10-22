using MiniFramework.Attributes;
namespace MiniFramework.Models;

public class Aluno
{
    [PrimaryKey]
    public int Id {get; set;}

    public string RA {get; set;} = string.Empty;
    public string Nome {get; set;} = string.Empty;
    public int Idade {get; set;}
    public string Email {get; set;} = string.Empty;

    [RadioOptions("Matutino", "Vespertino", "Noturno")]
    public string Periodo {get; set;} = string.Empty;

    [SelectOptions("Ativo", "Trancado")]
    public string Situacao {get; set;} = string.Empty;

    [ForeignKey(typeof(Curso))]
    public int CursoId {get; set;}

}