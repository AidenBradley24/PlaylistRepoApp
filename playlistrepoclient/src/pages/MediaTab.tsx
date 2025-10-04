import React, { useState } from "react";
import MediaView from "../components/MediaView";
import Dropdown from 'react-bootstrap/Dropdown';
import { BsPlus, BsUpload, BsArrowRepeat } from "react-icons/bs";
import { useEdits } from "../components/EditContext";
import { useRefresh } from "../components/RefreshContext"
import type { Playlist, Media } from "../models";
import { useTasks } from "../components/TaskContext";
import { useOpenFileDialog, DragAndDropUploader } from "../components/FileUpload";
import MassOperationMedia from "../components/MassOperationMedia";

const MediaTab: React.FC = () => {

    const [massDeleting, setMassDeleting] = useState<boolean>(false);
    const [massPatching, setMassPatching] = useState<boolean>(false);

    const { query, setQuery, setShowPlaylistModal, setEditingPlaylist, setShowMediaModal, setViewingMediaId, setEditingMedia } = useEdits();
    const { invokeTask } = useTasks()!;
    const { triggerRefresh } = useRefresh()!;
    const { openFileDialog } = useOpenFileDialog("api/actions/upload");

    function createPlaylistFromQuery() {
        const playlist = {} as Playlist;
        playlist.id = 0;
        playlist.title = '';
        playlist.description = '';
        playlist.userQuery = query;
        setEditingPlaylist(playlist);
        setShowPlaylistModal(true);
    }

    function createNewMedia() {
        const media = {} as Media;
        media.id = 0;
        media.title = '';
        media.description = '';
        setEditingMedia(media);
        setViewingMediaId(0);
        setShowMediaModal(true);
    }

    function testTask() {
        const task = fetch("api/service/test", { method: 'POST', headers: { milliseconds: '10000' } });
        invokeTask('running test', task);
    }

    function refreshMedia() {
        const task = fetch('api/action/ingest', { method: 'POST', body: "\"\"", headers: { 'content-type': 'application/json' } });
        invokeTask('ingesting', task, () => {
            triggerRefresh();
        });
    }

    return (
        <div>
            <div className="d-flex gap-4">
                <div className="d-flex gap-4" style={{ flex: 1 }}>
                    <h3>Media</h3>
                </div>
                <div className="d-flex gap-4" style={{ flex: 3 }}>
                    <Dropdown >
                        <Dropdown.Toggle variant="secondary" id="dropdown-basic">
                            Options
                        </Dropdown.Toggle>

                        <Dropdown.Menu>
                            <Dropdown.Header>Media</Dropdown.Header>
                            <Dropdown.Item onClick={() => refreshMedia()}><BsArrowRepeat /> Refresh</Dropdown.Item>
                            <Dropdown.Item onClick={() => openFileDialog()}><BsUpload /> Upload</Dropdown.Item>
                            <Dropdown.Item onClick={() => createNewMedia()}><BsPlus /> Create</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Header>Mass Media Operations</Dropdown.Header>
                            <Dropdown.Item onClick={() => setMassPatching(true)}>Edit</Dropdown.Item>
                            <Dropdown.Item onClick={() => setMassDeleting(true)}>Delete</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Header>Other</Dropdown.Header>
                            <Dropdown.Item onClick={() => createPlaylistFromQuery()}>Create Playlist from Query</Dropdown.Item>
                            <Dropdown.Item onClick={() => testTask()}>Test Server</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                </div>
                <div className="d-flex gap-4" style={{ flex: 3 }}>
                    Drag and drop media onto this page to upload!
                    <br />
                    <br />
                </div>

            </div>

            <DragAndDropUploader uploadUrl="api/action/upload" />

            <MediaView path="api/data/media" pageSize={20} query={query} setQuery={setQuery} />
            <MassOperationMedia massUrl="api/data/media" query={query} massDeleting={massDeleting} massPatching={massPatching} setMassDeleting={setMassDeleting} setMassPatching={setMassPatching} />
        </div>
    );
};

export default MediaTab;