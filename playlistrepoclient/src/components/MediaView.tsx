import React, { useEffect, useState, useRef } from "react";
import { InputGroup, Table, Form, Button, Pagination, Spinner } from "react-bootstrap";
import type { Response, Media } from "../models";
import { useRefresh } from "./RefreshContext";
import { useEdits } from "./EditContext";
import { BsSortDown, BsSortUp, BsXLg, BsFillLockFill, BsFillUnlockFill } from "react-icons/bs";
import { MdFileDownloadDone, MdFileDownloadOff } from "react-icons/md";
import { formatMillisecondsToHHMMSS } from "../utils";

import "./records.css";

export interface MediaViewProps {
    query: string;
    setQuery: (query: string) => void;
    path: string;
    pageSize?: number;
}

const MediaView: React.FC<MediaViewProps> = ({ query, setQuery, path, pageSize = 20 }) => {
    const [records, setRecords] = useState<Media[]>([]);
    const [page, setPage] = useState(1);
    const [total, setTotal] = useState(0);

    const [sortColumn, setSortColumn] = useState<string | null>();
    const [sortDirection, setSortDirection] = useState<"asc" | "desc" | null>(null);

    const [debouncedQuery, setDebouncedQuery] = useState<string>('');

    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState<boolean>(false);

    const { refreshKey } = useRefresh();
    const { setViewingMediaId, setShowMediaModal } = useEdits();

    useEffect(() => {
        const timeout = records.length === 0 ? 0 : 500;
        const handler = setTimeout(() => {
            setDebouncedQuery(query);
            setPage(1);
        }, timeout);

        return () => {
            clearTimeout(handler);
        };
    }, [query]);

    useEffect(() => {
        // Look for patterns like "orderby title" or "orderbydescending rating"
        const match = query.match(/\borderby(descending)?\s+(\w+)/i);
        if (match) {
            const [, desc, column] = match;
            setSortColumn(column);
            setSortDirection(desc ? "desc" : "asc");
        } else {
            // If no orderby in query, clear sorting
            setSortColumn(null);
            setSortDirection(null);
        }
    }, [query]);

    useEffect(() => {
        if (!sortColumn || !sortDirection) return;

        // Remove any existing orderby clause from query
        const withoutOrder = query.replace(/\borderby(descending)?\s+\w+/i, "").trim();

        // Append the current sort
        const newQuery = `${withoutOrder} orderby${sortDirection === "desc" ? "descending" : ""} ${sortColumn}`.trim();

        if (newQuery !== query) {
            setQuery(newQuery);
        }
    }, [sortColumn, sortDirection]);


    /** Fetch records when query params change */
    useEffect(() => {
        const loadData = async () => {
            setLoading(true);
            setError(null);

            try {
                const response = await fetch(
                    `${path}?query=${encodeURIComponent(debouncedQuery)}&pageSize=${pageSize}&currentPage=${page}`
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
    }, [path, page, debouncedQuery, sortDirection, sortColumn, pageSize, refreshKey]);

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
        setViewingMediaId(record.id);
        setShowMediaModal(true);
    };

    const handleSort = (column: string) => {
        if (sortColumn === column) {
            setSortDirection(sortDirection === "asc" ? "desc" : "asc");
        } else {
            setSortColumn(column);
            setSortDirection("asc");
        }
    };

    const inputRef = useRef<HTMLInputElement | null>(null);

    const handleReset = () => {
        setQuery('');
        // Focus the input again after clearing
        if (inputRef.current) {
            inputRef.current.focus();
        }
    };

    return (
        <div>
            <InputGroup className="mb-3">
                {query && (
                    <Button
                        variant="outline-secondary"
                        onClick={handleReset}
                        className="d-flex align-items-center"
                    >
                        <BsXLg />
                    </Button>
                )}
                <Form.Control
                    type="text"
                    placeholder="Type to filter media..."
                    value={query}
                    isInvalid={!!error}
                    onChange={(e) => setQuery(e.target.value)}
                    ref={inputRef}
                />
                <Form.Control.Feedback type="invalid">
                    {error}
                </Form.Control.Feedback>
            </InputGroup>

            {loading && <Spinner animation="border" size="sm" className="mb-2" />}

            {/* Records table */}
            <Table striped bordered hover responsive>
                <thead>
                    <tr>
                        <th onClick={() => handleSort("id")} style={{ cursor: "pointer" }}>
                            ID {sortColumn === "id" && (sortDirection === "asc" ? <BsSortUp /> : <BsSortDown />)}
                        </th>
                        <th/>
                        <th/>
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
                        <th onClick={() => handleSort("genre")} style={{ cursor: "pointer" }}>
                            Genre {sortColumn === "genre" && (sortDirection === "asc" ? <BsSortUp /> : <BsSortDown />)}
                        </th>
                    </tr>
                </thead>
                <tbody>
                    {records.length === 0 ? (
                        <tr>
                            <td colSpan={10} className="text-center">
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
                                <td>{record.isOnFile ? (<MdFileDownloadDone />) : (<MdFileDownloadOff />)}</td>
                                <td>{record.locked ? (<BsFillLockFill />) : (<BsFillUnlockFill />)}</td>
                                <td>{record.title ?? "-"}</td>
                                <td>{record.primaryArtist ?? "-"}</td>
                                <td>{record.album ?? "-"}</td>
                                <td>{record.rating === 0 ? "-" : record.rating} / 10</td>
                                <td>{record.lengthMilliseconds === 0 ? "-" : formatMillisecondsToHHMMSS(record.lengthMilliseconds)}</td>
                                <td>{record.mimeType ?? "-"}</td>
                                <td>{record.genre ?? "-"}</td>
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
        </div>
    );
};

export default MediaView;
