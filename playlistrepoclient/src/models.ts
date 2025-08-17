import type { TimeLike } from "node:fs";

export interface Response<T> {
    data: T[]
    total: number
}

export interface Media {
    id: number,
    title: string,
    primaryArtist: string | undefined,
    artists: string[] | undefined,
    album: string | undefined,
    description: string | undefined,
    rating: number,
    mediaLength: TimeLike,
    order: number
}
