using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorgoDresses.Common.Helpers;
public static class Utilities
{
    public static Random random = new Random();

    public static string GenerateNumericCode(int length)
    {
        var code = new StringBuilder(length);
        for(int i = 0; i < length; i++)
        {
            code.Append(Convert.ToString(random.Next(0, 9)));
        }
        return code.ToString();
    }
}
