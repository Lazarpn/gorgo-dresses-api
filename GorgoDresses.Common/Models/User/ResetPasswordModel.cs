using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaloriesTracking.Common.Models.User;
public class ResetPasswordModel
{
    public Guid UserId { get; set; }
    public string Token { get; set; }
    public string Password { get; set; }
}
