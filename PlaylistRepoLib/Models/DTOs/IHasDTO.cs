namespace PlaylistRepoLib.Models.DTOs
{
	public interface IHasDTO<TModel, TDTO> where TModel : class, new() where TDTO : DataTransferObject<TModel>, new()
	{

	}
}
