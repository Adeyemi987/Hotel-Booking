using hotel_booking_dto.ManagerDtos;
using hotel_booking_models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hotel_booking_data.Repositories.Abstractions
{
    public interface IManagerRequestRepository : IGenericRepository<ManagerRequest>
    {
        Task<ManagerRequest> GetHotelManagerRequestByEmail(string email);
        Task<ManagerRequest> GetHotelManagerByEmailToken(string email, string token);
        Task<IEnumerable<ManagerRequest>> GetManagerRequest();
    }
}
