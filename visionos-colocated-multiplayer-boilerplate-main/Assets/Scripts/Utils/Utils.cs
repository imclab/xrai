using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Utils
{
    public static float CalculateStdDev(IEnumerable<float> values)
    {
        float ret = 0f;

        if (values.Count() > 0)
        {
            // Compute the Average
            float avg = values.Average();

            // Perform the Sum of (value-avg)^2
            float sum = values.Sum(d => Mathf.Pow(d - avg, 2f));

            // Put it all together
            ret = Mathf.Sqrt(sum / values.Count());
        }
        return ret;
    }
}
