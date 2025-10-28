using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class RomanNumerals
{
    /// <summary>
    /// Converts 1..3999 to Roman numerals (classical form).
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string ToRoman(int number)
    {
        if (number <= 0 || number > 3999)
            throw new ArgumentOutOfRangeException(nameof(number), "Value must be in the range 1..3999.");

        // Greedy mapping of values to symbols
        int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        string[] sym = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

        var sb = new StringBuilder();

        for (int i = 0; i < values.Length; i++)
        {
            while (number >= values[i])
            {
                sb.Append(sym[i]);
                number -= values[i];
            }
        }

        return sb.ToString();
    }
}
