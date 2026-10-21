using System.Reflection;
using MiniFramework.Core;
using MiniFramework.Models;

ModelReader reader = new ModelReader();

PropertyInfo[] properties = reader.ReadModel(typeof(Aluno));

foreach (PropertyInfo property in properties)
{
    Console.WriteLine($"Propriedade: {property.Name}");
    Console.WriteLine($"Tipo: {property.PropertyType.Name}\n");
}