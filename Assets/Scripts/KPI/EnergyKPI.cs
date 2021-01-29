public class EnergyKPI
{
	public enum EnergyType
	{
		Pipelines,
		GreyProductionZones,
		GreenProductionZones,
		TransformerStations,
		LandSockets,
		Cables,
		NoEnergy
	};

	public static readonly EnergyType[] allEnergyTypes = { EnergyType.Pipelines, EnergyType.GreyProductionZones, EnergyType.GreenProductionZones, EnergyType.TransformerStations, EnergyType.LandSockets, EnergyType.Cables };
}