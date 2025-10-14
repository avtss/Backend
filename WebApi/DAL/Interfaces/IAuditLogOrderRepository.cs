using System.Threading;
using System.Threading.Tasks;
using WebApi.DAL.Models;

namespace WebApi.DAL.Interfaces;

public interface IAuditLogOrderRepository
{
    Task<V1AuditLogOrderDal[]> BulkInsert(V1AuditLogOrderDal[] model, CancellationToken token);

    Task<V1AuditLogOrderDal[]> Query(QueryAuditLogOrderDalModel model, CancellationToken token);
}
