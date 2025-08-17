import React, { useEffect, useState } from "react";
import type { Response, Media } from "./models";
import "./records.css";

const PageSize = 10;

async function fetchRecords(page: number) {
    return await fetch(`data/media?pageSize=${PageSize}&currentPage=${page}`);
}

const MediaView: React.FC = () => {
    const [records, setRecords] = useState<Media[]>([]);
    const [page, setPage] = useState(1);
    const [total, setTotal] = useState(0);

    useEffect(() => {
        const loadData = async () => {
            const response = await fetchRecords(page);

            if (!response.ok) {
                throw new Error(`Response status: ${response.status}`);
            }

            const result = (await response.json()) as Response<Media>;
            console.log(result);
            setRecords(result.data);
            setTotal(result.total);
        };
        loadData();
    }, [page]);

    const totalPages = Math.ceil(total / PageSize);

    return (
        <div className="records-container">
            <h3 className="records-title">Records</h3>

            {/* Records table */}
            <table className="records-table">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Title</th>
                        <th>Description</th>
                    </tr>
                </thead>
                <tbody>
                    {records.length === 0 ? (
                        <tr key="no">
                            <td colSpan={3} className="no-data">
                                No data
                            </td>
                        </tr>
                    ) : (
                        records.map((record) => (
                            <tr key={record.id}>
                                <td>{record.id ?? "—"}</td>
                                <td>{record.title ?? "—"}</td>
                                <td>{record.description ?? "—"}</td>
                            </tr>
                        ))
                    )}
                </tbody>
            </table>

            {/* Pagination controls */}
            <div className="pagination">
                {Array.from({ length: totalPages }, (_, i) => (
                    <button
                        key={i}
                        className={`page-button ${i + 1 === page ? "active" : ""}`}
                        onClick={() => setPage(i + 1)}
                    >
                        {i + 1}
                    </button>
                ))}
            </div>
        </div>
    );
};

export default MediaView;
