using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Entities;
public class User : IdentityUser<Guid>
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; }

    //[MaxLength(100, ErrorMessage = "Last name must be max 100 characters")]

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; }

    [Range(0, 20000)]
    public int CaloriesPreference { get; set; }

    [MaxLength(50)]
    public string FileName { get; set; }

    [MaxLength(1000)]
    public string FileOriginalName { get; set; }

    [MaxLength(1000)]
    public string FileUrl { get; set; }

    [MaxLength(1000)]
    public string ThumbUrl { get; set; }

    [MaxLength(6)]
    public string EmailVerificationCode { get; set; }

    public DateTime? DateVerificationCodeSent { get; set; }
}
