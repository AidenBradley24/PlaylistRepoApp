import React, { useState } from "react";
import MediaView from "../components/MediaView";
import QueryableDropdown from '../components/QueryableDropdown';
import type { RemotePlaylist } from "../models";
import Dropdown from 'react-bootstrap/Dropdown';
import { useRefresh } from "../components/RefreshContext";
import { BsPlus } from "react-icons/bs";
import { useEdits } from "../components/EditContext"
import { CopyToClipboardButton } from '../components/CopyToClipboard'
import { BsLink45Deg } from "react-icons/bs";
import { useTasks } from "../components/TaskContext";
import Card from 'react-bootstrap/Card';
import Badge from 'react-bootstrap/Badge';
import MassOperationMedia from "../components/MassOperationMedia";

const RemoteTab: React.FC = () => {

    const [massDeleting, setMassDeleting] = useState<boolean>(false);
    const [massPatching, setMassPatching] = useState<boolean>(false);

    const { query, setQuery, setShowRemoteModal, setEditingRemote, viewingRemote, setViewingPlaylistId, viewingPlaylistId } = useEdits();
    const { triggerRefresh } = useRefresh();
    const { invokeTask } = useTasks();

    async function createNewRemote() {
        const playlist = {} as RemotePlaylist;
        playlist.id = 0;
        playlist.name = '';
        playlist.description = '';
        playlist.link = '';
        setEditingRemote(playlist);
        setShowRemoteModal(true);
    }

    async function editExistingRemote() {
        if (viewingRemote === null) throw new Error();
        setEditingRemote(viewingRemote);
        setShowRemoteModal(true);
    }

    async function deleteRemote() {
        if (viewingRemote === null) throw new Error();
        if (!window.confirm(`Are you sure you want to delete this remote?: '${viewingRemote.name}'`)) return;

        const deletionRemote = viewingRemote;
        setViewingPlaylistId(0);

        const response = await fetch(`api/data/remotes/${deletionRemote.id}`, {
            method: "DELETE",
            headers: {
                alsoDeleteMedia: 'true',
                alsoDeleteMediaFiles: 'true'
            }
        });

        if (!response.ok) {
            const errText = await response.text();
            throw new Error(errText || "Failed to delete remote");
        }

        triggerRefresh();
    }

    async function fetchRemote() {
        if (viewingRemote === null) throw new Error();
        const task = fetch("api/action/fetch", {
            method: "POST",
            headers: { "remoteId": `${viewingRemote.id}` }
        });

        invokeTask(`Fetching Remote: ${viewingRemote.name}`, task, triggerRefresh);
    }

    return (
        <div>
            <div className="d-flex gap-4">
                <div className="d-flex gap-4" style={{ flex: 1, height: '50px' }}>
                    <QueryableDropdown
                        getLabel={remote => remote?.name}
                        getPath="api/data/remotes"
                        menuLabel="Select Remote Playlist"
                        onCreateNew={() => createNewRemote()}
                        selection={viewingPlaylistId}
                        setSelection={setViewingPlaylistId}
                    />
                    <Dropdown >
                        <Dropdown.Toggle variant="secondary" id="dropdown-basic">
                            Options
                        </Dropdown.Toggle>

                        <Dropdown.Menu>
                            <Dropdown.Header>Remote</Dropdown.Header>
                            <Dropdown.Item onClick={() => createNewRemote()}><BsPlus />Add New</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item onClick={() => editExistingRemote()} disabled={viewingRemote === null}>Edit Remote</Dropdown.Item>
                            <Dropdown.Item onClick={() => deleteRemote()} disabled={viewingRemote === null}>Delete Remote</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Header>Mass Media Operations</Dropdown.Header>
                            <Dropdown.Item onClick={() => setMassPatching(true)}>Edit</Dropdown.Item>
                            <Dropdown.Item onClick={() => setMassDeleting(true)}>Delete</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item onClick={() => fetchRemote()} disabled={viewingRemote === null}>Fetch from Remote</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                    {viewingRemote && <CopyToClipboardButton getText={() => Promise.resolve(viewingRemote.link)}><BsLink45Deg /></CopyToClipboardButton>}
                </div>

                <div style={{ flex: 1, paddingBottom: '15px' }}>
                    {viewingRemote && (
                        <Card style={{ marginRight: 'auto', maxWidth: '40rem' }}>
                            <Card.Header>{viewingRemote.name}{" "}<Badge bg="secondary">Playlist Details</Badge></Card.Header>
                            <Card.Body>
                                <div className="d-flex align-items-start">
                                    <Badge bg="secondary" className="me-2 text-center flex-shrink-0" style={{ width: "100px" }}>
                                        Type
                                    </Badge>
                                    <span>{viewingRemote.type}</span>
                                </div>
                                <div className="d-flex align-items-start">
                                    <Badge bg="secondary" className="me-2 text-center flex-shrink-0" style={{ width: "100px" }}>
                                        Link
                                    </Badge>
                                    <a href={viewingRemote.link}>{viewingRemote.link}</a>
                                </div>
                                <div className="d-flex align-items-start">
                                    <Badge bg="secondary" className="me-2 text-center flex-shrink-0" style={{ width: "100px" }}>
                                        Description
                                    </Badge>
                                    <span>{viewingRemote.description}</span>
                                </div>
                            </Card.Body>
                        </Card>
                    )}
                </div>
            </div>
            {
                viewingRemote === null ? (
                    <div className="mt-3">Select a playlist.</div>
                ) : (
                    <>
                        <MediaView path={`api/data/remotes/${viewingRemote.id}/media`} pageSize={15} query={query} setQuery={setQuery} />
                        <MassOperationMedia massUrl={`api/data/remotes/${viewingRemote.id}/media`} query={query} massDeleting={massDeleting} massPatching={massPatching} setMassDeleting={setMassDeleting} setMassPatching={setMassPatching} />
                    </>             
                )
            }
        </div>
    );
}

export default RemoteTab;