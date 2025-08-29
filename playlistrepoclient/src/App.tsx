import React, { useState, useEffect } from "react";
import { RefreshProvider } from "./RefreshContext";
import { EditProvider } from "./EditContext"
import { TaskProvider } from "./TaskContext"
import Tab from "react-bootstrap/Tab";
import Tabs from "react-bootstrap/Tabs";
import Button from "react-bootstrap/Button";
import { BsMoonFill, BsSunFill } from "react-icons/bs"; // Optional: icons for better UX
import MediaTab from "./MediaTab";
import PlaylistTab from "./PlaylistTab";
import "bootstrap/dist/css/bootstrap.min.css";
import "./app.css";

const App: React.FC = () => {

    const [darkMode, setDarkMode] = useState<boolean>(() => {
        const savedMode = localStorage.getItem("darkMode");
        if (savedMode !== null) {
            return JSON.parse(savedMode);
        }
        return window.matchMedia("(prefers-color-scheme: dark)").matches;
    });

    useEffect(() => {
        document.documentElement.setAttribute(
            "data-bs-theme",
            darkMode ? "dark" : "light"
        );
        localStorage.setItem("darkMode", JSON.stringify(darkMode));
    }, [darkMode]);

    const toggleDarkMode = () => {
        setDarkMode((prev) => !prev);
    };

    return (
        <RefreshProvider>
            <TaskProvider>
                <EditProvider>
                    <div className="app-container">
                        {/* Header */}
                        <header className="app-header d-flex justify-content-between align-items-center">
                            <span>My App</span>
                            <Button
                                variant="outline-secondary"
                                size="sm"
                                onClick={toggleDarkMode}
                                aria-label={darkMode ? "Switch to light mode" : "Switch to dark mode"}
                                className="d-flex align-items-center"
                            >
                                {darkMode ? <BsSunFill className="me-1" /> : <BsMoonFill className="me-1" />}
                                {darkMode ? "Light" : "Dark"}
                            </Button>
                        </header>

                        {/* Tabs */}
                        <Tabs defaultActiveKey="media" id="app-tabs" className="mb-3" fill>
                            <Tab eventKey="media" title="Media">
                                <MediaTab />
                            </Tab>
                            <Tab eventKey="playlists" title="Playlists">
                                <PlaylistTab />
                            </Tab>
                            <Tab eventKey="remotes" title="Remotes">
                                <div className="placeholder">Placeholder content for Tab 3</div>
                            </Tab>
                        </Tabs>
                    </div>
                </EditProvider>
            </TaskProvider>
        </RefreshProvider>
    );
};

export default App;   