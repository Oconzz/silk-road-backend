using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.ViewModel
{
    public class RegisterVM
    {
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string email { get; set; }

        [Required]
        public string password { get; set; }

        [Required]
        [Compare("password", ErrorMessage = "The password and confirmation password do not match")]
        public string confirmPassword { get; set; }

        public string fullName { get; set; }
        public string phone { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zipcode { get; set; }
        public string country { get; set; }
        public string company { get; set; }
        public string? personalPhoto { get; set; }
        public string? companyLogo { get; set; }  // Company logo URL or base64
        public string? category { get; set; }  // Business category (e.g., "Painting", "Roofing", etc.)
    }
}
