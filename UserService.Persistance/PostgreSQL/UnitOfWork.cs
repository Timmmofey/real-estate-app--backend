using Microsoft.EntityFrameworkCore.Storage;
using UserService.Domain.Abstactions;

namespace UserService.Persistance.PostgreSQL
{
    //public class EfUnitOfWork : IUnitOfWork
    //{
    //    private readonly UserServicePostgreDbContext _context;
    //    private IDbContextTransaction? _transaction;

    //    public EfUnitOfWork(UserServicePostgreDbContext context)
    //    {
    //        _context = context;
    //    }

    //    public async Task BeginAsync()
    //    {
    //        _transaction = await _context.Database.BeginTransactionAsync();
    //    }

    //    public async Task CommitAsync()
    //    {
    //        await _context.SaveChangesAsync();
    //        if (_transaction != null)
    //            await _transaction.CommitAsync();
    //    }

    //    public async Task RollbackAsync()
    //    {
    //        if (_transaction != null)
    //            await _transaction.RollbackAsync();
    //    }
    //}

    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly UserServicePostgreDbContext _context;
        private IDbContextTransaction? _transaction;

        public EfUnitOfWork(UserServicePostgreDbContext context)
        {
            _context = context;
        }

        public async Task BeginAsync()
        {
            // Начинаем транзакцию один раз
            if (_transaction == null)
                _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            await _context.SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}
