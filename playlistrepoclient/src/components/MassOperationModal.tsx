import React, { useState, useEffect } from "react";
import { Modal, Button, Form, Alert } from "react-bootstrap";

export interface MassOperationModalProps<T extends Record<string, any>> {
    title: string;
    show: boolean;
    onHide: () => void;
    /** Initial values for the form. Must include userQuery as string */
    initialValues: T & { userQuery: string };
    /** Called with the final form values on submit */
    onSubmit: (values: T & { userQuery: string }) => Promise<void>;
    /** Optional extra form fields */
    children?: (
        values: T & { userQuery: string },
        updateField: <K extends keyof (T & { userQuery: string }) >(
            field: K,
            value: (T & { userQuery: string })[K]
        ) => void
    ) => React.ReactNode;
}

function MassOperationModal<T extends Record<string, any>>({
    title,
    show,
    onHide,
    initialValues,
    onSubmit,
    children,
}: MassOperationModalProps<T>) {
    const [formValues, setFormValues] = useState(initialValues);
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);

    // Reset form values whenever modal is opened
    useEffect(() => {
        if (show) {
            setFormValues(initialValues);
            setError(null);
        }
    }, [show, initialValues]);

    function updateField<K extends keyof typeof formValues>(
        field: K,
        value: (typeof formValues)[K]
    ) {
        setFormValues((prev) => ({ ...prev, [field]: value }));
    }

    function updateStringField(field: keyof typeof formValues, value: string) {
        setFormValues((prev) => ({ ...prev, [field]: value }));
    }

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setSubmitting(true);
        setError(null);

        try {
            await onSubmit(formValues);
            onHide();
        } catch (err: any) {
            setError(err.message ?? "Submission failed");
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
                        <Form.Label>User Query</Form.Label>
                        <Form.Control
                            type="text"
                            value={formValues.userQuery}
                            onChange={(e) =>
                                updateStringField("userQuery", e.target.value)
                            }
                            required
                        />
                    </Form.Group>

                    {/* Render additional fields if provided */}
                    {children?.(formValues, updateField)}

                    <div className="d-flex justify-content-end">
                        <Button
                            variant="secondary"
                            onClick={onHide}
                            className="me-2"
                        >
                            Cancel
                        </Button>
                        <Button
                            type="submit"
                            variant="primary"
                            disabled={submitting}
                        >
                            {submitting ? "Submitting..." : "Submit"}
                        </Button>
                    </div>
                </Form>
            </Modal.Body>
        </Modal>
    );
}

export default MassOperationModal;
