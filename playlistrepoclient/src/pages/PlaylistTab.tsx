import React from "react";
import MediaView from "../components/MediaView";
import QueryableDropdown from '../components/QueryableDropdown';
import type {Playlist } from "../models";
import Dropdown from 'react-bootstrap/Dropdown';
import { useRefresh } from "../components/RefreshContext";
import { BsPlus } from "react-icons/bs";
import { useEdits } from "../components/EditContext"
import { CopyToClipboardButton } from '../components/CopyToClipboard'
import { BsLink45Deg } from "react-icons/bs";
import { useTasks } from "../components/TaskContext";
import { download } from "../utils";
import Card from 'react-bootstrap/Card';
import Badge from 'react-bootstrap/Badge';

const PlaylistTab: React.FC = () => {

    const { triggerRefresh } = useRefresh();
    const { query, setQuery, setShowPlaylistModal, setEditingPlaylist, viewingPlaylist, setViewingPlaylist } = useEdits(); 
    const { invokeTask } = useTasks()!;

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

        const response = await fetch(`api/data/playlists/${deletionPlaylist.id}`, {
            method: "DELETE"
        });

        if (!response.ok) {
            const errText = await response.text();
            throw new Error(errText || "Failed to delete playlist");
        }

        triggerRefresh();
    }

    async function exportPlaylist(extension: string) {
        if (viewingPlaylist === null) throw new Error();
        await download(`api/play/playlist/${viewingPlaylist.id}${extension}`, `${viewingPlaylist.title}${extension}`);
    }

    async function exportZip() {
        if (viewingPlaylist === null) throw new Error();
        const task = fetch(`api/export/playlist/${viewingPlaylist.id}.zip`);
        invokeTask('exporting zip', task, (record) => {
            download(`api/export/result/${record.guid}`);
        });
    }

    function copyLink() {
        if (viewingPlaylist === null) throw new Error();
        return `${window.location.origin}/api/play/playlist/${viewingPlaylist.id}.xspf`
    }

    return (
        <div>
            <div className="d-flex gap-4">
                <div className="d-flex gap-4" style={{ flex: 2, height: '50px' }}>
                    <QueryableDropdown
                        getLabel={playlist => playlist.title}
                        getPath="api/data/playlists"
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
                            <Dropdown.Item onClick={() => exportPlaylist('.csv')} disabled={viewingPlaylist === null}>CSV</Dropdown.Item>
                            <Dropdown.Item onClick={() => exportZip()} disabled={viewingPlaylist === null}>ZIP</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item onClick={() => createNewPlaylist()}><BsPlus />Create New</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                    <CopyToClipboardButton getText={() => Promise.resolve(copyLink())}><BsLink45Deg /></CopyToClipboardButton>
                </div>

                <div style={{ flex: 1, paddingBottom: '15px' }}>
                    {viewingPlaylist && (
                        <Card style={{ marginRight: 'auto', maxWidth: '40rem' }}>
                            <Card.Header>{viewingPlaylist.title}{" "}<Badge bg="secondary">Playlist Details</Badge></Card.Header>
                            <Card.Body>
                                <div className="d-flex flex-column gap-2">
                                    <div className="d-flex align-items-start">
                                        <Badge
                                            bg="secondary"
                                            className="me-2 text-center flex-shrink-0"
                                            style={{ width: "100px" }}
                                        >
                                            Query
                                        </Badge>
                                        <code>{viewingPlaylist.userQuery}</code>
                                    </div>

                                    <div className="d-flex align-items-start">
                                        <Badge
                                            bg="secondary"
                                            className="me-2 text-center flex-shrink-0"
                                            style={{ width: "100px" }}
                                        >
                                            Description
                                        </Badge>
                                        <span>{viewingPlaylist.description}</span>
                                    </div>
                                </div>
                            </Card.Body>

                        </Card>
                    )}
                </div>
            </div>
            {
                viewingPlaylist === null ? (
                    <div className="mt-3">Select a playlist.</div>
                ) : (
                        <MediaView path={`api/data/playlists/${viewingPlaylist.id}/media`} pageSize={15} query={query} setQuery={setQuery} />
                )
            }
        </div>

    );
};

export default PlaylistTab;
