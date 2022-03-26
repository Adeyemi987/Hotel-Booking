﻿using FluentValidation;
using hotel_booking_dto.AmenityDtos;

namespace hotel_booking_utilities.Validators.AmenityValidators
{
    public class AddAmenityRequestDtoValidator : AbstractValidator<AddAmenityRequestDto>
    {
        public AddAmenityRequestDtoValidator()
        {
            RuleFor(Amenity => Amenity.Name).NotEmpty().WithMessage("Name cannot be empty")
               .NotNull().WithMessage("Name is required")
               .Matches("[A-Za-z]").WithMessage("Name can only contain alphabets")
               .MinimumLength(2).WithMessage("Name is limited to a minimum of 2 characters");

            RuleFor(Amenity => Amenity.Price).NotEmpty().WithMessage("Price cannot be empty")
            .GreaterThan(0);

            RuleFor(Amenity => Amenity.Discount).NotEmpty().WithMessage("Discount cannot be empty")
            .GreaterThan(0);
            RuleFor(Amenity => Amenity.HotelId).NotEmpty().WithMessage("Hotel id cannot be empty").Length(32).WithMessage("Id is incomplete");

        }
    }
}
