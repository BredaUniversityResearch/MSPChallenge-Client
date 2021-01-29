using UnityEngine;

namespace KPI
{
	[CreateAssetMenu(menuName = "MSP2050/KPI Value Procedural Color Scheme")]
	class KPIValueProceduralColorScheme: ScriptableObject
	{
		public class Context
		{
			internal int usedColors;
		};

		[SerializeField]
		private Color[] colors = { Color.white };

		public Color GetColor(Context context)
		{
			Color result = colors[context.usedColors % colors.Length];
			++context.usedColors;
			return result;
		}
	}
}
