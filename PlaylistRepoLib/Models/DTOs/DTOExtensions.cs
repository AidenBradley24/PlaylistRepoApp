namespace PlaylistRepoLib.Models.DTOs
{
	public static class DTOExtensions
	{
		public static TDTO GetDTO<TModel, TDTO>(this IHasDTO<TModel, TDTO> model) where TModel : class, new() where TDTO : DataTransferObject<TModel>, new()
		{
			var dto = new TDTO();
			dto.SyncDTO((TModel)model);
			return dto;
		}
	}
}
