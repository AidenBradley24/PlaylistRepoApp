import type { TimeLike } from "node:fs";
import type { MIMEType } from "node:util";

export interface Response<T> {
    data: T[]
    total: number
}

export interface Media {
    id: number,
    mimeType: string,
    title: string,
    primaryArtist: string | undefined,
    artists: string[] | undefined,
    album: string | undefined,
    description: string | undefined,
    rating: number,
    mediaLength: TimeLike | undefined,
    order: number
}
