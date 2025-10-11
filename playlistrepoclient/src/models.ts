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
    order: number,
    isOnFile: boolean,
    locked: boolean,
    genre: string
}

export interface Playlist {
    id: number,
    title: string,
    description: string;
    userQuery: string;
}

export interface RemotePlaylist {
    id: number,
    name: string,
    description: string;
    link: string;
    type: string;
    mediaMime: string;
}

export interface Patch<T> {
    userQuery: string;
    propertyName: keyof T;
    propertyValue: T[keyof T];
    type: "replace" | "append" | "prepend";
}
