﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace hotel_booking_models
{
    public class RoomType : BaseEntity
    {
        public string HotelId { get; set; }
        public string Name { get; set; }

        [DataType(DataType.Text)]
        public string Description { get; set; }
        public decimal Price { get; set; } 
        public decimal Discount { get; set; }
        public string Thumbnail { get; set; }
        public Hotel Hotel { get; set; }
        public ICollection<Room> Rooms { get; set; }
    }
}