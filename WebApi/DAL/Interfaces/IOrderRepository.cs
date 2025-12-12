using System;

public interface IOrderRepository
{
    Task<V1OrderDal[]> BulkInsert(V1OrderDal[] model, CancellationToken token);

    Task<V1OrderDal[]> Query(QueryOrdersDalModel model, CancellationToken token);

    Task UpdateStatus(long[] ids, string newStatus, DateTimeOffset updatedAt, CancellationToken token);
}
