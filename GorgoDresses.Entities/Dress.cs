using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Entities;
public class Dress
{
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; }

    [MaxLength(50)]
    public string Type { get; set; }

    [Range(0, 100000)]
    public string RentingPrice { get; set; }

    [Range(0, 100000)]
    public string SellingPrice { get; set; }

    public DateTime Date { get; set; }

    [MaxLength(50)]
    public string FileName { get; set; }

    [MaxLength(1000)]
    public string FileOriginalName { get; set; }

    [MaxLength(1000)]
    public string FileUrl { get; set; }

}
