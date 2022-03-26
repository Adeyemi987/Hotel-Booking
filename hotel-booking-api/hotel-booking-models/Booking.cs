﻿using System;

namespace hotel_booking_models
{
    public class Booking : BaseEntity
    {
        public string CustomerId { get; set; }
        public string HotelId { get; set; }
        public string BookingReference { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int NoOfPeople { get; set; }
        public string ServiceName { get; set; }
        public Hotel Hotel { get; set; }
        public Customer Customer { get; set; }
        public bool PaymentStatus {  get; set; }
        public string RoomId {  get; set; }
        public Room Room {  get; set; }
        public Payment Payment { get; set; }
    }
}