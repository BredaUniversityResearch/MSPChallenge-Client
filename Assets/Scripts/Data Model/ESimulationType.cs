using System;

/// <summary>
/// Enum specifying the simulations that we have.
/// </summary>
[Flags]
public enum ESimulationType
{
	None = 0,
	MEL = (1 << 0),
	SEL = (1 << 1),
	CEL = (1 << 2)
}
