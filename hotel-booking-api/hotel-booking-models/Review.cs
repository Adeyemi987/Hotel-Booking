﻿using System.ComponentModel.DataAnnotations;

namespace hotel_booking_models
{
    public class Review : BaseEntity
    {
     
        [DataType(DataType.Text)]
        public string Comment { get; set; }
        public string HotelId { get; set; }
        public string CustomerId { get; set; }
        public Hotel Hotel { get; set; }
        public Customer Customer { get; set; }
    }
}