using AutoMapper;
using hotel_booking_core.Interface;
using hotel_booking_core.Interfaces;
using hotel_booking_data.UnitOfWork.Abstraction;
using hotel_booking_dto;
using hotel_booking_dto.HotelDtos;
using hotel_booking_dto.ManagerDtos;
using hotel_booking_models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using hotel_booking_models.Mail;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Transactions;
using hotel_booking_dto.CustomerDtos;
using hotel_booking_dto.BookingDtos;
using hotel_booking_utilities.Pagination;
using hotel_booking_dto.commons;

namespace hotel_booking_core.Services
{
    public class ManagerService : IManagerService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMailService _mailService;
        private readonly ILogger _logger;
        private readonly UserManager<AppUser> _userManager;

        public ManagerService(IMapper mapper, IUnitOfWork unitOfWork, 
            IMailService mailService, ILogger logger, UserManager<AppUser> userManager)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _mailService = mailService;
            _logger = logger;
        }

        public async Task<Response<ManagerResponseDto>> AddManagerAsync(ManagerDto managerDto)
        {
            var getPotentialManager = await _unitOfWork.ManagerRequest.GetHotelManagerByEmailToken(email: managerDto.BusinessEmail, token: managerDto.Token);
            var expired = getPotentialManager.ExpiresAt < DateTime.Now.AddMinutes(-5);
            if (expired)
            {
                return Response<ManagerResponseDto>.Fail("Link has expired", StatusCodes.Status405MethodNotAllowed);
            }
            var appUser = _mapper.Map<AppUser>(managerDto);
            var manager = _mapper.Map<Manager>(managerDto);
            var hotel = _mapper.Map<Hotel>(managerDto);
            manager.AppUserId = appUser.Id;
            hotel.ManagerId = manager.AppUserId;
            appUser.Manager = manager;
            manager.Hotels = new List<Hotel>() { hotel };
            var result = await _userManager.CreateAsync(appUser, managerDto.Password);
                
            if (result.Succeeded)
            {
                var managerResponse = _mapper.Map<ManagerResponseDto>(manager);

                var response = new Response<ManagerResponseDto>()
                {
                    StatusCode = StatusCodes.Status200OK,
                    Succeeded = true,
                    Data = managerResponse,
                    Message = $"{manager.CompanyName} hotel with ID: {manager.AppUserId}: registered successfully"
                };
                return response;
            }
            return Response<ManagerResponseDto>.Fail("Registration Failed", StatusCodes.Status400BadRequest);
        }

        public async Task<Response<IEnumerable<HotelBasicDto>>> GetAllHotelsAsync(string managerId)
        {
            var hotelList = await _unitOfWork.Managers.GetAllHotelsForManagerAsync(managerId);
            var hotelListDto = _mapper.Map<IEnumerable<HotelBasicDto>>(hotelList);
            var response = new Response<IEnumerable<HotelBasicDto>>(StatusCodes.Status200OK, true, "hotels for manager", hotelListDto);
            return response;
        }

        public async Task<Response<string>> ActivateManager(string managerId)
        {
            Manager manager = await _unitOfWork.Managers.GetManagerAsync(managerId);
            var response = new Response<string>();
            if (manager != null)
            {
                if (manager.AppUser.IsActive == false)
                {
                    manager.AppUser.IsActive = true;
                    _unitOfWork.Managers.Update(manager);
                    await _unitOfWork.Save();

                    response.Message = $"Manager activated successfully";
                    response.StatusCode = (int)HttpStatusCode.NoContent;
                    response.Succeeded = true;
                    return response;
                }

                response.Message = $"Manager is already active.";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Succeeded = false;

                return response;
            }
            response.Message = $"User is not a manager.";
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Succeeded = false;

            return response;

        }

        public async Task<Response<PageResult<IEnumerable<HotelManagersDto>>>> GetAllHotelManagersAsync(PagingDto paging)
        {
            var hotelManagers = _unitOfWork.Managers.GetHotelManagersAsync();
            var item = await hotelManagers.PaginationAsync<Manager, HotelManagersDto>(paging.PageSize, paging.PageNumber, _mapper);
            return Response<PageResult<IEnumerable<HotelManagersDto>>>.Success("Success", item); ;
        }

        public async Task<Response<string>> UpdateManager(string managerId, UpdateManagerDto updateManager)
        {
            var response = new Response<string>();

            var manager = await _unitOfWork.Managers.GetManagerAsync(managerId);

            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                if (manager != null)
                {
                    // Update user details in AspNetAppUser table
                    _logger.Information($"Attempting to update app user with Id {managerId}  in the user table");
                    var user = await _userManager.FindByIdAsync(managerId);

                    var userUpdateResult = await UpdateUser(user, updateManager);

                    if (userUpdateResult.Succeeded)
                    {
                       
                        _logger.Information($"Attempting to update manager with Id {managerId}  in the manager table");
                        manager.CompanyName = updateManager.CompanyName;
                        manager.CompanyAddress = updateManager.CompanyAddress;
                        manager.BusinessEmail = updateManager.BusinessEmail;
                        manager.State = updateManager.State;
                        manager.AccountName = updateManager.AccountName;
                        manager.AccountNumber = updateManager.AccountNumber;


                        _unitOfWork.Managers.Update(manager);
                        await _unitOfWork.Save();

                        _logger.Information($" manager with Id {managerId} updated in the manager table");
                        response.Message = "Update Successful";
                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.Succeeded = true;
                        response.Data = managerId;
                        transaction.Complete();
                        return response;
                    }

                    transaction.Dispose();
                    _logger.Information($"Unable to update manager with Id {managerId} in the manager table");
                    response.Message = "Unable to update app user table";
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Succeeded = false;
                    return response;
                }
                _logger.Information($"Manager with Id  {managerId} not found the app user table");
                response.Message = "Manager Not Found";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Succeeded = false;
                transaction.Complete();
                return response;
            }


        }
        private async Task<IdentityResult> UpdateUser(AppUser user, UpdateManagerDto model)
        {
           
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Age = model.Age;

            return await _userManager.UpdateAsync(user);
        }

        public async Task<Response<string>> SoftDeleteManagerAsync(string managerId)
        {
            Manager manager = await _unitOfWork.Managers.GetManagerAsync(managerId);
            Response<string> response = new();

            if (manager != null)
            {
                if (manager.AppUser.IsActive == true)
                {
                    manager.AppUser.IsActive = false;
                    _unitOfWork.Managers.Update(manager);
                    await _unitOfWork.Save();

                    response.Message = $"Manager with {manager.AppUser.Id} has been deactivated successfully.";
                    response.StatusCode = (int)HttpStatusCode.Created;
                    response.Succeeded = true;

                    return response;
                }

                response.Message = $"Attention, manager with {manager.AppUser.Id} is already inactive.";
                response.StatusCode = (int)HttpStatusCode.AlreadyReported;
                response.Succeeded = false;

                return response;

            }
            response.Message = $"Sorry, user with {managerId} is not a manager.";
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Succeeded = false;

            return response;
    } 

        public async Task<Response<string>> AddManagerRequest(ManagerRequestDto managerRequest)
        {
            var getManager = await _unitOfWork.ManagerRequest.GetHotelManagerRequestByEmail(managerRequest.Email);
            var getUser = await _unitOfWork.Managers.GetAppUserByEmail(managerRequest.Email);

            if (getUser == null && getManager == null)
            {
                var addManager = _mapper.Map<ManagerRequest>(managerRequest);
                addManager.Token = Guid.NewGuid().ToString();
                await _unitOfWork.ManagerRequest.InsertAsync(addManager);
                await _unitOfWork.Save();

                return new Response<string>
                {
                    Message = "Thank you for your interest, you will get a response from us shortly",
                    StatusCode = StatusCodes.Status200OK,
                    Succeeded = true
                };
            }
            return Response<string>.Fail("Email already exist", StatusCodes.Status409Conflict);
        }

        public async Task<Response<bool>> SendManagerInvite(string email)
        {
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var check = await _unitOfWork.ManagerRequest.GetHotelManagerRequestByEmail(email);
            var getUser = await _unitOfWork.Managers.GetAppUserByEmail(email);

            var result = false;
            
                if (getUser == null && check != null)
                {
                    var newGuid = Guid.Parse(check.Token);
                    var mailBody = await GetEmailBody(emailTempPath: "StaticFiles/Html/ManagerInvite.html", token: Encode(newGuid), email);

                    var mailRequest = new MailRequest()
                    {
                        Subject = "Request Approved",
                        Body = mailBody,
                        ToEmail = check.Email
                    };

                    result = await _mailService.SendEmailAsync(mailRequest);
                    if (result)
                    {
                        check.ExpiresAt = DateTime.UtcNow.AddHours(24);
                        _unitOfWork.ManagerRequest.Update(check);
                        await _unitOfWork.Save();

                        _logger.Information("Mail sent successfully");
                        transaction.Complete();
                        return Response<bool>.Success("Mail sent successfully", true, StatusCodes.Status200OK);
                    }
                    _logger.Information("Mail service failed");
                    transaction.Dispose();
                    return Response<bool>.Fail("Mail service failed", StatusCodes.Status400BadRequest);
                }
                transaction.Dispose();
                _logger.Information("Invalid email address");
            return Response<bool>.Fail("Invalid email address", StatusCodes.Status409Conflict);
            
        }

        public async Task<Response<bool>> CheckTokenExpiring(string email, string token)
        {
            token = Decode(token).ToString();
            var managerRequest = await _unitOfWork.ManagerRequest.GetHotelManagerByEmailToken(email, token);
            var getUser = await _unitOfWork.Managers.GetAppUserByEmail(email);

            if (managerRequest != null)
            {
                if (getUser == null)
                {
                    var expired = managerRequest.ExpiresAt < DateTime.Now;
                    if (expired)
                    {
                        var resendMail = await SendManagerInvite(email);
                        if (resendMail.Succeeded)
                        {
                            return Response<bool>.Fail("Link has expired, a new link has been sent", StatusCodes.Status408RequestTimeout);
                        }
                        return Response<bool>.Fail("Weak or no internet access, please try again", StatusCodes.Status408RequestTimeout);
                    }
                    return Response<bool>.Success("Redirecting to registration page", true, StatusCodes.Status200OK);
                }
                return Response<bool>.Fail("This User has been registered already", StatusCodes.Status409Conflict);
            }
            return Response<bool>.Fail("Invalid email or token", StatusCodes.Status404NotFound);
        }

        public async Task<Response<IEnumerable<ManagerRequestResponseDTo>>> GetAllManagerRequest()
        {
            var getAllManagersRequest = await _unitOfWork.ManagerRequest.GetManagerRequest();
            
            var mapResponse = _mapper.Map<List<ManagerRequestResponseDTo>>(getAllManagersRequest);

            return Response<IEnumerable<ManagerRequestResponseDTo>>
                .Success("All manager requests", mapResponse, StatusCodes.Status200OK); 
        }

        private static async Task<string> GetEmailBody(string emailTempPath, string token, string email)
        {
            var link = $"https://hoteldotnet.herokuapp.com/Manager/validate-email?{email}&token={token}";
            var temp = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), emailTempPath));
            var emailBody = temp.Replace("**link**", link);
            return emailBody;
        }

        private string Encode(Guid guid)
        {
            string encoded = Convert.ToBase64String(guid.ToByteArray());
            encoded = encoded.Replace("/", "_").Replace("+", "-").Replace("=", "");
            return encoded;
        }

        private Guid Decode(string value)
        {
            Guid buffer = default;
            value = value.Replace("_", "/").Replace("-", "+") + "==";
            try
            {
                buffer = new Guid(Convert.FromBase64String(value));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return buffer;
        }

        public async Task<Response<IEnumerable<TopManagerCustomers>>> GetManagerTopCustomers(string managerId)
        {
            var result = await _unitOfWork.Customers.GetTopCustomerForManagerAsync(managerId);
            var dtos = _mapper.Map<IEnumerable<TopManagerCustomers>>(result);
            var response = new Response<IEnumerable<TopManagerCustomers>>(StatusCodes.Status200OK,true,"Top Customers for Manager", dtos);
            return response;
        }

        public async Task<Response<PageResult<IEnumerable<BookingResponseDto>>>> GetManagerBookings(string managerId, int pageSize, int pageNumber)
        {
            var manager = await _unitOfWork.Managers.GetManagerAsync(managerId);
            if(manager == null)
            {
                return Response<PageResult<IEnumerable<BookingResponseDto>>>.Fail("Manager not found");
            }
            var result = _unitOfWork.Booking.GetBookingsByManagerId(managerId);
            var response = await result.PaginationAsync<Booking, BookingResponseDto>(pageSize, pageNumber, _mapper);
            return Response<PageResult<IEnumerable<BookingResponseDto>>>.Success("Bookings Fetched", response, StatusCodes.Status200OK);
        }
    }
}
