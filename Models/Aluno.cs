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

    [ForeignKey(typeof(Curso))]
    public int CursoId {get; set;}

}