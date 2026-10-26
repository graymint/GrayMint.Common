namespace GrayMint.Common.Generics;

public class IdName
{
    public static IdName<T> Create<T>(T id, string name)
    {
        return new IdName<T>(id, name);
    }
}