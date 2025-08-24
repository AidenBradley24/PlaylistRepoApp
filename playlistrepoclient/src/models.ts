export interface Response<T> {
    data: T[]
    total: number
}

export interface Media {
    id: number,
    mimeType: string,
    title: string,
    primaryArtist: string,
    artists: string[] | undefined,
    album: string,
    description: string,
    rating: number,
    lengthMilliseconds: number,
    order: number
}

export interface Playlist {
    id: number,
    title: string,
    description: string;
    userQuery: string;
    bakedEntries: number[];
}
