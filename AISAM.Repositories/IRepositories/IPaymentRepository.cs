using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IPaymentRepository
    {
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment?> GetByIdAsync(Guid id);
        Task<Payment?> GetByTransactionIdAsync(string transactionId);
        Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId);
        Task UpdateAsync(Payment payment);
        Task DeleteAsync(Guid id);
    }
}