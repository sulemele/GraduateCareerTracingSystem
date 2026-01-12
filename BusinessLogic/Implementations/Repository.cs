
using BusinessLogic.Interfaces;
using DataAccess.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BusinessLogic.Implementations
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly DatabaseEntity context;
        private readonly DbSet<T> table = null;

        public Repository(DatabaseEntity _context)
        {
            context = _context;
            table = context.Set<T>();
        }
        public bool Add(T t)
        {
            table.Add(t);
            return Save();
        }

        public bool AddRange(IEnumerable<T> entities)
        {
            table.AddRange(entities); // Optimized batch insert
            return Save();
        }


        public bool Delete(T t)
        {
            table.Remove(t);
            return Save();
        }


        public async Task<IEnumerable<T>> GetAll()
        {
            return await table.ToListAsync();
        }

        public IQueryable<T> GetAllQ()
        {
            return table.AsQueryable();
        }


        public bool Save()
        {
            var isSaved = context.SaveChanges();
            return isSaved > 0;
        }


        public bool Update(T t)
        {
            table.Attach(t);
            context.Entry(t).State = EntityState.Modified;
            return Save();
        }

        public async Task<T> GetByIdAsync(Expression<Func<T, bool>> wherecondition)
        {
            T? t = await table.Where(wherecondition).FirstOrDefaultAsync();
            return t;
        }

        public async Task<IEnumerable<T>> GetByQueryAsync(Expression<Func<T, bool>> wherecondition)
        {
           IEnumerable<T?> t = await table.Where(wherecondition).ToListAsync();
            return t;
        }

       


        public async Task<T> GetById(int id)
        {
            T? t = await table.FindAsync(id);
            return t;
        }

        public async Task<T> GetByIdString(string id)
        {
            T? t = await table.FindAsync(id);
            return t;
        }

        public async Task<T> GetUserByIdNoTracking(Expression<Func<T, bool>> wherecondition)
        {
            T? t = await table.Where(wherecondition).AsNoTracking().FirstOrDefaultAsync();
            return t;
        }


        public async Task<IEnumerable<T>> GetByQueryAsyncNew(Expression<Func<T, bool>> wherecondition)
        {
            // Replace AndAlso with &&
            var modifiedCondition = ReplaceAndAlsoWithAndAlso(wherecondition);

            IEnumerable<T> result = await table.Where(modifiedCondition).ToListAsync();
            return result;
        }

        private Expression<Func<T, bool>> ReplaceAndAlsoWithAndAlso(Expression<Func<T, bool>> expression)
        {
            var parameter = expression.Parameters.First();
            var body = new AndAlsoVisitor().Visit(expression.Body);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

      
        private class AndAlsoVisitor : ExpressionVisitor
        {
            protected override Expression VisitBinary(BinaryExpression node)
            {
                if (node.NodeType == ExpressionType.AndAlso)
                {
                    return Expression.MakeBinary(ExpressionType.And, Visit(node.Left), Visit(node.Right));
                }
                return base.VisitBinary(node);
            }
        }
    }
}
