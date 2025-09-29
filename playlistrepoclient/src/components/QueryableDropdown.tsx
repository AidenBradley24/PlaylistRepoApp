import React, { useEffect, useState, useCallback } from "react";
import Dropdown from 'react-bootstrap/Dropdown';
import Form from 'react-bootstrap/Form';
import { useRefresh } from "./RefreshContext";
import { BsPlus } from "react-icons/bs";

import type { Response } from "../models";

export interface QueryableDropdownProps {
    menuLabel: string;
    getPath: string;
    selection: number;
    setSelection: (id: number) => void;
    getLabel: (entry: any) => string;
    onCreateNew: () => any;
}

const QueryableDropdown: React.FC<QueryableDropdownProps> = ({ menuLabel, getPath, setSelection, getLabel, onCreateNew, selection }) => {

    const [entries, setEntries] = useState<any[]>([]);
    const [query, setQuery] = useState<string>("");
    const [error, setError] = useState<string | null>(null);

    const { refreshKey } = useRefresh();

    const fetchRecords = useCallback(async (query: string) => {
        try {
            const response = await fetch(`${getPath}?query=${encodeURIComponent(query)}`);
            if (!response.ok) {
                if (response.status === 400) {
                    const message = await response.text();
                    setError(message);
                    setEntries([]);
                    setSelection(0);
                    return;
                }
                throw new Error("fetch failed");
            }
            const result = (await response.json()) as Response<any>;
            setEntries(result.data);
            setError(null);
            if (result.data.length === 0) {
                setSelection(0);
            }
            else if (!selection) {
                console.log("no seleection");
                setSelection(result.data[0].id);
            }
            else {
                const index = result.data.findIndex(item => item.id === selection);
                if (index === -1)
                    setSelection(result.data[0].id);
                else
                    setSelection(result.data[index].id);
            }
        } catch (e) {
            console.error(e);
            setEntries([]);
            setSelection(0);
            setError("An unexpected error occurred");
        }
    }, [getPath, refreshKey]);

    // Debounce query updates
    useEffect(() => {
        const handler = setTimeout(() => {
            fetchRecords(query);
        }, 300);
        return () => clearTimeout(handler);
    }, [query, fetchRecords]);

    function getSelectedTitle(): string {
        if (selection === 0) return "Select...";
        const index = entries.findIndex(item => item.id === selection);
        if (index < 0) {
            return "";
        }
        return getLabel(entries[index]);
    }

    function makeSelection(dropdownKey: string | null) {
        if (dropdownKey === null) return;
        if (dropdownKey === "create") {
            onCreateNew();
            return;
        }
        const index = Number.parseInt(dropdownKey, 10);
        if (!isNaN(index)) {
            setSelection(entries[index].id);
        }
    }

    interface CustomMenuProps {
        children: React.ReactNode;
        style: React.CSSProperties;
        className: string;
        'aria-labelledby': string;
    }

    const CustomMenu = React.forwardRef<HTMLDivElement, CustomMenuProps>(
        ({ children, style, className, 'aria-labelledby': labeledBy }, ref) => {
            return (
                <div
                    ref={ref}
                    style={style}
                    className={className}
                    aria-labelledby={labeledBy}
                >
                    <Form.Control
                        autoFocus
                        className={`mx-3 my-2 w-auto ${error ? 'is-invalid' : ''}`}
                        placeholder="Type to filter..."
                        onChange={(e) => setQuery(e.target.value)}
                        value={query}
                    />
                    {error && <div className="invalid-feedback d-block mx-3">{error}</div>}
                    <ul className="list-unstyled">
                        {children}
                    </ul>
                </div>
            );
        }
    );
    CustomMenu.displayName = "CustomMenu";

    return (
        <Dropdown onSelect={(key) => makeSelection(key)}>
            <Dropdown.Toggle variant="primary" id="dropdown-basic">
                {getSelectedTitle()}
            </Dropdown.Toggle>

            <Dropdown.Menu as={CustomMenu}>
                <Dropdown.ItemText key="label">{menuLabel}</Dropdown.ItemText>
                {entries.map((entry, i) => (
                    <Dropdown.Item key={i} eventKey={i} active={selection === entry.id}>{getLabel(entry)}</Dropdown.Item>
                ))}
                <Dropdown.Divider />
                <Dropdown.Item eventKey="create"><BsPlus />Create New</Dropdown.Item>
            </Dropdown.Menu>
        </Dropdown>
    );
};

export default QueryableDropdown;
