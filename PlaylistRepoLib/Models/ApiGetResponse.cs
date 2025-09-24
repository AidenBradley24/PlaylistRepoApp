using PlaylistRepoLib.Models.DTOs;
using PlaylistRepoLib.UserQueries;
using System.Text.Json.Serialization;

namespace PlaylistRepoLib.Models;

public class ApiGetResponse<TModel, TDTO>
	where TModel : class, new()
	where TDTO : DataTransferObject<TModel>, new()
{
	[JsonPropertyName("total")]
	public int Total { get; set; }

	[JsonPropertyName("data")]
	public TDTO[]? Data { get; set; }

	public ApiGetResponse() { }

	public ApiGetResponse(IQueryable<TModel> dataset, string userQuery, int pageSize, int currentPage)
	{
		var result = dataset.EvaluateUserQuery(userQuery);
		Total = result.Count();
		currentPage -= 1;
		if (currentPage < 0) currentPage = 0;
		Data = [.. result.Skip(pageSize * currentPage).Take(pageSize).AsEnumerable().Select((model) =>
		{
			var dto = new TDTO();
			dto.SyncDTO(model);
			return dto;
		})];
	}
}
