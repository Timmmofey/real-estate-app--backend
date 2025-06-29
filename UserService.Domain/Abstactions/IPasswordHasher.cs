namespace UserService.Domain.Abstactions
{
    public interface IPasswordHasher
    {
        public string Generate(string password);
        public bool Verify(string password, string hashedPassword);

    }
}
