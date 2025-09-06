using PlaylistRepoLib.Models.DTOs;

namespace PlaylistRepoAPI
{
	public interface IMetadataEnricher
	{
		/// <summary>
		/// Attempt to enrich the metadata of media
		/// </summary>
		/// <param name="media">Media (not modified)</param>
		/// <returns>A modifed DTO if successful</returns>
		public Task<MediaDTO?> TryEnrich(MediaDTO media); 
	}
}
