import React, { useEffect, useState } from "react";
import { Table, Button, Pagination } from "react-bootstrap";
import type { Response, Media } from "./models";
import Modal from "react-bootstrap/Modal";

const PageSize = 20;

async function fetchRecords(query: string, page: number) {
    return await fetch(`data/media?query=${encodeURI(query)}&pageSize=${PageSize}&currentPage=${page}`);
}

function getUserQuery(filter: string, sortDirection: string, sortColumn: string) {
    return `${filter} orderby${sortDirection == "desc" ? "descending" : ""} ${sortColumn}`
}

const MediaView: React.FC = () => {
    const [records, setRecords] = useState<Media[]>([]);
    const [page, setPage] = useState(1);
    const [total, setTotal] = useState(0);

    const [selected, setSelected] = useState<Media | null>(null);
    const [showModal, setShowModal] = useState(false);

    const [sortColumn, setSortColumn] = useState<string>('id');
    const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc");
    const [filter, setFilter] = useState<string>('');

    useEffect(() => {
        const loadData = async () => {
            const response = await fetchRecords(getUserQuery(filter, sortDirection, sortColumn), page);

            if (!response.ok) {
                throw new Error(`Response status: ${response.status}`);
            }

            const result = (await response.json()) as Response<Media>;
            setRecords(result.data);
            setTotal(result.total);
        };
        loadData();
    }, [page, filter, sortDirection, sortColumn]);

    const totalPages = Math.ceil(total / PageSize);

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
            // toggle direction
            setSortDirection(sortDirection === "asc" ? "desc" : "asc");
        } else {
            // new column
            setSortColumn(column);
            setSortDirection("asc");
        }
    };

    return (
        <div>
            <h3>Records</h3>

            {/* Records table */}
            <Table striped bordered hover responsive>
                <thead>
                    <tr>
                        <th onClick={() => handleSort("id")} style={{ cursor: "pointer" }}>
                            ID {sortColumn === "id" && (sortDirection === "asc" ? "▲" : "▼")}
                        </th>
                        <th onClick={() => handleSort("title")} style={{ cursor: "pointer" }}>
                            Title {sortColumn === "title" && (sortDirection === "asc" ? "▲" : "▼")}
                        </th>
                        <th onClick={() => handleSort("artist")} style={{ cursor: "pointer" }}>
                            Artist {sortColumn === "artist" && (sortDirection === "asc" ? "▲" : "▼")}
                        </th>
                        <th onClick={() => handleSort("album")} style={{ cursor: "pointer" }}>
                            Album {sortColumn === "album" && (sortDirection === "asc" ? "▲" : "▼")}
                        </th>
                        <th onClick={() => handleSort("rating")} style={{ cursor: "pointer" }}>
                            Rating {sortColumn === "rating" && (sortDirection === "asc" ? "▲" : "▼")}
                        </th>
                        <th onClick={() => handleSort("length")} style={{ cursor: "pointer" }}>
                            Length {sortColumn === "length" && (sortDirection === "asc" ? "▲" : "▼")}
                        </th>
                    </tr>
                </thead>
                <tbody>
                    {records.length === 0 ? (
                        <tr>
                            <td colSpan={6} className="text-center">
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
                                <td>{record.mediaLength?.toLocaleString() ?? "-"}</td>
                            </tr>
                        ))
                    )}
                </tbody>
            </Table>

            {/* Pagination controls */}
            <Pagination className="justify-content-center">
                {/* Back button */}
                <Pagination.Prev
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    disabled={page === 1}
                />

                {/* Page numbers */}
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

                {/* Next button */}
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
                            <p><strong>Length:</strong> {selected.mediaLength?.toLocaleString()}</p>

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
                                    <audio
                                        controls
                                        src={`play/media/${selected.id}`}
                                        style={{ width: "100%" }}
                                    />
                                )}

                                {selected.mimeType?.startsWith("video/") && (
                                    <video
                                        controls
                                        src={`play/media/${selected.id}`}
                                        style={{ width: "100%", borderRadius: "8px" }}
                                    />
                                )}

                                {selected.mimeType?.startsWith("text/") && (
                                    <iframe
                                        src={`play/media/${selected.id}`}
                                        title="text preview"
                                        style={{
                                            width: "100%",
                                            height: "300px",
                                            border: "1px solid #ccc",
                                            borderRadius: "8px",
                                        }}
                                    />
                                )}

                                {!selected.mimeType && (
                                    <p>No preview available</p>
                                )}
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
