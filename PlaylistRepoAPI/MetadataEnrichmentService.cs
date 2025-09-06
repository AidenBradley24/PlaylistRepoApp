using PlaylistRepoLib.Models.DTOs;

namespace PlaylistRepoAPI
{
	public class MetadataEnrichmentService(IMetadataEnricher/*[]*/ metadataEnrichers) // TODO if additional enrichers are added then change this file
	{
		public async Task<MediaDTO?> EnrichMedia(MediaDTO media)
		{
			MediaDTO? result = null;
			foreach (var enricher in new IMetadataEnricher[] { metadataEnrichers })
			{
				result = await enricher.TryEnrich(media);
				if (result != null) break;
			}
			return result;
		}
	}
}
