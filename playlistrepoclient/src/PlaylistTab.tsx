import React, { useState } from "react";
import MediaView from "./View";
import QueryableDropdown from './QueryableDropdown';
import CreatePlaylistModal from './CreatePlaylistModal';
import type { Playlist } from "./models";

const PlaylistTab: React.FC = () => {
    const [selectedPlaylist, setSelectedPlaylist] = useState<Playlist | null>(null);
    const [showModal, setShowModal] = useState(false);

    async function createNew() {
        setShowModal(true);
    }

    return (
        <div>
            <div className="d-flex gap-4">
                <div style={{ flex: 1 }}>
                    <QueryableDropdown
                        getLabel={playlist => playlist.title}
                        getPath="data/playlists"
                        menuLabel="Select Playlist"
                        onCreateNew={() => createNew()}
                        onSelection={setSelectedPlaylist}
                    />
                </div>

                <div style={{ flex: 1 }}>
                    {selectedPlaylist && (
                        <div>
                            <h5>Playlist Details</h5>
                            <p><strong>Title:</strong> {selectedPlaylist.title}</p>
                            {selectedPlaylist.description && <p><strong>Description:</strong> {selectedPlaylist.description}</p>}
                            <p><strong>User Query:</strong> {selectedPlaylist.userQuery}</p>
                            <p><strong>Entries:</strong> {selectedPlaylist.bakedEntries.length}</p>
                        </div>
                    )}
                </div>
                <CreatePlaylistModal
                    show={showModal}
                    onHide={() => setShowModal(false)}
                    onCreated={(playlist) => {
                        setSelectedPlaylist(playlist);
                        setShowModal(false);
                    }}
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
