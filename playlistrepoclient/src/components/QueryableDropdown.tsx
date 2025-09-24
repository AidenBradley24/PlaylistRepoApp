import React, { useEffect, useState, useCallback } from "react";
import Dropdown from 'react-bootstrap/Dropdown';
import Form from 'react-bootstrap/Form';
import { useRefresh } from "./RefreshContext";
import { BsPlus } from "react-icons/bs";

import type { Response } from "../models";

export interface QueryableDropdownProps {
    menuLabel: string;
    getPath: string;
    selection: any | undefined;
    setSelection: (data: any | null) => void;
    getLabel: (entry: any) => string;
    onCreateNew: () => any;
}

const QueryableDropdown: React.FC<QueryableDropdownProps> = ({ menuLabel, getPath, setSelection, getLabel, onCreateNew, selection }) => {

    const [entries, setEntries] = useState<any[]>([]);
    const [query, setQuery] = useState<string>("");
    const [error, setError] = useState<string | null>(null);

    const { refreshKey } = useRefresh(); // subscribe to refresh key

    const fetchRecords = useCallback(async (query: string) => {
        try {
            const response = await fetch(`${getPath}?query=${encodeURIComponent(query)}`);
            if (!response.ok) {
                if (response.status === 400) {
                    const message = await response.text();
                    setError(message);
                    setEntries([]);
                    setSelection(null);
                    return;
                }
                throw new Error("fetch failed");
            }
            const result = (await response.json()) as Response<any>;
            setEntries(result.data);
            setError(null);
            if (result.data.length === 0) {
                setSelection(null);
            }
            else if (!selection) {
                setSelection(result.data[0]);
            }
            else {
                let index = result.data.findIndex(item => item.id === selection.id);
                if (index === -1)
                    setSelection(result.data[0]);
                else
                    setSelection(result.data[index]);
            }
        } catch (e) {
            console.error(e);
            setEntries([]);
            setSelection(null);
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
        if (selection === null) return "Select...";
        return getLabel(selection);
    }

    function makeSelection(dropdownKey: string | null) {
        if (dropdownKey === null) return;
        if (dropdownKey === "create") {
            onCreateNew();
            return;
        }
        const index = Number.parseInt(dropdownKey, 10);
        if (!isNaN(index)) {
            setSelection(entries[index]);
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
                    <Dropdown.Item key={i} eventKey={i} active={selection?.id === entry.id}>{getLabel(entry)}</Dropdown.Item>
                ))}
                <Dropdown.Divider />
                <Dropdown.Item eventKey="create"><BsPlus />Create New</Dropdown.Item>
            </Dropdown.Menu>
        </Dropdown>
    );
};

export default QueryableDropdown;
