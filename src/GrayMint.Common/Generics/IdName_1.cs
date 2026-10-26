namespace GrayMint.Common.Generics;

public class IdName<T>(T id, string name)
{
    public T Id { get; set; } = id;
    public string Name { get; set; } = name;
}
