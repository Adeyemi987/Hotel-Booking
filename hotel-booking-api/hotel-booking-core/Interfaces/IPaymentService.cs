using hotel_booking_models;
using System.Threading.Tasks;

namespace hotel_booking_core.Interfaces
{
    public interface IPaymentService
    {
        Task<string> InitializePayment(decimal amount, Customer customer, string paymentService, string bookingId, string transactionRef, string redirect_url);
        Task<bool> VerifyTransaction(string transactionRef, string paymentMethod, string transactionId = null);
    }
}
