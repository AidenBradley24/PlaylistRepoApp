using System.Reflection;
using System.Text.Json.Serialization;

namespace PlaylistRepoLib.Models.DTOs
{
	public abstract class DataTransferObject<TModel> where TModel : class, new()
	{
		/// <summary>
		/// Get all properties with the same name on both the DTO and the model.
		/// </summary>
		public IEnumerable<(PropertyInfo dtoProp, PropertyInfo modelProp)> SharedProperties()
		{
			var dtoProps = GetType().GetProperties();
			var modelProps = typeof(TModel).GetProperties();
			foreach (var dtoProp in dtoProps)
			{
				foreach (var modelProp in modelProps)
				{
					if (dtoProp.Name == modelProp.Name && modelProp.PropertyType == dtoProp.PropertyType)
						yield return (dtoProp, modelProp);
				}
			}
		}

		/// <summary>
		/// Update the model to match the contents of this DTO
		/// </summary>
		public void UpdateModel(TModel model)
		{
			OnUpdateModel(model);
			foreach (var (dtoProp, modelProp) in SharedProperties())
			{
				if (!modelProp.CanWrite) continue;
				modelProp.SetValue(model, dtoProp.GetValue(this));
			}
		}

		/// <summary>
		/// Update this DTO to match the contents of the model
		/// </summary>
		public void SyncDTO(TModel model)
		{
			OnSyncDTO(model);
			foreach (var (dtoProp, modelProp) in SharedProperties())
			{
				if (!dtoProp.CanWrite) continue;
				dtoProp.SetValue(this, modelProp.GetValue(model));
			}
		}

		public override string ToString()
		{
			TModel model = new();
			UpdateModel(model);
			return model.ToString() ?? base.ToString() ?? "";
		}

		/// <summary>
		/// Update the model to match the contents of this DTO.
		/// Used to update properties manually. Note that properties with the same name are updated automatically afterwards.
		/// </summary>
		/// <param name="model">The model to update</param>
		public virtual void OnUpdateModel(TModel model) { }

		/// <summary>
		/// Update this DTO to match the contents of the model.
		/// Used to update properties manually. Note that properties with the same name are updated automatically afterwards.
		/// </summary>
		public virtual void OnSyncDTO(TModel model) { }

		/// <summary>
		/// Used to patch a property in one or more dtos
		/// </summary>
		public class PatchElement
		{
			public string UserQuery { get; set; } = "";
			/// <summary>
			/// Case insensitive property name
			/// </summary>
			public string PropertyName { get; set; } = "";

			/// <summary>
			/// Property value automatically parsed
			/// </summary>
			public string PropertyValue { get; set; } = "";

			[JsonConverter(typeof(JsonStringEnumConverter))]
			public PatchType Type { get; set; } = PatchType.replace;

			public enum PatchType
			{
				replace, append, prepend
			}
		}

		/// <summary>
		/// Patch a property in a DTO
		/// </summary>
		/// <returns>True if successful</returns>
		public bool Patch(PatchElement element)
		{
			var prop = SharedProperties().FirstOrDefault(prop => prop.dtoProp.Name.Equals(element.PropertyName, StringComparison.OrdinalIgnoreCase)).dtoProp;
			if (prop == null) return false;
			object? value;
			try
			{
				value = Convert.ChangeType(element.PropertyValue, prop.PropertyType);
			}
			catch
			{
				return false;
			}

			switch (element.Type)
			{
				case PatchElement.PatchType.replace:
					prop.SetValue(this, value);
					break;
				case PatchElement.PatchType.append:
					if (prop.PropertyType == typeof(string))
					{
						prop.SetValue(this, (string?)prop.GetValue(this) + (string)value);
					}
					else
					{
						prop.SetValue(this, value);
					}
					break;
				case PatchElement.PatchType.prepend:
					if (prop.PropertyType == typeof(string))
					{
						prop.SetValue(this, (string)value + (string?)prop.GetValue(this));
					}
					else
					{
						prop.SetValue(this, value);
					}
					break;
			}

			return true;
		}
	}
}
