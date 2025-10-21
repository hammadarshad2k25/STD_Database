using STD_Database.Database;

namespace STD_Database.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IStudentRepository StdRepo { get; }
        IStudentRepositoryDapper StdRepoDapper { get; }
        IUserRepository UserRepo { get; }
        Task<int> CompleteAsync();
    }
    public class UnitOfWork : IUnitOfWork
    {
        private readonly StudentDB _context;
        public IStudentRepository StdRepo { get; }
        public IStudentRepositoryDapper StdRepoDapper { get; }
        public IUserRepository UserRepo { get; }
        public UnitOfWork(StudentDB context, IStudentRepository stdRepo, IStudentRepositoryDapper stdRepoDapper, IUserRepository userRepo)
        {
            _context = context;
            StdRepo = stdRepo;
            StdRepoDapper = stdRepoDapper;
            UserRepo = userRepo;
        }
        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
