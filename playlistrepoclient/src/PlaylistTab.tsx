import React, { useState } from "react";
import MediaView from "./View";
import QueryableDropdown from './QueryableDropdown';
import type {Playlist } from "./models";
import Dropdown from 'react-bootstrap/Dropdown';
import { useRefresh } from "./RefreshContext";
import { BsPlus } from "react-icons/bs";
import { useEdits } from "./EditContext"
import { CopyToClipboardButton } from './CopyToClipboard'
import { BsLink45Deg } from "react-icons/bs";

const PlaylistTab: React.FC = () => {

    const { triggerRefresh } = useRefresh();
    const { setShowPlaylistModal, setEditingPlaylist, viewingPlaylist, setViewingPlaylist } = useEdits(); 

    const [query, setQuery] = useState<string>('');

    async function createNewPlaylist() {
        const playlist = {} as Playlist;
        playlist.id = 0;
        playlist.title = '';
        playlist.description = '';
        playlist.userQuery = '';
        setEditingPlaylist(playlist);
        setShowPlaylistModal(true);
    }

    async function editExistingPlaylist() {
        if (viewingPlaylist === null) throw new Error();
        setEditingPlaylist(viewingPlaylist);
        setShowPlaylistModal(true);
    }

    async function deletePlaylist () {
        if (viewingPlaylist === null) throw new Error();
        const deletionPlaylist = viewingPlaylist;
        setViewingPlaylist(null);

        const response = await fetch("data/playlists", {
            method: "DELETE",
            headers: { "id": `${deletionPlaylist.id}` }
        });

        if (!response.ok) {
            const errText = await response.text();
            throw new Error(errText || "Failed to delete playlist");
        }

        triggerRefresh();
    }

    async function exportPlaylist(extension: string) {
        if (viewingPlaylist === null) throw new Error();
        const response = await fetch(`play/playlist/${viewingPlaylist.id}${extension}`);
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute(
            'download',
            `${viewingPlaylist.title}${extension}`,
        );
        document.body.appendChild(link);
        link.click();
        link.parentNode?.removeChild(link);
    }

    function copyLink() {
        if (viewingPlaylist === null) throw new Error();
        return `${window.location.origin}/play/playlist/${viewingPlaylist.id}.xspf`
    }

    return (
        <div>
            <div className="d-flex gap-4">
                <div className="d-flex gap-4" style={{ flex: 1, height: '50px' }}>
                    <QueryableDropdown
                        getLabel={playlist => playlist.title}
                        getPath="data/playlists"
                        menuLabel="Select Playlist"
                        onCreateNew={() => createNewPlaylist()}
                        selection={viewingPlaylist}
                        setSelection={setViewingPlaylist}
                    />
                    <Dropdown >
                        <Dropdown.Toggle variant="secondary" id="dropdown-basic">
                            Options
                        </Dropdown.Toggle>

                        <Dropdown.Menu>
                            <Dropdown.Item onClick={() => editExistingPlaylist()} disabled={viewingPlaylist === null}>Edit</Dropdown.Item>
                            <Dropdown.Item onClick={() => deletePlaylist()} disabled={viewingPlaylist === null}>Delete</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Header>Export</Dropdown.Header>
                            <Dropdown.Item onClick={() => exportPlaylist('.xspf')} disabled={viewingPlaylist === null}>XSPF</Dropdown.Item>
                            <Dropdown.Item onClick={() => exportPlaylist('.m3u8')} disabled={viewingPlaylist === null}>M3U8</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item onClick={() => createNewPlaylist()}><BsPlus />Create New</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                    <CopyToClipboardButton getText={() => Promise.resolve(copyLink())}><BsLink45Deg /></CopyToClipboardButton>
                </div>

                <div style={{ flex: 1 }}>
                    {viewingPlaylist && (
                        <div>
                            <h5>Playlist Details</h5>
                            <p><strong>Title:</strong> {viewingPlaylist.title}</p>
                            {viewingPlaylist.description && <p><strong>Description:</strong> {viewingPlaylist.description}</p>}
                            <p><strong>User Query:</strong> {viewingPlaylist.userQuery}</p>
                        </div>
                    )}
                </div>
            </div>
            {
                viewingPlaylist === null ? (
                    <div className="mt-3">Select a playlist.</div>
                ) : (
                        <MediaView path={`data/playlists/${viewingPlaylist.id}/media`} pageSize={15} query={query} setQuery={setQuery} />
                )
            }
        </div>

    );
};

export default PlaylistTab;
