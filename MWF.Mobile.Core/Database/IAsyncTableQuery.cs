using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MWF.Mobile.Core.Database
{

    public interface IAsyncTableQuery<T> where T : class
    {
        IAsyncTableQuery<T> Where(Expression<Func<T, bool>> predExpr);
        Task<List<T>> ToListAsync(CancellationToken cancellationToken = default(CancellationToken));
    }

}
