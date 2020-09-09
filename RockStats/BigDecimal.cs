using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Nethereum.Util
{
    /// BigNumber based on the original http://uberscraper.blogspot.co.uk/2013/09/c-bigdecimal-class-from-stackoverflow.html
    /// which was inspired by http://stackoverflow.com/a/4524254
    /// Original Author: Jan Christoph Bernack (contact: jc.bernack at googlemail.com)
    /// Changes JB: Added parse, Fix Normalise, Added Floor, New ToString, Change Equals (normalise to validate first), Change Casting to avoid overflows (even if might be slower), Added Normalise Bigger than zero, test on operations, parsing, casting, and other test coverage for ethereum unit conversions
    /// Changes KJ: Added Culture formatting
    /// http://stackoverflow.com/a/13813535/956364" />
    /// <summary>
    /// Arbitrary precision Decimal.
    /// All operations are exact, except for division.
    /// Division never determines more digits than the given precision of 50.
    /// </summary>
    public class BigDecimal
    {
        public const int Precision = 50;

        /// <summary>
        /// </summary>
        /// <param name="mantissa"></param>
        /// <param name="exponent">
        /// The number of decimal units for example (-18). A positive value will be normalised as 10 ^exponent
        /// </param>
        /// <param name="alwaysTruncate">
        /// Specifies whether the significant digits should be truncated to the given precision after each operation.
        /// </param>
        public BigDecimal(BigInteger mantissa, int exponent, bool alwaysTruncate = false)
        {
            Mantissa = mantissa;
            Exponent = exponent;
            NormaliseExponentBiggerThanZero();
            Normalize();
            if (alwaysTruncate)
                Truncate();
        }

        public BigInteger Mantissa { get; internal set; }
        
        public int Exponent { get; internal set; }

        public void NormaliseExponentBiggerThanZero()
        {
            if (Exponent > 0)
            {
                Mantissa = Mantissa * BigInteger.Pow(10, Exponent);
                Exponent = 0;
            }
        }

        public void Normalize()
        {
            if (Exponent == 0) return;

            if (Mantissa.IsZero)
            {
                Exponent = 0;
            }
            else
            {
                BigInteger remainder = 0;
                while (remainder == 0)
                {
                    var shortened = BigInteger.DivRem(Mantissa, 10, out remainder);
                    if (remainder != 0)
                        continue;
                    Mantissa = shortened;
                    Exponent++;
                }
                NormaliseExponentBiggerThanZero();
            }
        }

        /// <summary>
        /// Truncate the number to the given precision by removing the least significant digits.
        /// </summary>
        /// <returns>The truncated number</returns>
        internal BigDecimal Truncate()
        {
            // copy this instance (remember its a struct)
            var shortened = this;
            // save some time because the number of digits is not needed to remove trailing zeros
            shortened.Normalize();
            // remove the least significant digits, as long as the number of digits is higher than the given Precision
            while (BigNumberNumberOfDigits(shortened.Mantissa) > Precision)
            {
                shortened.Mantissa /= 10;
                shortened.Exponent++;
            }
            return shortened;
        }

        public override string ToString()
        {
            Normalize();
            var s = Mantissa.ToString();
            if (Exponent != 0)
            {
                var decimalPos = s.Length + Exponent;
                if (decimalPos < s.Length)
                    if (decimalPos >= 0)
                        s = s.Insert(decimalPos, decimalPos == 0 ? "0." : ".");
                    else
                        s = "0." + s.PadLeft(decimalPos * -1 + s.Length, '0');
                else
                    s = s.PadRight(decimalPos, '0');
            }
            return s;
        }

        public static BigDecimal Add(BigDecimal left, BigDecimal right)
        {
            return left.Exponent > right.Exponent
                ? new BigDecimal(AlignExponent(left, right) + right.Mantissa, right.Exponent)
                : new BigDecimal(AlignExponent(right, left) + left.Mantissa, left.Exponent);
        }

        public static BigDecimal Parse(string value)
        {
            //todo culture format
            var decimalCharacter = ".";
            var indexOfDecimal = value.IndexOf(".");
            var exponent = 0;
            if (indexOfDecimal != -1)
                exponent = (value.Length - (indexOfDecimal + 1)) * -1;
            var mantissa = BigInteger.Parse(value.Replace(decimalCharacter, ""));
            return new BigDecimal(mantissa, exponent);
        }

        /// <summary>
        /// Returns the mantissa of value, aligned to the exponent of reference.
        /// Assumes the exponent of value is larger than of value.
        /// </summary>
        private static BigInteger AlignExponent(BigDecimal value, BigDecimal reference)
        {
            return value.Mantissa * BigInteger.Pow(10, value.Exponent - reference.Exponent);
        }

        public static int BigNumberNumberOfDigits(BigInteger value)
        {
            return (value * value.Sign).ToString().Length;
        }
    }
}