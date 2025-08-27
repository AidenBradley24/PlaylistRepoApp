import React, { createContext, useContext, useState } from "react";
import type { Media, Playlist } from "./models";

const EditContext = createContext<{
    editingPlaylist: Playlist | null;
    setEditingPlaylist: (value: Playlist | null) => void;
    editingMedia: Media | null;
    setEditingMedia: (value: Media | null) => void;
}>({ editingMedia: null, editingPlaylist: null, setEditingMedia: () => { }, setEditingPlaylist: () => {} });

export const EditProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {

    const [editingPlaylist, setEditingPlaylist] = useState<Playlist | null>(null);
    const [editingMedia, setEditingMedia] = useState<Media | null>(null);

    return (
        <EditContext.Provider value={{ editingMedia, setEditingMedia, editingPlaylist, setEditingPlaylist }}>
            {children}
        </EditContext.Provider>
    );
};

export const useEdits = () => useContext(EditContext);
