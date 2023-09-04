using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Common.Validation;
public class ValidationResult
{
    public string Property { get; set; }
    public List<string> Errors { get; set; } = new();
}
