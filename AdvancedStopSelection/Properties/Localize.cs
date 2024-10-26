namespace AdvancedStopSelection
{
	public class Localize
	{
		public static System.Globalization.CultureInfo Culture {get; set;}
		public static ModsCommon.LocalizeManager LocaleManager {get;} = new ModsCommon.LocalizeManager("Localize", typeof(Localize).Assembly);

		/// <summary>
		/// Allows to explicitly specify platform when pressing Shift
		/// </summary>
		public static string Mod_Description => LocaleManager.GetString("Mod_Description", Culture);

		/// <summary>
		/// [NEW] Added Plazas & Promenades DLC support.
		/// </summary>
		public static string Mod_WhatsNewMessage2_0 => LocaleManager.GetString("Mod_WhatsNewMessage2_0", Culture);

		/// <summary>
		/// [NEW] Added Hubs&Transport support.
		/// </summary>
		public static string Mod_WhatsNewMessage2_1 => LocaleManager.GetString("Mod_WhatsNewMessage2_1", Culture);

		/// <summary>
		/// [UPDATED] Updated required game version to 1.18.1-f3
		/// </summary>
		public static string Mod_WhatsNewMessage2_2 => LocaleManager.GetString("Mod_WhatsNewMessage2_2", Culture);
	}
}