import React, { useState, useEffect } from "react";
import { Row, Col, Modal, Button, Tabs, Tab, Form, Alert } from "react-bootstrap";
import type { Media } from "../models";
import { useRefresh } from "../components/RefreshContext";
import { formatMillisecondsToHHMMSS } from "../utils";
import Rating from "../components/Rating";

interface MediaModalProps {
    viewingMedia: Media | null;
    editingMedia: Media | null;
    setEditingMedia: (media: Media) => void;
    show: boolean;
    onHide: () => void;
    onSaved: (media: Media) => void;
}

const MediaModal: React.FC<MediaModalProps> = ({ show, onHide, viewingMedia, onSaved, editingMedia, setEditingMedia }) => {
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);
    const { triggerRefresh } = useRefresh();

    const [editingMediaArtists, setEditingMediaArtists] = useState<string>("");

    useEffect(() => {
        setEditingMediaArtists("");
    }, [editingMedia?.id])

    useEffect(() => {
        if (!editingMedia) return;
        if (!editingMedia.artists || editingMediaArtists === "") {
            setEditingMediaArtists(editingMedia.artists?.join(",") ?? "");
            return;
        }
    }, [editingMedia?.artists, editingMediaArtists])

    useEffect(() => {
        if (viewingMedia) {
            const clone = structuredClone(viewingMedia);
            setEditingMedia(clone);
        }
        else if (editingMedia) {

        }
    }, [viewingMedia])

    function updateField<K extends keyof Media>(field: K, value: Media[K]) {
        if (!editingMedia) return;
        const clone = structuredClone(editingMedia);
        clone[field] = value;
        setEditingMedia(clone);
    }

    function editArtists(artists: string) {
        if (!editingMedia) return;
        setEditingMediaArtists(artists);
        const arr = artists.split(",").map(a => a.trim());
        const clone = structuredClone(editingMedia);
        clone.artists = arr;
        if (arr.length > 0) {
            clone.primaryArtist = arr[0];
        } else {
            clone.primaryArtist = "";
        }
        setEditingMedia(clone);
    }

    function editArtist(artist: string) {
        if (!editingMedia) return;
        const clone = structuredClone(editingMedia);
        artist = artist.replace(',', '');
        clone.primaryArtist = artist;
        if (!clone.artists || clone.artists.length === 0) {
            clone.artists = [artist];
        } else {
            clone.artists[0] = artist;
        }
        setEditingMediaArtists(clone.artists?.join(","));
        setEditingMedia(clone);
    }

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setSubmitting(true);
        setError(null);

        try {
            const response = await fetch("api/data/media", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(editingMedia),
            });

            if (!response.ok) {
                const errText = await response.text();
                throw new Error(errText || "Failed to save media");
            }

            const savedMedia = (await response.json()) as Media;
            onSaved(savedMedia);
            triggerRefresh();
            onHide();
        } catch (err: any) {
            setError(err.message);
        } finally {
            setSubmitting(false);
        }
    }

    const isCreate = editingMedia && editingMedia.id === 0;
    const modalTitle = isCreate ? "Create Media Record" : `${viewingMedia?.title} - Preview`;

    return (
        <Modal show={show} onHide={onHide} centered size="lg">
            <Modal.Header closeButton>
                <Modal.Title>{modalTitle}</Modal.Title>
            </Modal.Header>
            <Modal.Body>
                <Tabs defaultActiveKey={isCreate ? 'edit' : 'view'} id="media-tabs" className="mb-3">
                    {/* View Tab */}
                    {viewingMedia &&
                        <Tab eventKey="view" title="Details">
                            <div>
                                <Row className="mb-2">
                                    <Col md={4}><strong>ID:</strong> {viewingMedia.id}</Col>
                                    <Col md={4}><strong>Title:</strong> {viewingMedia.title}</Col>
                                    <Col md={4}><strong>Artists:</strong> {viewingMedia?.artists?.join(", ")}</Col>
                                </Row>
                                <Row className="mb-2">
                                    <Col md={4}><strong>Album:</strong> {viewingMedia.album}</Col>
                                    <Col md={4}><strong>Rating:</strong> <Rating rating={viewingMedia.rating} /></Col>
                                    <Col md={4}><strong>Length:</strong> {formatMillisecondsToHHMMSS(viewingMedia.lengthMilliseconds)}</Col>
                                </Row>
                                <Row className="mb-3">
                                    <Col md={4}><strong>Type:</strong> {viewingMedia.mimeType}</Col>
                                    <Col md={4}><strong>Genre:</strong> {viewingMedia.genre}</Col>
                                </Row>

                                <div style={{ marginTop: "1rem" }}>
                                    {viewingMedia.isOnFile ? (
                                        <div>
                                            {viewingMedia.mimeType?.startsWith("image/") && (
                                                <img
                                                    src={`api/play/media/preview/${viewingMedia.id}`}
                                                    alt={viewingMedia.title ?? "media"}
                                                    style={{ maxWidth: "100%", borderRadius: "8px" }}
                                                />
                                            )}
                                            {viewingMedia.mimeType?.startsWith("audio/") && (
                                                <audio
                                                    controls
                                                    src={`api/play/media/preview/${viewingMedia.id}`}
                                                    style={{ width: "100%" }}
                                                />
                                            )}
                                            {viewingMedia.mimeType?.startsWith("video/") && (
                                                <video
                                                    controls
                                                    src={`api/play/media/preview/${viewingMedia.id}`}
                                                    style={{ width: "100%", borderRadius: "8px" }}
                                                />
                                            )}
                                            {viewingMedia.mimeType?.startsWith("text/") && (
                                                <iframe
                                                    src={`api/play/media/preview/${viewingMedia.id}`}
                                                    title="text preview"
                                                    style={{
                                                        width: "100%",
                                                        height: "300px",
                                                        border: "1px solid #ccc",
                                                        borderRadius: "8px",
                                                    }}
                                                />
                                            )}
                                            {!viewingMedia.mimeType && <p>No preview available</p>}
                                        </div>
                                    ) : (
                                        <p>This media has no content yet.</p>
                                    )}
                                </div>
                            </div>

                        </Tab>
                    }

                    {/* Edit Tab */}
                    {
                        editingMedia &&
                        <Tab eventKey="edit" title={isCreate ? 'Create' : 'Edit'}>
                            {error && <Alert variant="danger">{error}</Alert>}
                            <Form onSubmit={handleSubmit}>
                                {editingMedia.id !== 0 &&
                                <Form.Group className="mb-3">
                                    <Form.Label>ID</Form.Label>
                                    <Form.Control type="text" value={editingMedia.id} readOnly />
                                    <Form.Check
                                        type="switch"
                                        label="Locked"
                                        checked={editingMedia.locked}
                                        onChange={(e) => updateField("locked", e.target.checked)}
                                    />
                                </Form.Group>}
                                <Form.Group className="mb-3">
                                    <Form.Label>Title</Form.Label>
                                    <Form.Control
                                        type="text"
                                        value={editingMedia.title}
                                        onChange={(e) => updateField("title", e.target.value)}
                                        required
                                    />
                                </Form.Group>
                                <Form.Group className="mb-3">
                                    <Form.Label>Primary Artist</Form.Label>
                                    <Form.Control
                                        type="text"
                                        value={editingMedia.primaryArtist}
                                        onChange={(e) => editArtist(e.target.value)}
                                    />
                                </Form.Group>
                                <Form.Group className="mb-3">
                                    <Form.Label>All Artists</Form.Label>
                                    <Form.Control
                                        type="text"
                                        value={editingMediaArtists}
                                        onChange={(e) => editArtists(e.target.value)}
                                    />
                                </Form.Group>
                                <Form.Group className="mb-3">
                                    <Form.Label>Album</Form.Label>
                                    <Form.Control
                                        type="text"
                                        value={editingMedia.album}
                                        onChange={(e) => updateField("album", e.target.value)}
                                    />
                                </Form.Group>
                                <Form.Group className="mb-3">
                                    <Form.Label>Description</Form.Label>
                                    <Form.Control
                                        as="textarea"
                                        rows={2}
                                        value={editingMedia.description}
                                        onChange={(e) => updateField("description", e.target.value)}
                                    />
                                </Form.Group>
                                <Form.Group className="mb-3">
                                    <Form.Label>Rating</Form.Label>
                                    <Form.Control
                                        type="number"
                                        min={0}
                                        max={10}
                                        value={editingMedia.rating}
                                        onChange={(e) => updateField("rating", parseInt(e.target.value))}
                                    />
                                </Form.Group>
                                <Form.Group className="mb-3">
                                    <Form.Label>Order</Form.Label>
                                    <Form.Control
                                        type="number"
                                        value={editingMedia.order}
                                        onChange={(e) => updateField("order", parseInt(e.target.value))}
                                    />
                                </Form.Group>
                                <Form.Group className="mb-3">
                                    <Form.Label>Genre</Form.Label>
                                    <Form.Control
                                        type="text"
                                        value={editingMedia.genre}
                                        onChange={(e) => updateField("genre", e.target.value)}
                                    />
                                </Form.Group>
                                <div className="d-flex justify-content-end">
                                    <Button variant="secondary" onClick={onHide} className="me-2">
                                        Cancel
                                    </Button>
                                    <Button type="submit" variant="primary" disabled={submitting}>
                                        {submitting ? "Saving..." : "Save"}
                                    </Button>
                                </div>
                            </Form>
                        </Tab>
                    }
                </Tabs>
            </Modal.Body>
        </Modal>
    );
};

export default MediaModal;
