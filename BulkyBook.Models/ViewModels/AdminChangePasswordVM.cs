using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BulkyBook.Models.ViewModels
{
    public class AdminChangePasswordVM
    {
        public string UserId { get; set; }

        [DisplayName("User Email")]
        public string UserEmail { get; set; }

        [DisplayName("New Password")]
        [DataType(DataType.Password)]
        [Required]
        public string NewPassword { get; set; }

        [DisplayName("Confirm New Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The Password and Confirm Password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
