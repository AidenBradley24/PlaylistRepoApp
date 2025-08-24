import React, { useEffect, useState } from "react";
import { Table, Form, Button, Pagination, Spinner } from "react-bootstrap";
import type { Response, Media } from "./models";
import Modal from "react-bootstrap/Modal";
import { useRefresh } from "./RefreshContext";

import { BsSortDown, BsSortUp } from "react-icons/bs";

import "./records.css";

export interface MediaViewProps {
    path: string;
    pageSize?: number
}

const MediaView: React.FC<MediaViewProps> = ({ path, pageSize = 20 }) => {
    const [records, setRecords] = useState<Media[]>([]);
    const [page, setPage] = useState(1);
    const [total, setTotal] = useState(0);

    const [selected, setSelected] = useState<Media | null>(null);
    const [showModal, setShowModal] = useState(false);

    const [sortColumn, setSortColumn] = useState<string>('id');
    const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc");

    // Immediate filter (bound to input)
    const [filter, setFilter] = useState<string>('');
    // Debounced filter (actually used for queries)
    const [debouncedFilter, setDebouncedFilter] = useState<string>('');

    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState<boolean>(false);

    const { refreshKey } = useRefresh(); // subscribe to refresh key

    /** Debounce filter input */
    useEffect(() => {
        const handler = setTimeout(() => {
            setDebouncedFilter(filter);
            setPage(1); // reset to first page on new filter
        }, 500);

        return () => {
            clearTimeout(handler);
        };
    }, [filter]);

    /** Fetch records when query params change */
    useEffect(() => {
        const loadData = async () => {
            setLoading(true);
            setError(null);

            try {
                const query = `${debouncedFilter} orderby${sortDirection === "desc" ? "descending" : ""} ${sortColumn}`;
                const response = await fetch(
                    `${path}?query=${encodeURIComponent(query)}&pageSize=${pageSize}&currentPage=${page}`
                );

                if (!response.ok) {
                    const text = await response.text();
                    if (response.status === 400) {
                        setError(text);
                    } else {
                        setError("An unexpected error occurred.");
                    }
                    setRecords([]);
                    setTotal(0);
                    setLoading(false);
                    return;
                }

                const result = (await response.json()) as Response<Media>;
                setRecords(result.data);
                setTotal(result.total);
            } catch (err) {
                setError("Failed to fetch data.");
                setRecords([]);
                setTotal(0);
            } finally {
                setLoading(false);
            }
        };

        loadData();
    }, [path, page, debouncedFilter, sortDirection, sortColumn, pageSize, refreshKey]);

    const totalPages = Math.ceil(total / pageSize);

    /** Generate list of page numbers with ellipsis */
    const getPageNumbers = () => {
        const pages: (number | string)[] = [];

        if (totalPages <= 7) {
            for (let i = 1; i <= totalPages; i++) {
                pages.push(i);
            }
            return pages;
        }

        pages.push(1);

        let start = Math.max(2, page - 2);
        let end = Math.min(totalPages - 1, page + 2);

        if (start > 2) {
            pages.push("...");
        }

        for (let i = start; i <= end; i++) {
            pages.push(i);
        }

        if (end < totalPages - 1) {
            pages.push("...");
        }

        pages.push(totalPages);

        return pages;
    };

    /** Open modal for a selected record */
    const handleRowClick = (record: Media) => {
        setSelected(record);
        setShowModal(true);
    };

    const handleSort = (column: string) => {
        if (sortColumn === column) {
            setSortDirection(sortDirection === "asc" ? "desc" : "asc");
        } else {
            setSortColumn(column);
            setSortDirection("asc");
        }
    };

    function formatMillisecondsToHHMMSS(milliseconds: number) {
        const totalSeconds = Math.floor(milliseconds / 1000);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;

        return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
    }

    return (
        <div>
            <Form.Group className="mb-3" controlId="filterInput">
                <Form.Label>Filter</Form.Label>
                <Form.Control
                    type="text"
                    placeholder="Type to filter media..."
                    value={filter}
                    isInvalid={!!error}
                    onChange={(e) => setFilter(e.target.value)}
                />
                <Form.Control.Feedback type="invalid">
                    {error}
                </Form.Control.Feedback>
            </Form.Group>

            {loading && <Spinner animation="border" size="sm" className="mb-2" />}

            {/* Records table */}
            <Table striped bordered hover responsive>
                <thead>
                    <tr>
                        <th onClick={() => handleSort("id")} style={{ cursor: "pointer" }}>
                            ID {sortColumn === "id" && (sortDirection === "asc" ? <BsSortUp /> : <BsSortDown />)}
                        </th>
                        <th onClick={() => handleSort("title")} style={{ cursor: "pointer" }}>
                            Title {sortColumn === "title" && (sortDirection === "asc" ? <BsSortUp /> : <BsSortDown />)}
                        </th>
                        <th onClick={() => handleSort("artist")} style={{ cursor: "pointer" }}>
                            Artist {sortColumn === "artist" && (sortDirection === "asc" ? <BsSortUp /> : <BsSortDown />)}
                        </th>
                        <th onClick={() => handleSort("album")} style={{ cursor: "pointer" }}>
                            Album {sortColumn === "album" && (sortDirection === "asc" ? <BsSortUp /> : <BsSortDown />)}
                        </th>
                        <th onClick={() => handleSort("rating")} style={{ cursor: "pointer" }}>
                            Rating {sortColumn === "rating" && (sortDirection === "asc" ? <BsSortUp /> : <BsSortDown />)}
                        </th>
                        <th onClick={() => handleSort("length")} style={{ cursor: "pointer" }}>
                            Length {sortColumn === "length" && (sortDirection === "asc" ? <BsSortUp /> : <BsSortDown />)}
                        </th>
                        <th onClick={() => handleSort("type")} style={{ cursor: "pointer" }}>
                            Type {sortColumn === "type" && (sortDirection === "asc" ? <BsSortUp /> : <BsSortDown />)}
                        </th>
                    </tr>
                </thead>
                <tbody>
                    {records.length === 0 ? (
                        <tr>
                            <td colSpan={7} className="text-center">
                                No data
                            </td>
                        </tr>
                    ) : (
                        records.map((record) => (
                            <tr
                                key={record.id}
                                style={{ cursor: "pointer" }}
                                onClick={() => handleRowClick(record)}
                            >
                                <td>{record.id ?? "-"}</td>
                                <td>{record.title ?? "-"}</td>
                                <td>{record.primaryArtist ?? "-"}</td>
                                <td>{record.album ?? "-"}</td>
                                <td>{record.rating ?? "-"}</td>
                                <td>{record.lengthMilliseconds ?? "-"}</td>
                                <td>{record.mimeType ?? "-"}</td>
                            </tr>
                        ))
                    )}
                </tbody>
            </Table>

            {/* Pagination controls */}
            <Pagination className="justify-content-center">
                <Pagination.Prev
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    disabled={page === 1}
                />
                {getPageNumbers().map((p, idx) =>
                    p === "..." ? (
                        <Pagination.Ellipsis key={`ellipsis-${idx}`} disabled />
                    ) : (
                        <Pagination.Item
                            key={`page-${p}`}
                            active={p === page}
                            onClick={() => setPage(p as number)}
                        >
                            {p}
                        </Pagination.Item>
                    )
                )}
                <Pagination.Next
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    disabled={page === totalPages}
                />
            </Pagination>

            {/* Modal */}
            <Modal
                show={showModal}
                onHide={() => setShowModal(false)}
                centered
                size="lg"
            >
                <Modal.Header closeButton>
                    <Modal.Title>Media Details</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    {selected ? (
                        <div>
                            <p><strong>ID:</strong> {selected.id}</p>
                            <p><strong>Title:</strong> {selected.title}</p>
                            <p><strong>Artist:</strong> {selected.primaryArtist}</p>
                            <p><strong>Album:</strong> {selected.album}</p>
                            <p><strong>Rating:</strong> {selected.rating}</p>
                            <p><strong>Length:</strong> {formatMillisecondsToHHMMSS(selected.lengthMilliseconds)}</p>
                            <p><strong>Type:</strong> {selected.mimeType}</p>

                            <div style={{ marginTop: "1rem" }}>
                                <h5>Preview</h5>
                                {selected.mimeType?.startsWith("image/") && (
                                    <img
                                        src={`play/media/${selected.id}`}
                                        alt={selected.title ?? "media"}
                                        style={{ maxWidth: "100%", borderRadius: "8px" }}
                                    />
                                )}
                                {selected.mimeType?.startsWith("audio/") && (
                                    <audio controls src={`play/media/${selected.id}`} style={{ width: "100%" }} />
                                )}
                                {selected.mimeType?.startsWith("video/") && (
                                    <video controls src={`play/media/${selected.id}`} style={{ width: "100%", borderRadius: "8px" }} />
                                )}
                                {selected.mimeType?.startsWith("text/") && (
                                    <iframe
                                        src={`play/media/${selected.id}`}
                                        title="text preview"
                                        style={{ width: "100%", height: "300px", border: "1px solid #ccc", borderRadius: "8px" }}
                                    />
                                )}
                                {!selected.mimeType && <p>No preview available</p>}
                            </div>
                        </div>
                    ) : (
                        <p>No record selected.</p>
                    )}
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" onClick={() => setShowModal(false)}>
                        Close
                    </Button>
                </Modal.Footer>
            </Modal>
        </div>
    );
};

export default MediaView;
