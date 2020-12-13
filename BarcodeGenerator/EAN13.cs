using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BarcodeGenerator
{
    class EAN13
    {
        public string Data { get; set; }
        public float Height { get; set; }
        public float Width { get; set; }
        public float FontSize { get; set; } = 17.0f;
        public float Scale { get; set; }

        private readonly string[] _lCode = 
        { 
            "0001101", 
            "0011001", 
            "0010011", 
            "0111101",
            "0100011", 
            "0110001", 
            "0101111", 
            "0111011",
            "0110111", 
            "0001011" 
        };

        private readonly string[] _gCode = 
        { 
            "0100111", 
            "0110011", 
            "0011011", 
            "0100001",
            "0011101", 
            "0111001", 
            "0000101", 
            "0010001",
            "0001001", 
            "0010111" 
        };

        private readonly string[] _rCode = 
        { 
            "1110010", 
            "1100110", 
            "1101100", 
            "1000010",
            "1011100", 
            "1001110", 
            "1010000", 
            "1000100",
            "1001000", 
            "1110100" 
        };

        private readonly string[] _countryCodePattern = 
        { 
            "LLLLLL",
            "LLGLGG",
            "LLGGLG",
            "LLGGGL",
            "LGLLGG",
            "LGGLLG",
            "LGGGLL",
            "LGLGLG",
            "LGLGGL",
            "LGGLGL"
        };

        private readonly string _quiteZone = "0000000000";
        private readonly string _guardPatterns = "101";
        private readonly string _middleGuardPatterns = "01010";

        private string _countryCode = "00";
        private string _manufacturerCode = "";
        private string _productCode = "";
        private string _checkDigit = "";

        public EAN13(string data, float width, float height, float scale)
        {
            Data = data;
            Width = width;
            Height = height;
            Scale = scale;
        }

        #region Encode
        public string Encode(string digits)
        {
            if (!new Regex(@"^\d+$").IsMatch(digits))
            {
                throw new Exception("EAN-13 allowed numeric values only.");
            }
            if (digits.Length < 12)
            {
                throw new Exception("EAN-13 format required min 12 char.");
            }
            if (digits.Length > 13)
            {
                throw new Exception("EAN-13 format required max 13 char.");
            }

            var checkDegit = CalculateChecksumDigit();
            if (digits.Length == 13)
            {
                if ((digits[12] - '0') != checkDegit)
                    throw new Exception("Invalid check degit");
            }

            var countryCode = digits.Substring(0, 2);
            var encodedData = GetEncodedData(countryCode, digits, checkDegit.ToString());

            return encodedData;
        }

        private string GetEncodedData(string countryCode, string digits, string checkDigitPart)
        {
            var numberSystem = EncodedDigitToPattern(countryCode.Substring(0, 1), _rCode);
            var left = Regex.Replace(EncodedDigitToPatternsLeft(digits.Substring(0, 7), countryCode), ".{7}", "$0 ");
            var right = Regex.Replace(EncodedDigitToPattern(digits.Substring(7, 5), _rCode), ".{7}", "$0 ");
            var checkDigit = EncodedDigitToPattern(checkDigitPart, _rCode);

            var encodedData =
                $"{_quiteZone} {_guardPatterns} {numberSystem} {left}{_middleGuardPatterns} {right}{checkDigit} {_guardPatterns} {_quiteZone}";

            return encodedData;
        }

        private string EncodedDigitToPatternsLeft(string digits, string countryCode)
        {
            var pattern = _countryCodePattern[(countryCode[0] - '0')];
            digits = digits.Substring(1);

            var sbTemp = new StringBuilder();
            for (int i = 0; i < digits.Length; i++)
            {
                if (pattern[i] == 'G')
                {
                    sbTemp.Append(_gCode[(digits[i] - '0')]);
                }
                else if (pattern[i] == 'L')
                {
                    sbTemp.Append(_lCode[(digits[i] - '0')]);
                }
            }

            return Convert.ToString(sbTemp);
        }

        private string EncodedDigitToPatternsLeft(string digits)
        {
            var pattern = _countryCodePattern[(_countryCode[0] - '0')];
            digits = digits.Substring(1);

            var sbTemp = new StringBuilder();
            for (int i = 0; i < digits.Length; i++)
            {
                if (pattern[i] == 'G')
                {
                    sbTemp.Append(_gCode[(digits[i] - '0')]);
                }
                else if (pattern[i] == 'L')
                {
                    sbTemp.Append(_lCode[(digits[i] - '0')]);
                }
            }

            return Convert.ToString(sbTemp);
        }
        public string EncodedDigitToPattern(string digits, string[] patterns)
        {
            return string.Join("", digits.Select(x => patterns[(x - '0')]));
        }

        #endregion
        public Bitmap GenerateImage()
        {
            if (!new Regex(@"^\d+$").IsMatch(Data))
            {
                throw new Exception("EAN-13 allowed numeric values only.");
            }
            if (Data.Length < 12)
            {
                throw new Exception("EAN-13 format required min 12 char.");
            }
            if (Data.Length > 13)
            {
                throw new Exception("EAN-13 format required max 13 char.");
            }

            _countryCode = Data.Substring(0, 2);
            _manufacturerCode = Data.Substring(2, 5);
            _productCode = Data.Substring(7, 5);

            var checkDegit = CalculateChecksumDigit();
            if (Data.Length == 13)
            {
                if ((Data[12] - '0') != checkDegit)
                    throw new Exception("Invalid check degit");
            }
            else
            {
                Data = string.Concat(Data, checkDegit);
            }
            _checkDigit = Data.Substring(12);
            
            var tempWidth = (Width * Scale) * 100;
            var tempHeight = (Height * Scale) * 100;

            Bitmap bmp = new Bitmap((int)tempWidth, (int)tempHeight);

            Graphics g = Graphics.FromImage(bmp);
            DrawUpcaBarcode(g, new Point(0, 0));
            g.Dispose();
            return bmp;
        }

        public void DrawUpcaBarcode(Graphics g, Point pt)
        {
            var left = EncodedDigitToPatternsLeft(Data.Substring(0, 7));
            var right = EncodedDigitToPattern(Data.Substring(7, 5), _rCode);
            var checkDigit = EncodedDigitToPattern(_checkDigit, _rCode);

            var ean13 =
                $"{_quiteZone}{_guardPatterns}{left}{_middleGuardPatterns}{right}{checkDigit}{_guardPatterns}{_quiteZone}";

            var width = Width * Scale;
            var height = Height * Scale;

            float lineWidth = width / 113f;

            GraphicsState gs = g.Save();
            g.PageUnit = GraphicsUnit.Inch;
            g.PageScale = 1;

            float xPosition = 0;
            float xStart = pt.X;
            float yStart = pt.Y;

            SolidBrush brush = new SolidBrush(Color.Black);
            Font font = new Font("Arial", FontSize * Scale);

            float fTextHeight = g.MeasureString(ean13, font).Height;

            // Draw the barcode lines.
            for (int i = 0; i < ean13.Length; i++)
            {
                if (ean13.Substring(i, 1) == "1")
                {
                    if (xStart == pt.X)
                        xStart = xPosition;

                    if ((i > 12 && i < 55) || (i > 59 && i < 101))
                        g.FillRectangle(brush, xPosition, yStart, lineWidth, height - fTextHeight);
                    else
                        g.FillRectangle(brush, xPosition, yStart, lineWidth, height);
                }

                xPosition += lineWidth;
            }

            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            xPosition = xStart - g.MeasureString(  _countryCode.Substring(0, 1), font).Width;
            float yPosition = yStart + (height - fTextHeight);
            g.DrawString(_countryCode.Substring(0, 1), font, brush, new PointF(xPosition, yPosition));

            xPosition += g.MeasureString(_countryCode.Substring(0, 1), font).Width + 45 * lineWidth -
                            g.MeasureString(string.Concat(_countryCode[1], _manufacturerCode), font).Width;

            g.DrawString(string.Concat(_countryCode[1], _manufacturerCode), font, brush, new PointF(xPosition, yPosition));

            xPosition += g.MeasureString(string.Concat(_countryCode[1], _manufacturerCode), font).Width +
                         5 * lineWidth;

            g.DrawString(string.Concat(_productCode, _checkDigit), font, brush, new PointF(xPosition, yPosition));

            g.Restore(gs);
        }

        private int CalculateChecksumDigit()
        {
            var degits = Data.Take(12).Select(x => x - '0').Reverse();
            var even = degits.Where((x, i) => i % 2 == 0).Sum() * 3;
            var odd = degits.Where((x, i) => i % 2 != 0).Sum();

            int checkDegit = (10 - ((even + odd) % 10)) % 10;

            return checkDegit;
        }

        #region Decode
        public string Decode(string digits)
        {
            var digitsParts = digits.Trim().Split(' ');

            if (digitsParts.Count() != 13 + 5)
            {
                throw new Exception("Incorrect EAN-13 format.");
            }

            var numberSystem = digitsParts[2];
            var left = digitsParts.SubArray(3, 6);
            var right = digitsParts.SubArray(10, 6);
            
            var ean13_decoded = GetDecodedData(left, right, numberSystem);

            return ean13_decoded;
        }

        private string GetDecodedData(
            string[] leftPart,
            string[] rightPart,
            string numberSystem)
        {
            var numberSystemPart = DecodeDigitToPattern(numberSystem, _rCode);
            string left = string.Concat(leftPart.Select(x => DecodeDigitToPatternsLeft(x, numberSystem)));
            var right = string.Concat(rightPart.Select(x => DecodeDigitToPattern(x, _rCode)));

            var decodedData = $"{numberSystemPart}{left}{right}";

            return decodedData;
        }

        private string DecodeDigitToPattern(string code, string[] pattern)
        {
            return string.Join(
                "",
                pattern
                    .Select((x, i) => new { i, x })
                    .Where(t => t.x == code)
                    .Select(t => t.i)
                );
        }

        private string DecodeDigitToPatternsLeft(string digits, string countryCode)
        {
            var pattern = _countryCodePattern[(countryCode[0] - '0')];

            var sbTemp = new StringBuilder();
            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] == 'G')
                {
                    sbTemp.Append(
                        string.Join(
                            "",
                            _gCode
                                .Select((x, y) => new { y, x })
                                .Where(t => t.x == digits)
                                .Select(t => t.y)
                            )
                    );
                }
                else if (pattern[i] == 'L')
                {
                    sbTemp.Append(
                        string.Join(
                            "",
                            _lCode
                                .Select((x, y) => new { y, x })
                                .Where(t => t.x == digits)
                                .Select(t => t.y)
                            )
                    );
                }
            }
            var a = Convert.ToString(sbTemp);
            var result = a[0];

            return result.ToString();
        }

        #endregion
    }
}
