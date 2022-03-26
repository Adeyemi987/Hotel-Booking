﻿namespace hotel_booking_models
{
    public class Amenity : BaseEntity
    {
        public string HotelId  { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; } 
        public decimal Discount { get; set; } 
        public Hotel Hotel { get; set; }
    }
}