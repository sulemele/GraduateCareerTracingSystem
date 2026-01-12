using System.Linq.Expressions;

namespace BusinessLogic.Interfaces
{

    public interface IRepository<T> where T : class
    {
        
        Task<IEnumerable<T>> GetAll();
        IQueryable<T> GetAllQ();
        Task<T> GetByIdAsync(Expression<Func<T, bool>> wherecondition);
        Task<IEnumerable<T>> GetByQueryAsync(Expression<Func<T, bool>> wherecondition);
        Task<IEnumerable<T>> GetByQueryAsyncNew(Expression<Func<T, bool>> wherecondition);
        bool Add(T t);
        bool AddRange(IEnumerable<T> entities);
        bool Update(T t);
        bool Delete(T t);
        bool Save();
        Task<T> GetById(int id);
        Task<T> GetByIdString(string id);
        Task<T> GetUserByIdNoTracking(Expression<Func<T, bool>> wherecondition);
    }
}
