import React, { useState } from "react";
import { Modal, Button, Form, Alert } from "react-bootstrap";
import type { RemotePlaylist } from "../models";
import { useRefresh } from "../components/RefreshContext";

interface EditRemoteModalProps {
    title: string;
    show: boolean;
    onHide: () => void;
    onCreated: (playlist: RemotePlaylist) => void;
    editingPlaylist: RemotePlaylist | null;
    setEditingPlaylist: (playlist: RemotePlaylist | null) => void;
}

const EditRemoteModal: React.FC<EditRemoteModalProps> = ({ title, show, onHide, onCreated, editingPlaylist, setEditingPlaylist }) => {
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);
    const { triggerRefresh } = useRefresh();

    if (!editingPlaylist) return;

    function updateField<K extends keyof RemotePlaylist>(field: K, value: RemotePlaylist[K]) {
        const clone = structuredClone(editingPlaylist)!;
        clone[field] = value;
        setEditingPlaylist(clone);
    }

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setSubmitting(true);
        setError(null);

        try {
            const response = await fetch("api/data/remotes", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(editingPlaylist),
            });

            if (!response.ok) {
                const errText = await response.text();
                throw new Error(errText || "Failed to create playlist");
            }

            const playlist = (await response.json()) as RemotePlaylist;
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
                        <Form.Label>Name</Form.Label>
                        <Form.Control
                            type="text"
                            value={editingPlaylist.name}
                            onChange={(e) => updateField("name", e.target.value)}
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
                        <Form.Label>Type</Form.Label>
                        <Form.Select
                            value={editingPlaylist.type}
                            onChange={(e) => updateField("type", e.target.value)}
                            required
                        >
                            <option value="">-- Select a Type --</option>
                            <option value="internet">Internet</option>
                            <option value="ytdlp">YT-DLP</option>
                        </Form.Select>
                    </Form.Group>
                    <Form.Group className="mb-3">
                        <Form.Label>Media Type</Form.Label>
                        <Form.Select
                            value={editingPlaylist.mediaMime}
                            onChange={(e) => updateField("mediaMime", e.target.value)}
                            defaultValue={""}
                        >
                            <option value="">(auto)</option>
                            <option value="audio/mp3">audio/mp3</option>
                            <option value="video/mp4">video/mp4</option>
                        </Form.Select>
                    </Form.Group>
                    <Form.Group className="mb-3">
                        <Form.Label>Link</Form.Label>
                        <Form.Control
                            type="text"
                            value={editingPlaylist.link}
                            onChange={(e) => updateField("link", e.target.value)}
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

export default EditRemoteModal;
