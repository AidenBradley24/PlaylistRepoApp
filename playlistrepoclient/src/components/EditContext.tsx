import React, { createContext, useContext, useState, useEffect } from "react";
import type { Media, Playlist, RemotePlaylist } from "../models";
import EditPlaylistModal from '../pages/EditPlaylistModal';
import MediaModal from '../pages/MediaModal';
import EditRemoteModal from '../pages/EditRemoteModal';
import { useSearchParams, useLocation } from "react-router-dom";

const EditContext = createContext<EditContextType | undefined>(undefined);

interface EditContextType {
    query: string;
    setQuery: (value: string) => void;

    showMediaModal: boolean;
    setShowMediaModal: (value: boolean) => void;
    viewingMedia: Media | null;
    setViewingMediaId: (id: number) => void;
    editingMedia: Media | null;
    setEditingMedia: (value: Media | null) => void;

    viewingPlaylistId: number;
    setViewingPlaylistId: (id: number) => void;

    showPlaylistModal: boolean;
    setShowPlaylistModal: (value: boolean) => void;
    viewingPlaylist: Playlist | null;
    editingPlaylist: Playlist | null;
    setEditingPlaylist: (value: Playlist | null) => void;

    showRemoteModal: boolean;
    setShowRemoteModal: (value: boolean) => void;
    viewingRemote: RemotePlaylist | null;
    editingRemote: RemotePlaylist | null;
    setEditingRemote: (value: RemotePlaylist | null) => void;
}

export const EditProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {

    const [searchParams, setSearchParams] = useSearchParams();
    const location = useLocation();

    const queryFromUrl = searchParams.get("q") ?? "";
    const [query, setQuery] = useState<string>(queryFromUrl);

    const [showMediaModal, setShowMediaModal] = useState<boolean>(false);
    const mediaFromUrl = Number(searchParams.get("m")) ?? 0;
    const [viewingMediaId, setViewingMediaId] = useState<number>(mediaFromUrl);
    const [viewingMedia, setViewingMedia] = useState<Media | null>(null);
    const [editingMedia, setEditingMedia] = useState<Media | null>(null);

    const playlistFromUrl = Number(searchParams.get("p")) ?? 0;
    const [viewingPlaylistId, setViewingPlaylistId] = useState<number>(playlistFromUrl);

    const [showPlaylistModal, setShowPlaylistModal] = useState<boolean>(false);
    const [viewingPlaylist, setViewingPlaylist] = useState<Playlist | null>(null);
    const [editingPlaylist, setEditingPlaylist] = useState<Playlist | null>(null);

    const [showRemoteModal, setShowRemoteModal] = useState<boolean>(false);
    const [viewingRemote, setViewingRemote] = useState<RemotePlaylist | null>(null);
    const [editingRemote, setEditingRemote] = useState<RemotePlaylist | null>(null);

    useEffect(() => {
        const params = {} as any;
        if (viewingPlaylistId !== 0) params["p"] = viewingPlaylistId;
        if (query) params['q'] = query;
        if (viewingMediaId !== 0) params["m"] = viewingMediaId;
        setSearchParams(params);
    }, [query, viewingMediaId, viewingPlaylistId, setSearchParams]);

    useEffect(() => {
        if (viewingMediaId > 0) {
            fetch(`api/data/media/${viewingMediaId}`).then(async (response) => {
                if (!response.ok) {
                    setViewingMedia(null);
                    throw new Error("NOT OKAY");
                }
                setViewingMedia(await response.json() as Media);
            });
            setShowMediaModal(true);
        }
        setViewingMedia(null);
    }, [viewingMediaId]);

    useEffect(() => {
        if (viewingPlaylistId > 0) {
            if (location.pathname === "/playlists") {
                fetch(`api/data/playlists/${viewingPlaylistId}`).then(async (response) => {
                    if (!response.ok) {
                        setViewingPlaylist(null);
                        throw new Error("NOT OKAY");
                    }
                    setViewingPlaylist(await response.json() as Playlist);
                });
            }
            else if (location.pathname === "/remotes") {
                fetch(`api/data/remotes/${viewingPlaylistId}`).then(async (response) => {
                    if (!response.ok) {
                        setViewingRemote(null);
                        throw new Error("NOT OKAY");
                    }
                    setViewingRemote(await response.json() as RemotePlaylist);
                });
            }
            else {
                throw new Error("Incorrect pathname " + location.pathname);
            }
        }
        setViewingPlaylist(null);
    }, [viewingPlaylistId]);

    return (
        <EditContext.Provider value={{
            query,
            setQuery,
            showMediaModal,
            setShowMediaModal,
            showPlaylistModal,
            setShowPlaylistModal,
            viewingMedia,
            editingMedia,
            setEditingMedia,
            viewingPlaylist,
            editingPlaylist,
            setEditingPlaylist,
            showRemoteModal,
            setShowRemoteModal,
            viewingRemote,
            editingRemote,
            setEditingRemote,
            setViewingMediaId,
            setViewingPlaylistId,
            viewingPlaylistId
        }}>
            {children}
            <MediaModal
                show={showMediaModal}
                viewingMedia={viewingMedia}
                onHide={() => {
                    setShowMediaModal(false)
                    setViewingMediaId(0);
                }}
                onSaved={() => { }}
                editingMedia={editingMedia}
                setEditingMedia={setEditingMedia}
            />
            <EditPlaylistModal
                title={!editingPlaylist || editingPlaylist.id === 0 ? "Create Playlist" : "Edit Playlist"}
                show={showPlaylistModal}
                onHide={() => setShowPlaylistModal(false)}
                onCreated={(playlist) => {
                    setShowPlaylistModal(false);
                    setViewingPlaylist(playlist);
                }}
                editingPlaylist={editingPlaylist}
                setEditingPlaylist={setEditingPlaylist}
            />
            <EditRemoteModal
                title={!editingRemote || editingRemote.id === 0 ? "Add Remote Reference" : "Edit Remote Reference"}
                show={showRemoteModal}
                onHide={() => setShowRemoteModal(false)}
                onCreated={(playlist) => {
                    setShowRemoteModal(false);
                    setViewingRemote(playlist);
                }}
                editingPlaylist={editingRemote}
                setEditingPlaylist={setEditingRemote}
            />
        </EditContext.Provider>
    );
};

export const useEdits = () => useContext(EditContext)!;
