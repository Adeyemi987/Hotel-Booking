﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hotel_booking_dto.AmenityDtos
{
    public class UpdateAmenityDto
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
    }
}
