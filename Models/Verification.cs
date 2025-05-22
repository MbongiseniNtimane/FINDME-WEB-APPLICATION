using System.ComponentModel.DataAnnotations;

namespace ASPNETCore_DB.Models
{
    
        public class ResendVerificationEmailViewModel
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid Email Address.")]
            public string Email { get; set; }
        }
    
}
