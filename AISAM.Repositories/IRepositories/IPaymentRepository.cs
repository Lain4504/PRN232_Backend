using AISAM.Data.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AISAM.Repositories.IRepositories
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(Guid id);
        Task<Payment?> GetByOrderIdAsync(string orderId);
        Task<List<Payment>> GetByUserIdAsync(Guid userId);
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment> UpdateAsync(Payment payment);
    }
}