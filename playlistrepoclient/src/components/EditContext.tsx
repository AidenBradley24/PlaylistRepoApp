import React, { createContext, useContext, useState, useEffect } from "react";
import type { Media, Playlist, RemotePlaylist } from "../models";
import EditPlaylistModal from '../pages/EditPlaylistModal';
import MediaModal from '../pages/MediaModal';
import EditRemoteModal from '../pages/EditRemoteModal';
import { useSearchParams } from "react-router-dom";

const EditContext = createContext<EditContextType | undefined>(undefined);

interface EditContextType {
    query: string;
    setQuery: (value: string) => void;

    showMediaModal: boolean;
    setShowMediaModal: (value: boolean) => void;
    viewingMedia: Media | null;
    setViewingMedia: (value: Media | null) => void;
    editingMedia: Media | null;
    setEditingMedia: (value: Media | null) => void;

    showPlaylistModal: boolean;
    setShowPlaylistModal: (value: boolean) => void;
    viewingPlaylist: Playlist | null;
    setViewingPlaylist: (value: Playlist | null) => void;
    editingPlaylist: Playlist | null;
    setEditingPlaylist: (value: Playlist | null) => void;

    showRemoteModal: boolean;
    setShowRemoteModal: (value: boolean) => void;
    viewingRemote: RemotePlaylist | null;
    setViewingRemote: (value: RemotePlaylist | null) => void;
    editingRemote: RemotePlaylist | null;
    setEditingRemote: (value: RemotePlaylist | null) => void;
}

export const EditProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {

    const [searchParams, setSearchParams] = useSearchParams();
    const queryFromUrl = searchParams.get("q") ?? "";
    const [query, setQuery] = useState<string>(queryFromUrl);

    const [showMediaModal, setShowMediaModal] = useState<boolean>(false);
    const [viewingMedia, setViewingMedia] = useState<Media | null>(null);
    const [editingMedia, setEditingMedia] = useState<Media | null>(null);

    const [showPlaylistModal, setShowPlaylistModal] = useState<boolean>(false);
    const [viewingPlaylist, setViewingPlaylist] = useState<Playlist | null>(null);
    const [editingPlaylist, setEditingPlaylist] = useState<Playlist | null>(null);

    const [showRemoteModal, setShowRemoteModal] = useState<boolean>(false);
    const [viewingRemote, setViewingRemote] = useState<RemotePlaylist | null>(null);
    const [editingRemote, setEditingRemote] = useState<RemotePlaylist | null>(null);

    useEffect(() => {
        if (query) {
            setSearchParams({ q: query });
        } else {
            setSearchParams({});
        }
    }, [query, setSearchParams]);

    return (
        <EditContext.Provider value={{
            query,
            setQuery,
            showMediaModal,
            setShowMediaModal,
            showPlaylistModal,
            setShowPlaylistModal,
            viewingMedia,
            setViewingMedia,
            editingMedia,
            setEditingMedia,
            viewingPlaylist,
            setViewingPlaylist,
            editingPlaylist,
            setEditingPlaylist,
            showRemoteModal,
            setShowRemoteModal,
            viewingRemote,
            setViewingRemote,
            editingRemote,
            setEditingRemote
        }}>
            {children}
            <MediaModal
                show={showMediaModal}
                viewingMedia={viewingMedia}
                onHide={() => setShowMediaModal(false)}
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
