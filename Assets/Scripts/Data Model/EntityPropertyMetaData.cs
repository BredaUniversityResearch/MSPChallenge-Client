
using UnityEngine.UI;

/// <summary>
/// Meta data for properties defined on the entities. These properties are stored in the metaData on the entities and the geometry_data field in the database.
/// </summary>
public class EntityPropertyMetaData
{
	public readonly string PropertyName;
	public readonly bool Enabled;   //Visible in game for non-dev users
	public readonly bool Editable;  //Is this property editable for non-dev users? 
	public readonly string DisplayName;
	public readonly string SpriteName;
	public readonly string DefaultValue;
	public readonly bool UpateVisuals;
	public readonly bool UpateText;
	public readonly bool UpateCalculation;
    public readonly InputField.ContentType ContentType;
    public readonly LayerInfoPropertiesObject.ContentValidation ContentValidation;
	public readonly string Unit;

	public EntityPropertyMetaData(string propertyName, bool enabled, bool editable, string displayName, string spriteName, string defaultValue, bool updateVisuals, bool updateText, bool updateCalculation, InputField.ContentType contentType, LayerInfoPropertiesObject.ContentValidation contentValidation, string unit)
	{
		PropertyName = propertyName;
		Enabled = enabled;
		Editable = editable;
		DisplayName = displayName;
		SpriteName = spriteName;
		DefaultValue = defaultValue;
		UpateVisuals = updateVisuals;
        UpateText = updateText;
        UpateCalculation = updateCalculation;
		ContentType = contentType;
        ContentValidation = contentValidation;
		Unit = unit;
	}

	public bool ShowInEditMode
	{
		get { return Enabled && Editable; }
	}
}
