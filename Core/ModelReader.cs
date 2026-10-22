using System.Reflection; // O "Reflection" é um recurso que permite
// inspecionar classes, propriedades, tipos e outros elementos do 
// programa durante a execução.
namespace MiniFramework.Core;

public class ModelReader
{
    public PropertyInfo[] ReadModel(Type modelType) 
    // o tipo "Type" é genérico e vale para qualquer classe.
    {
        PropertyInfo[] properties = modelType.GetProperties();
        // PropertyInfo é um tipo definido da linguagem para consultar informações
        // de uma classe. O método GetProperties pega as propriedades públicas da classe.

        return properties;// Retorna um array de objetos PropertyInfo.
    }
}
