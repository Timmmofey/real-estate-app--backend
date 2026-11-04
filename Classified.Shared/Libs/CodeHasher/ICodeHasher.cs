namespace Classified.Shared.Libs.CodeHasher
{
    public interface ICodeHasher
    {
        string Hash(string code);

        bool Verify(string code, string hash);
    }
}
