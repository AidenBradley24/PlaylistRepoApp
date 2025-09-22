import React, { useState, useEffect } from "react";

import Tab from "react-bootstrap/Tab";
import Tabs from "react-bootstrap/Tabs";
import Button from "react-bootstrap/Button";
import { BsMoonFill, BsSunFill } from "react-icons/bs"; // Optional: icons for better UX
import MediaTab from "./pages/MediaTab";
import PlaylistTab from "./pages/PlaylistTab";
import RemoteTab from "./pages/RemoteTab";
import "bootstrap/dist/css/bootstrap.min.css";
import "./app.css";

import { Routes, Route, useNavigate, useLocation } from "react-router-dom";

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

    const navigate = useNavigate();
    const location = useLocation();

    // Map routes to tab keys
    const routeToKey: Record<string, string> = {
        "/media": "media",
        "/playlists": "playlists",
        "/remotes": "remotes",
    };

    const keyToRoute: Record<string, string> = {
        media: "/media",
        playlists: "/playlists",
        remotes: "/remotes",
    };

    const activeKey = routeToKey[location.pathname] ?? "media";

    return (
            <div className="app-container">
                <header className="app-header d-flex justify-content-between align-items-center">
                    <span>Playlist Repository GUI</span>
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
            <div id="tab-container">
                <Tabs
                    id="app-tabs"
                    className="mb-3"
                    fill
                    activeKey={activeKey}
                    onSelect={(k) => {
                        if (k) navigate(keyToRoute[k]);
                    }}
                >
                    <Tab eventKey="media" title="Media" />
                    <Tab eventKey="playlists" title="Playlists" />
                    <Tab eventKey="remotes" title="Remotes" />
                </Tabs>

                <Routes>
                    <Route path="/media" element={<MediaTab />} />
                    <Route path="/playlists" element={<PlaylistTab />} />
                    <Route path="/remotes" element={<RemoteTab />} />
                    <Route path="*" element={<MediaTab />} /> {/* fallback */}
                </Routes>
            </div>
            </div>
    );
};

export default App;   