import React, { useState } from "react";
import { Modal, Button, Form, Alert } from "react-bootstrap";
import type { Playlist } from "../models";
import { useRefresh } from "../components/RefreshContext";

interface EditPlaylistModalProps {
    title: string;
    show: boolean;
    onHide: () => void;
    onCreated: (playlist: Playlist) => void;
    editingPlaylist: Playlist | null;
    setEditingPlaylist: (playlist: Playlist | null) => void;
}

const EditPlaylistModal: React.FC<EditPlaylistModalProps> = ({ title, show, onHide, onCreated, editingPlaylist, setEditingPlaylist }) => {
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);
    const { triggerRefresh } = useRefresh();

    if (!editingPlaylist) return;

    function updateField<K extends keyof Playlist>(field: K, value: Playlist[K]) {
        const clone = structuredClone(editingPlaylist)!;
        clone[field] = value;
        setEditingPlaylist(clone);
    }

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setSubmitting(true);
        setError(null);

        try {
            const response = await fetch("api/data/playlists", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(editingPlaylist),
            });

            if (!response.ok) {
                const errText = await response.text();
                throw new Error(errText || "Failed to create playlist");
            }

            const playlist = (await response.json()) as Playlist;
            onCreated(playlist);
            triggerRefresh();
            onHide();
        } catch (err: any) {
            setError(err.message);
        } finally {
            setSubmitting(false);
        }
    }

    return (
        <Modal show={show} onHide={onHide}>
            <Modal.Header closeButton>
                <Modal.Title>{title}</Modal.Title>
            </Modal.Header>
            <Modal.Body>
                {error && <Alert variant="danger">{error}</Alert>}
                <Form onSubmit={handleSubmit}>
                    <Form.Group className="mb-3">
                        <Form.Label>Title</Form.Label>
                        <Form.Control
                            type="text"
                            value={editingPlaylist.title}
                            onChange={(e) => updateField("title", e.target.value)}
                            required
                        />
                    </Form.Group>
                    <Form.Group className="mb-3">
                        <Form.Label>Description</Form.Label>
                        <Form.Control
                            type="text"
                            value={editingPlaylist.description}
                            onChange={(e) => updateField("description", e.target.value)}
                        />
                    </Form.Group>
                    <Form.Group className="mb-3">
                        <Form.Label>User Query</Form.Label>
                        <Form.Control
                            type="text"
                            value={editingPlaylist.userQuery}
                            onChange={(e) => updateField("userQuery", e.target.value)}
                        />
                    </Form.Group>
                    <div className="d-flex justify-content-end">
                        <Button variant="secondary" onClick={onHide} className="me-2">
                            Cancel
                        </Button>
                        <Button type="submit" variant="primary" disabled={submitting}>
                            {submitting ? "Submitting..." : "Submit"}
                        </Button>
                    </div>
                </Form>
            </Modal.Body>
        </Modal>
    );
};

export default EditPlaylistModal;
