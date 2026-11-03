namespace UserService.Domain.Abstactions
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        Task BeginAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
