namespace EmployeeSelfServiceApi.Repositories.Interfaces;

public interface IGenericRepository<TEntity>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Delete(TEntity entity);
}