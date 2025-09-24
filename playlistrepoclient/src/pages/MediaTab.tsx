import React, { useState } from "react";
import MediaView from "../components/MediaView";
import Dropdown from 'react-bootstrap/Dropdown';
import { BsPlus, BsUpload, BsArrowRepeat } from "react-icons/bs";
import { useEdits } from "../components/EditContext";
import { useRefresh } from "../components/RefreshContext"
import type { Playlist, Media, Patch } from "../models";
import { useTasks } from "../components/TaskContext";
import { useOpenFileDialog, DragAndDropUploader } from "../components/FileUpload";
import MassOperationModal from "../components/MassOperationModal";
import { Form } from "react-bootstrap";

const MediaTab: React.FC = () => {

    const [massDeleting, setMassDeleting] = useState<boolean>(false);
    const [massPatching, setMassPatching] = useState<boolean>(false);
    const { query, setQuery, setShowPlaylistModal, setEditingPlaylist, setShowMediaModal, setViewingMedia, setEditingMedia } = useEdits();
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
        setViewingMedia(null);
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

    function massDeleteMedia() {
        setMassDeleting(true);
    }

    function massPatchMedia() {
        setMassPatching(true);
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
                            <Dropdown.Item onClick={() => refreshMedia()}><BsArrowRepeat /> Refresh</Dropdown.Item>
                            <Dropdown.Item onClick={() => openFileDialog()}><BsUpload /> Upload</Dropdown.Item>
                            <Dropdown.Item onClick={() => createNewMedia()}><BsPlus /> Create</Dropdown.Item>
                            <Dropdown.Divider />
                            <Dropdown.Header>Mass Operations</Dropdown.Header>
                            <Dropdown.Item onClick={() => massDeleteMedia()}>Delete</Dropdown.Item>
                            <Dropdown.Item onClick={() => massPatchMedia()}>Edit</Dropdown.Item>
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
            <MediaView path="api/data/media" pageSize={20} query={query} setQuery={setQuery} />
            <DragAndDropUploader uploadUrl="api/action/upload" />
            <MassOperationModal
                title="Mass Media Deletion"
                show={massDeleting}
                onHide={() => setMassDeleting(false)}
                initialValues={{ userQuery: query, alsoDeleteFile: true }}
                onSubmit={async (values) => {
                    const response = await fetch("/api/data/media", {
                        method: "DELETE",
                        headers: { "query": values.userQuery, "alsoDeleteFile": values.alsoDeleteFile ? "true" : "false" }
                    });
                    if (!response.ok) {
                        throw new Error(await response.text());
                    }
                    triggerRefresh();
                }}
            >
                {(values, updateField) => (
                    <Form.Group className="mb-3">
                        <Form.Check id="alsoDeleteFile" label="also delete file" checked={values.alsoDeleteFile} onChange={e => updateField("alsoDeleteFile", e.target.value === "true")} />
                    </Form.Group>
                )}
            </MassOperationModal>
            <MassOperationModal<Patch<Media>>
                title="Mass Edit Fields"
                show={massPatching}
                onHide={() => setMassPatching(false)}
                initialValues={{ userQuery: query, propertyName: "title", propertyValue: "", type: "replace" }}
                onSubmit={async (values) => {
                    const response = await fetch("/api/data/media", {
                        method: "PATCH",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(values),
                    });
                    if (!response.ok) {
                        throw new Error(await response.text());
                    }
                    triggerRefresh();
                }}
            >
                {(values, updateField) => (
                    <>
                        {/* Property selector */}
                        <Form.Group className="mb-3">
                            <Form.Label>Property to Edit</Form.Label>
                            <Form.Select
                                value={values.propertyName}
                                onChange={(e) =>
                                    updateField("propertyName", e.target.value as keyof Media)
                                }
                            >
                                {(
                                    [
                                        "title",
                                        "primaryArtist",
                                        "artists",
                                        "album",
                                        "description",
                                        "rating",
                                        "order",
                                    ] as (keyof Media)[]
                                ).map((key) => (
                                    <option key={key} value={key}>
                                        {key}
                                    </option>
                                ))}
                            </Form.Select>
                        </Form.Group>

                        <Form.Group className="mb-3">
                            <Form.Label>Modification Type</Form.Label>
                            <Form.Select
                                value={values.type}
                                onChange={(e) => updateField("type", e.target.value as any)}
                            >
                                {["replace", "append", "prepend"].map((key) =>
                                (
                                    <option key={key} value={key}>
                                        {key}
                                    </option>
                                ))}
                            </Form.Select>
                        </Form.Group>

                        {/* Property value input (dynamic based on type) */}
                        <Form.Group className="mb-3">
                            <Form.Label>Modification</Form.Label>
                            {(() => {
                                switch (values.propertyName) {
                                    case "rating":
                                    case "lengthMilliseconds":
                                    case "order":
                                        return (
                                            <Form.Control
                                                type="number"
                                                value={values.propertyValue ?? "" as any}
                                                onChange={(e) =>
                                                    updateField("propertyValue", Number(e.target.value))
                                                }
                                            />
                                        );
                                    case "artists":
                                        return (
                                            <Form.Control
                                                type="text"
                                                value={(values.propertyValue as string[] | undefined)?.join(", ") ?? ""}
                                                onChange={(e) =>
                                                    updateField(
                                                        "propertyValue",
                                                        e.target.value
                                                            .split(",")
                                                            .map((s) => s.trim())
                                                            .filter(Boolean)
                                                    )
                                                }
                                                placeholder="Comma separated list of artists"
                                            />
                                        );
                                    default:
                                        return (
                                            <Form.Control
                                                type="text"
                                                value={(values.propertyValue as string) ?? ""}
                                                onChange={(e) =>
                                                    updateField("propertyValue", e.target.value)
                                                }
                                            />
                                        );
                                }
                            })()}
                        </Form.Group>
                    </>
                )}
            </MassOperationModal>


        </div>
    );
};

export default MediaTab;