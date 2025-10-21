using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using AISAM.Data.Model;

namespace AISAM.Repositories.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AisamContext _context;

        public PaymentRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            return await _context.Payments
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Subscription)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task<Payment?> GetByOrderIdAsync(string orderId)
        {
            return await _context.Payments
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Subscription)
                .FirstOrDefaultAsync(p => p.TransactionId == orderId && !p.IsDeleted);
        }

        public async Task<List<Payment>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Payments
                .AsNoTracking()
                .Include(p => p.Subscription)
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }
    }
}