import React, { useState } from "react";
import MediaView from "../components/MediaView";
import Dropdown from 'react-bootstrap/Dropdown';
import { BsPlus } from "react-icons/bs";
import { useEdits } from "../components/EditContext";
import type { Playlist, Media } from "../models";
import { useTasks } from "../components/TaskContext";
import { useOpenFileDialog, DragAndDropUploader } from "../components/FileUpload";

const MediaTab: React.FC = () => {

    const [query, setQuery] = useState<string>('');
    const { setShowPlaylistModal, setEditingPlaylist, setShowMediaModal, setViewingMedia, setEditingMedia } = useEdits();
    const { invokeTask } = useTasks()!;

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
        setViewingMedia(null);
        setShowMediaModal(true);
    }

    function testTask() {
        const task = fetch("api/service/test", { method: 'POST', headers: { milliseconds: '10000' } });
        invokeTask('running test', task);
    }

    function uploadMedia() {
        useOpenFileDialog("api/actions/upload");
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
                            <Dropdown.Item onClick={() => createPlaylistFromQuery()}>Create Playlist from Query</Dropdown.Item>
                            <Dropdown.Item onClick={() => testTask()}>Test</Dropdown.Item>
                            <Dropdown.Item onClick={() => uploadMedia()}>Upload Media</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item onClick={() => createNewMedia()}><BsPlus />Create New</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                </div>
                <div className="d-flex gap-4" style={{ flex: 3 }}>
                    Drag and drop media onto this page to upload!
                    <br />
                    <br />
                </div>

            </div>
            <MediaView path="api/data/media" pageSize={20} query={query} setQuery={setQuery} />
            <DragAndDropUploader uploadUrl="api/action/upload" />
        </div>
    );
};

export default MediaTab;