﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace hotel_booking_models
{
    public class Hotel : BaseEntity
    {
        public string ManagerId { get; set; }
        public string Name { get; set; }

        [DataType(DataType.Text)]
        public string Description { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public Manager Manager { get; set; }
        public ICollection<WishList> WishLists { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<Rating> Ratings { get; set; }
        public ICollection<RoomType> RoomTypes { get; set; }
        public ICollection<Amenity> Amenities { get; set; }
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<Gallery> Galleries { get; set; }
    }
}