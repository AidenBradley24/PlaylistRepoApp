import React, { useState } from "react";
import MediaView from "./View";
import QueryableDropdown from './QueryableDropdown';
import EditPlaylistModal from './EditPlaylistModal';
import type {Playlist } from "./models";
import Dropdown from 'react-bootstrap/Dropdown';
import { useRefresh } from "./RefreshContext";
import { BsPlus } from "react-icons/bs";
import { useEdits } from "./EditContext"

const PlaylistTab: React.FC = () => {
    const [selectedPlaylist, setSelectedPlaylist] = useState<Playlist | null>(null);
    const [showModal, setShowModal] = useState(false);

    const [recDropSelection, setRecDropSelection] = useState<Playlist>();

    const { triggerRefresh } = useRefresh();
    const { editingPlaylist, setEditingPlaylist } = useEdits(); 

    async function createNewPlaylist() {
        const playlist = {} as Playlist;
        playlist.title = '';
        playlist.description = '';
        playlist.userQuery = '';
        setEditingPlaylist(playlist);
        setShowModal(true);
    }

    async function editExistingPlaylist() {
        if (selectedPlaylist === null) throw new Error();
        setEditingPlaylist(selectedPlaylist);
        setShowModal(true);
    }

    async function deletePlaylist () {
        if (selectedPlaylist === null) throw new Error();
        const response = await fetch("data/playlists", {
            method: "DELETE",
            headers: { "id": `${selectedPlaylist.id}` }
        });

        if (!response.ok) {
            const errText = await response.text();
            throw new Error(errText || "Failed to delete playlist");
        }

        triggerRefresh();
    }

    async function exportPlaylist(extension: string) {
        if (selectedPlaylist === null) throw new Error();
        const response = await fetch(`play/playlist/${selectedPlaylist.id}${extension}`);
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute(
            'download',
            `${selectedPlaylist.title}${extension}`,
        );
        document.body.appendChild(link);
        link.click();
        link.parentNode?.removeChild(link);
    }

    return (
        <div>
            <div className="d-flex gap-4">
                <div className="d-flex gap-4" style={{ flex: 1 }}>
                    <QueryableDropdown
                        getLabel={playlist => playlist.title}
                        getPath="data/playlists"
                        menuLabel="Select Playlist"
                        onCreateNew={() => createNewPlaylist()}
                        onSelection={setSelectedPlaylist}
                        recommendedSelection={recDropSelection}
                    />
                    <Dropdown >
                        <Dropdown.Toggle variant="secondary" id="dropdown-basic">
                            Options
                        </Dropdown.Toggle>

                        <Dropdown.Menu>
                            <Dropdown.Item onClick={() => editExistingPlaylist()} disabled={selectedPlaylist === null}>Edit</Dropdown.Item>
                            <Dropdown.Item onClick={() => deletePlaylist()} disabled={selectedPlaylist === null}>Delete</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Header>Export</Dropdown.Header>
                            <Dropdown.Item onClick={() => exportPlaylist('.xspf')} disabled={selectedPlaylist === null}>XSPF</Dropdown.Item>
                            <Dropdown.Item onClick={() => exportPlaylist('.m3u8')} disabled={selectedPlaylist === null}>M3U8</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item onClick={() => createNewPlaylist()}><BsPlus />Create New</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                </div>

                <div style={{ flex: 1 }}>
                    {selectedPlaylist && (
                        <div>
                            <h5>Playlist Details</h5>
                            <p><strong>Title:</strong> {selectedPlaylist.title}</p>
                            {selectedPlaylist.description && <p><strong>Description:</strong> {selectedPlaylist.description}</p>}
                            <p><strong>User Query:</strong> {selectedPlaylist.userQuery}</p>
                        </div>
                    )}
                </div>
                <EditPlaylistModal
                    title={editingPlaylist?.id === selectedPlaylist?.id ? "Edit Playlist" : "Create Playlist"}
                    show={showModal}
                    onHide={() => setShowModal(false)}
                    onCreated={(playlist) => {
                        setShowModal(false);
                        setRecDropSelection(playlist);
                    }}
                    editingPlaylist={editingPlaylist}
                    setEditingPlaylist={setEditingPlaylist}
                />
            </div>
            {
                selectedPlaylist === null ? (
                    <div className="mt-3">Select a playlist.</div>
                ) : (
                    <MediaView path={`data/playlists/${selectedPlaylist.id}/media`} pageSize={15} />
                )
            }
        </div>

    );
};

export default PlaylistTab;
