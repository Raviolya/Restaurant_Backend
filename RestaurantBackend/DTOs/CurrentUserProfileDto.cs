﻿using System;

namespace RestaurantBackend.DTOs
{
    public class CurrentUserProfileDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string Phone { get; set; } 
    }
}