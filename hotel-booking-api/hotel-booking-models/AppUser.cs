﻿  using Microsoft.AspNetCore.Identity;
  using System;
  using System.Collections.Generic;
  using System.Text.Json.Serialization;

namespace hotel_booking_models
{
    public class AppUser : IdentityUser

    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string PublicId { get; set; }
        public string Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Manager Manager { get; set; }
        public Customer Customer { get; set; }
        public Guid RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}
