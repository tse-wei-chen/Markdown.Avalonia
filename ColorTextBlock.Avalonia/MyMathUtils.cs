using System;

namespace ColorTextBlock.Avalonia
{
    internal class MyMathUtils
    {
        public static bool AreClose(double value1, double value2)
        {
            if (value1 == value2)
                return true;
            double num1 = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * 2.220446049250313E-16;
            double num2 = value1 - value2;
            return -num1 < num2 && num1 > num2;
        }
    }
}