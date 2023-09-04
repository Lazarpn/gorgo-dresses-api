using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Common.Models.Dress;
public class DressCreateModel
{
    [MaxLength(50)]
    public string Name { get; set; }

    [MaxLength(50)]
    public string Type { get; set; }

    [Range(0, 100000)]
    public string RentingPrice { get; set; }

    [Range(0, 100000)]
    public string SellingPrice { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;

    public IFormFile File { get; set; }
}
