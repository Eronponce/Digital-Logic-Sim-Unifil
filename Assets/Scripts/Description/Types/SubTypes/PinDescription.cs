using UnityEngine;

namespace DLS.Description
{
	public struct PinDescription
	{
		public string Name;
		public int ID;
		public Vector2 Position;
		public PinBitCount BitCount;
		public PinColour Colour;
		public PinValueDisplayMode ValueDisplayMode;

		public PinDescription(string name, int id, Vector2 position, PinBitCount bitCount, PinColour colour, PinValueDisplayMode valueDisplayMode)
		{
			Name = name;
			ID = id;
			Position = position;
			BitCount = bitCount;
			Colour = colour;
			ValueDisplayMode = valueDisplayMode;
		}
	}

	public enum PinBitCount
	{
		Bit1 = 1,
		Bit4 = 4,
		Bit8 = 8
	}

	public enum PinColour
	{
		Red,
		Orange,
		Yellow,
		Green,
		Blue,
		Violet,
		Pink,
		White,
		// Extended hue wheel (indices 8-15 — append-only for save compatibility)
		RedOrange,
		YellowOrange,
		Lime,
		Teal,
		Cyan,
		SkyBlue,
		Indigo,
		Magenta
	}

	public enum PinValueDisplayMode
	{
		Off,
		UnsignedDecimal,
		SignedDecimal,
		HEX
	}
}