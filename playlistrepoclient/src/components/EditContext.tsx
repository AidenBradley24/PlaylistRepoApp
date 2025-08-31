import React, { createContext, useContext, useState } from "react";
import type { Media, Playlist } from "../models";
import EditPlaylistModal from '../pages/EditPlaylistModal';
import MediaModal from '../pages/MediaModal';

const EditContext = createContext<{
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
}>({
    showMediaModal: false,
    showPlaylistModal: false,
    viewingMedia: null,
    editingMedia: null,
    viewingPlaylist: null,
    editingPlaylist: null,
    setShowMediaModal: () => { },
    setShowPlaylistModal: () => { },
    setViewingMedia: () => { },
    setEditingMedia: () => { },
    setViewingPlaylist: () => { },
    setEditingPlaylist: () => { }
});

export const EditProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {

    const [showMediaModal, setShowMediaModal] = useState<boolean>(false);
    const [viewingMedia, setViewingMedia] = useState<Media | null>(null);
    const [editingMedia, setEditingMedia] = useState<Media | null>(null);

    const [showPlaylistModal, setShowPlaylistModal] = useState<boolean>(false);
    const [viewingPlaylist, setViewingPlaylist] = useState<Playlist | null>(null);
    const [editingPlaylist, setEditingPlaylist] = useState<Playlist | null>(null);

    return (
        <EditContext.Provider value={{
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
            setEditingPlaylist
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
        </EditContext.Provider>
    );
};

export const useEdits = () => useContext(EditContext);
