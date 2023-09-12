using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Common.Models.Dress;
public class DressBasicInfoModel
{
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; }

    [MaxLength(50)]
    public string Type { get; set; }

    public DateTime Date { get; set; }

    [MaxLength(1000)]
    public string FileUrl { get; set; }
}
