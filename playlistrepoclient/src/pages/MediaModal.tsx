import React, { useState, useEffect } from "react";
import { Modal, Button, Tabs, Tab, Form, Alert } from "react-bootstrap";
import type { Media } from "../models";
import { useRefresh } from "../components/RefreshContext";
import { formatMillisecondsToHHMMSS } from "../utils";

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

    return (
        <Modal show={show} onHide={onHide} centered size="lg">
            <Modal.Header closeButton>
                <Modal.Title>Media</Modal.Title>
            </Modal.Header>
            <Modal.Body>
                <Tabs defaultActiveKey={viewingMedia ? 'view' : 'edit'} id="media-tabs" className="mb-3">
                    {/* View Tab */}
                    {viewingMedia &&
                        <Tab eventKey="view" title="Details">
                            <div>
                                <p><strong>ID:</strong> {viewingMedia.id}</p>
                                <p><strong>Title:</strong> {viewingMedia.title}</p>
                                <p><strong>Artist:</strong> {viewingMedia.primaryArtist}</p>
                                <p><strong>Album:</strong> {viewingMedia.album}</p>
                                <p><strong>Rating:</strong> {viewingMedia.rating}</p>
                                <p><strong>Length:</strong> {formatMillisecondsToHHMMSS(viewingMedia.lengthMilliseconds)}</p>
                                <p><strong>Type:</strong> {viewingMedia.mimeType}</p>

                                <div style={{ marginTop: "1rem" }}>
                                    {viewingMedia.isOnFile && <div>
                                        <h5>Preview</h5>
                                        {
                                            viewingMedia.mimeType?.startsWith("image/") && (
                                                <img
                                                    src={`api/play/media/${viewingMedia.id}`}
                                                    alt={viewingMedia.title ?? "media"}
                                                    style={{ maxWidth: "100%", borderRadius: "8px" }}
                                                />
                                            )}
                                        {
                                            viewingMedia.mimeType?.startsWith("audio/") && (
                                                <audio controls src={`api/play/media/${viewingMedia.id}`} style={{ width: "100%" }} />
                                            )}
                                        {
                                            viewingMedia.mimeType?.startsWith("video/") && (
                                                <video controls src={`api/play/media/${viewingMedia.id}`} style={{ width: "100%", borderRadius: "8px" }} />
                                            )}
                                        {
                                            viewingMedia.mimeType?.startsWith("text/") && (
                                                <iframe
                                                    src={`api/play/media/${viewingMedia.id}`}
                                                    title="text preview"
                                                    style={{ width: "100%", height: "300px", border: "1px solid #ccc", borderRadius: "8px" }}
                                                />
                                            )}
                                        {!viewingMedia.mimeType && <p>No preview available</p>}
                                    </div>
                                    }
                                    {!viewingMedia.isOnFile && <p>This media has no content yet.</p>}
                                </div>
                            </div>
                        </Tab>
                    }

                    {/* Edit Tab */}
                    {
                        editingMedia &&
                        <Tab eventKey="edit" title={editingMedia.id === 0 ? 'Create' : 'Edit'}>
                            {error && <Alert variant="danger">{error}</Alert>}
                                <Form onSubmit={handleSubmit}>
                                    {editingMedia.id !== 0 && <Form.Group className="mb-3">
                                        <Form.Label>ID</Form.Label>
                                        <Form.Control type="text" value={editingMedia.id} readOnly />
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
                                        onChange={(e) => updateField("primaryArtist", e.target.value)}
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
                                        max={5}
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
