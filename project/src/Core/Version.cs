namespace Synergy
{
	class Version
	{
		public const int Major = 1;
		public const int Minor = 2;

		public static string String
		{
			get
			{
				return Major.ToString() + "." + Minor.ToString();
			}
		}

		public static string DisplayString
		{
			get
			{
				return "Synergy " + String;
			}
		}
	}
}
