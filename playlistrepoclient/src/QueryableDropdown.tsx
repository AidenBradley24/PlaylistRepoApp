import React, { useEffect, useState, useCallback } from "react";
import Dropdown from 'react-bootstrap/Dropdown';
import Form from 'react-bootstrap/Form';

import type { Response } from "./models";

export interface QueryableDropdownProps {
    menuLabel: string;
    getPath: string;
    onSelection: (data: any | null) => void;
    getLabel: (entry: any) => string;
    onCreateNew: () => any;
}

const QueryableDropdown: React.FC<QueryableDropdownProps> = ({ menuLabel, getPath, onSelection, getLabel, onCreateNew }) => {

    const NOT_AVAILABLE = -2;
    const UNSELECTED = -1;

    const [entries, setEntries] = useState<any[]>([]);
    const [selectedIndex, setSelectedIndex] = useState<number>(NOT_AVAILABLE);
    const [query, setQuery] = useState<string>("");
    const [error, setError] = useState<string | null>(null);

    const fetchRecords = useCallback(async (query: string) => {
        try {
            const response = await fetch(`${getPath}?query=${encodeURIComponent(query)}`);
            if (!response.ok) {
                if (response.status === 400) {
                    const message = await response.text();
                    setError(message);
                    setEntries([]);
                    setSelectedIndex(UNSELECTED);
                    return;
                }
                throw new Error("fetch failed");
            }
            const result = (await response.json()) as Response<any>;
            setEntries(result.data);
            setError(null);
            if (result.data.length === 0) {
                setSelectedIndex(UNSELECTED);
            } else {
                setSelectedIndex(0);
            }
        } catch (e) {
            console.error(e);
            setEntries([]);
            setSelectedIndex(UNSELECTED);
            setError("An unexpected error occurred");
        }
    }, [getPath]);

    // Debounce query updates
    useEffect(() => {
        const handler = setTimeout(() => {
            fetchRecords(query);
        }, 300);
        return () => clearTimeout(handler);
    }, [query, fetchRecords]);

    useEffect(() => {
        if (selectedIndex >= 0 && entries[selectedIndex]) {
            onSelection(entries[selectedIndex]);
        } else {
            onSelection(null);
        }
    }, [selectedIndex, entries, onSelection]);

    function getSelectedTitle(): string {
        if (selectedIndex === NOT_AVAILABLE) return "Loading...";
        if (selectedIndex === UNSELECTED) return "Select...";
        return getLabel(entries[selectedIndex]);
    }

    function makeSelection(dropdownKey: string | null) {
        if (dropdownKey === null) return;
        if (dropdownKey === "create") {
            onCreateNew();
            return;
        }
        const index = Number.parseInt(dropdownKey, 10);
        if (!isNaN(index)) {
            setSelectedIndex(index);
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
                    <Dropdown.Item key={i} eventKey={i} active={selectedIndex==i}>{getLabel(entry)}</Dropdown.Item>
                ))}
                <Dropdown.Divider />
                <Dropdown.Item eventKey="create">+ Create New</Dropdown.Item>
            </Dropdown.Menu>
        </Dropdown>
    );
};

export default QueryableDropdown;
