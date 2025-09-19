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
import { download } from "../utils";

const RemoteTab: React.FC = () => {

    const { setShowRemoteModal, setEditingRemote, viewingRemote, setViewingRemote } = useEdits(); 
    const [query, setQuery] = useState<string>('');
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
        const deletionRemote = viewingRemote;
        setViewingRemote(null);

        const response = await fetch(`api/data/remotes${deletionRemote.id}`, {
            method: "DELETE"
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
            headers: { "remoteId": `${viewingRemote.id}`}
        });

        invokeTask(`Fetching Remote: ${viewingRemote.name}`, task, triggerRefresh);
    }

    return (
        <div>
            <div className="d-flex gap-4">
                <div className="d-flex gap-4" style={{ flex: 1, height: '50px' }}>
                    <QueryableDropdown
                        getLabel={remote => remote.name}
                        getPath="api/data/remotes"
                        menuLabel="Select Remote Playlist"
                        onCreateNew={() => createNewRemote()}
                        selection={viewingRemote}
                        setSelection={setViewingRemote}
                    />
                    <Dropdown >
                        <Dropdown.Toggle variant="secondary" id="dropdown-basic">
                            Options
                        </Dropdown.Toggle>

                        <Dropdown.Menu>
                            <Dropdown.Item onClick={() => editExistingRemote()} disabled={viewingRemote === null}>Edit</Dropdown.Item>
                            <Dropdown.Item onClick={() => deleteRemote()} disabled={viewingRemote === null}>Delete</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item onClick={() => fetchRemote()} disabled={viewingRemote === null}>Fetch from Remote</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item onClick={() => createNewRemote()}><BsPlus />Add Remote Playlist</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                    {viewingRemote && <CopyToClipboardButton getText={() => Promise.resolve(viewingRemote.link)}><BsLink45Deg /></CopyToClipboardButton>}     
                </div>

                <div style={{ flex: 1 }}>
                    {viewingRemote && (
                        <div>
                            <h5>Remote Playlist Details</h5>
                            <p><strong>Title:</strong> {viewingRemote.name}</p>
                            {viewingRemote.description && <p><strong>Description:</strong> {viewingRemote.description}</p>}
                            <p><strong>Type:</strong> {viewingRemote.type}</p>
                            <p><strong>Link:</strong> <a href={viewingRemote.link}>{viewingRemote.link}</a></p>
                        </div>
                    )}
                </div>
            </div>
            {
                viewingRemote === null ? (
                    <div className="mt-3">Select a playlist.</div>
                ) : (
                     <MediaView path={`api/data/remotes/${viewingRemote.id}/media`} pageSize={15} query={query} setQuery={setQuery} />
                )
            }
        </div>
    );
}

export default RemoteTab;