using System.Diagnostics.CodeAnalysis;
namespace MSP2050.Scripts
{
	/// <summary>
	/// Meta data for properties defined on the entities. These properties are stored in the metaData on the entities and the geometry_data field in the database.
	/// </summary>
	[SuppressMessage("ReSharper", "InconsistentNaming")] // needs to match database
	public class EntityPropertyMetaData
	{
		public readonly string PropertyName;
		public readonly bool Enabled;   //Visible in game for non-dev users
		private readonly bool Editable;  //Is this property editable for non-dev users? 
		public readonly string DisplayName;
		public readonly string SpriteName;
		public readonly string DefaultValue;
		public readonly bool UpateVisuals; // todo: fix typo's. Will break existing database content
		public readonly bool UpateText;
		public readonly bool UpateCalculation;
		public readonly TMPro.TMP_InputField.ContentType ContentType;
		public readonly LayerInfoPropertiesObject.ContentValidation ContentValidation;
		public readonly string Unit;

		public EntityPropertyMetaData(string a_propertyName, bool a_enabled, bool a_editable, string a_displayName, string a_spriteName, string a_defaultValue, bool a_updateVisuals, bool a_updateText, bool a_updateCalculation, TMPro.TMP_InputField.ContentType a_contentType, LayerInfoPropertiesObject.ContentValidation a_contentValidation, string a_unit)
		{
			PropertyName = a_propertyName;
			Enabled = a_enabled;
			Editable = a_editable;
			DisplayName = a_displayName;
			SpriteName = a_spriteName;
			DefaultValue = a_defaultValue;
			UpateVisuals = a_updateVisuals;
			UpateText = a_updateText;
			UpateCalculation = a_updateCalculation;
			ContentType = a_contentType;
			ContentValidation = a_contentValidation;
			Unit = a_unit;
		}

		public bool ShowInEditMode => Enabled && Editable;
	}
}
