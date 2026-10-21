using MiniFramework.Attributes;

namespace MiniFramework.Models;

public class Curso
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;
}