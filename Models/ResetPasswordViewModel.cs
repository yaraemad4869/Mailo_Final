using System.ComponentModel.DataAnnotations;

namespace Mailo.Models
{
    public class ResetPasswordViewModel
    {

      
            [Required]
            public string? Token { get; set; }
            [Required]
            [DataType(DataType.Password)]
            [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
            public string? NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "The passwords do not match.")]
            public string? ConfirmNewPassword { get; set; }
        

    }
}
