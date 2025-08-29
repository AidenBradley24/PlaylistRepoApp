import React, { useState } from "react";
import MediaView from "./View";
import Dropdown from 'react-bootstrap/Dropdown';
import { BsPlus } from "react-icons/bs";
import { useEdits } from "./EditContext";
import type { Playlist, Media } from "./models";
import { useTasks } from "./TaskContext";

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
        const task = fetch("service/test", { method: 'POST', headers: { milliseconds: '10000' } });
        invokeTask(task);
    }

    return (
        <div>
            <div className="d-flex gap-4">
                <div className="d-flex gap-4" style={{ flex: 1 }}>
                    <Dropdown >
                        <Dropdown.Toggle variant="secondary" id="dropdown-basic">
                            Options
                        </Dropdown.Toggle>

                        <Dropdown.Menu>
                            <Dropdown.Item onClick={() => createPlaylistFromQuery()}>Create Playlist from Query</Dropdown.Item>
                            <Dropdown.Item onClick={() => testTask()}><BsPlus />Test</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Item onClick={() => createNewMedia()}><BsPlus />Create New</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                </div>
            </div>
            <h3>Media</h3>

            <MediaView path="data/media" pageSize={20} query={query} setQuery={setQuery} />
        </div>
    );
};

export default MediaTab;