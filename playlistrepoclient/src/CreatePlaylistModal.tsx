import React, { useState } from "react";
import { Modal, Button, Form, Alert } from "react-bootstrap";
import type { Playlist } from "./models";

interface CreatePlaylistModalProps {
    show: boolean;
    onHide: () => void;
    onCreated: (playlist: Playlist) => void;
}

const CreatePlaylistModal: React.FC<CreatePlaylistModalProps> = ({ show, onHide, onCreated }) => {
    const [title, setTitle] = useState("");
    const [description, setDescription] = useState("");
    const [userQuery, setUserQuery] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setSubmitting(true);
        setError(null);

        try {
            const response = await fetch("data/playlists", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    title,
                    description: description || undefined,
                    userQuery,
                    bakedEntries: [],
                }),
            });

            if (!response.ok) {
                const errText = await response.text();
                throw new Error(errText || "Failed to create playlist");
            }

            const playlist = (await response.json()) as Playlist;
            onCreated(playlist);
            setTitle("");
            setDescription("");
            setUserQuery("");
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
                <Modal.Title>Create Playlist Menu</Modal.Title>
            </Modal.Header>
            <Modal.Body>
                {error && <Alert variant="danger">{error}</Alert>}
                <Form onSubmit={handleSubmit}>
                    <Form.Group className="mb-3">
                        <Form.Label>Title</Form.Label>
                        <Form.Control
                            type="text"
                            value={title}
                            onChange={(e) => setTitle(e.target.value)}
                            required
                        />
                    </Form.Group>
                    <Form.Group className="mb-3">
                        <Form.Label>Description</Form.Label>
                        <Form.Control
                            type="text"
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                        />
                    </Form.Group>
                    <Form.Group className="mb-3">
                        <Form.Label>User Query</Form.Label>
                        <Form.Control
                            type="text"
                            value={userQuery}
                            onChange={(e) => setUserQuery(e.target.value)}
                            required
                        />
                    </Form.Group>
                    <div className="d-flex justify-content-end">
                        <Button variant="secondary" onClick={onHide} className="me-2">
                            Cancel
                        </Button>
                        <Button type="submit" variant="primary" disabled={submitting}>
                            {submitting ? "Creating..." : "Create"}
                        </Button>
                    </div>
                </Form>
            </Modal.Body>
        </Modal>
    );
};

export default CreatePlaylistModal;
