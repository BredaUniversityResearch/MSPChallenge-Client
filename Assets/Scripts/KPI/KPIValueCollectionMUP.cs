using System.Collections.Generic;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class KPIValueCollectionMUP: KPIValueCollection
	{
		const float MULTI_USE_AREA_RATIO = 0.2f;
		public const string MUP_SAVED_KPI_NAME = "MUP Saved";

		public void KPIUpdateComplete(int newMonth)
		{
			OnNewKpiDataReceived(newMonth);
		}

		public void UpdateLayerValues(PolygonLayer layer, LayerState state, int month)
		{
			float total = 0;

			if (layer != null)
			{
				foreach (Entity entity in state.baseGeometry)
				{
					if (countryId != 0 && entity.Country != countryId)
						continue;

					PolicyGeometryDataMUPlatform policyData = null;
					if (entity.TryGetMetaData(PolicyManager.MU_PLATFORM_POLICY_NAME, out string policyJSON))
					{
						policyData = JsonConvert.DeserializeObject<PolicyGeometryDataMUPlatform>(policyJSON);
					}
					if (policyData == null || policyData.options == null)
						continue;
					bool hasMultiUse = false;
					for (int i = 0; i < policyData.options.Length; i++)
					{
						if (policyData.options[i])
						{
							hasMultiUse = true;
							break;
						}
					}
					if (!hasMultiUse)
						continue;
					total += ((PolygonSubEntity)entity.GetSubEntity(0)).SurfaceAreaSqrKm * MULTI_USE_AREA_RATIO;
				}
			}
			TryUpdateKPIValue(MUP_SAVED_KPI_NAME, month, total);
		}
	}
}
