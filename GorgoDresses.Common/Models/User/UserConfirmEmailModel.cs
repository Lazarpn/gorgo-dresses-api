using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaloriesTracking.Common.Models.User;
public class UserConfirmEmailModel
{
    [Required(ErrorMessage = "You must provide the email verification code.")]
    public string EmailVerificationCode { get; set; }
}
