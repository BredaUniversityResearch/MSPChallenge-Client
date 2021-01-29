using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface IEnergyDataHolder
{
    long Capacity { get; set; }
    long UsedCapacity { get; set; }
	EnergyGrid LastRunGrid { get; set; }
	EnergyGrid CurrentGrid { get; set; }
}

