using System.Globalization;

public static class Localisation
{
	static Localisation()
	{
		NumberFormatting = new CultureInfo("en-US", false);
		NumberFormatting.NumberFormat.NumberDecimalDigits = 2;
		NumberFormatting.NumberFormat.NumberDecimalSeparator = ".";
		NumberFormatting.NumberFormat.NumberGroupSeparator = "";
		NumberFormatting.NumberFormat.NumberNegativePattern = 1;

		DateFormatting = DateTimeFormatInfo.GetInstance(NumberFormatting);
	}

	public static readonly CultureInfo NumberFormatting;
	public static NumberStyles FloatNumberStyle = NumberStyles.Float | NumberStyles.AllowThousands;
	public static readonly DateTimeFormatInfo DateFormatting;
}

