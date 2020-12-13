namespace BarcodeGenerator.Tests
{
	using System;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class EAN13
	{
		[TestMethod]
		[DataRow(
			"012345633354",
			"0000000000 101 1110010 0011001 0010011 0111101 0100011 0110001 0101111 01010 1000010 1000010 1000010 1001110 1011100 1000010 101 0000000000")
		]
		public void Encode(string data, string expected)
		{
			Barcode barcode = new Barcode()
			{
				Data = data,
				Width = 300,
				Height = 150
			};

			var result = barcode.EncodeBarcode();

			Assert.AreEqual(result, expected);
		}

		[TestMethod]
		[DataRow(
			"0000000000 101 1110010 0011001 0010011 0111101 0100011 0110001 0101111 01010 1000010 1000010 1000010 1001110 1011100 1000010 101 0000000000",
			"0123456333543")
		]
		[DataRow(
			"0000000000 101 1101100 0011001 0010011 0111101 0100011 0110001 0101111 01010 1000010 1000010 1000010 1001110 1011100 1000010 101 0000000000",
			"2123456333543")
		]
		[DataRow(
			"0000000000 101 1000010 0011001 0010011 0111101 0100011 0110001 0101111 01010 1000010 1000010 1000010 1001110 1011100 1000010 101 0000000000",
			"3123456333543")
		]
		public void Decode(string data, string expected)
		{
			Barcode barcode = new Barcode()
			{
				Data = data,
				Width = 300,
				Height = 150
			};

			var result = barcode.DecodeBarcode();

			Assert.AreEqual(result, expected);
		}

		[TestMethod]
		[DataRow("0000000000 101 0001101 0011001 0010011 0111101 0100011 0110001 01010 1010000 1000010 1000010 1000010 1001110 1001110 101 0000000000 0000000000 0001101")]
		[DataRow("0000000000 101 0001101")]
		[DataRow("")]
		public void Decode_ThrowException(string data)
		{
			Barcode barcode = new Barcode()
			{
				Data = data
			};

			Assert.ThrowsException<Exception>(() => barcode.DecodeBarcode());
		}
	}
}
