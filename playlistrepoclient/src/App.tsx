import React, { useState, useEffect } from "react";

import Container from 'react-bootstrap/Container';
import Nav from 'react-bootstrap/Nav';
import Navbar from 'react-bootstrap/Navbar';
import Button from "react-bootstrap/Button";
import { BsMoonFill, BsSunFill, BsBoxSeamFill } from "react-icons/bs";
import HomePage from "./pages/Home";
import MediaTab from "./pages/MediaTab";
import PlaylistTab from "./pages/PlaylistTab";
import RemoteTab from "./pages/RemoteTab";
import "bootstrap/dist/css/bootstrap.min.css";
import "./app.css";

import { Routes, Route, useLocation } from "react-router-dom";

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

    const location = useLocation();

    const routeToKey: Record<string, string> = {
        "/media": "media",
        "/playlists": "playlists",
        "/remotes": "remotes",
    };

    const activeKey = routeToKey[location.pathname] ?? "";

    return (
        <div className="app-container">
            <Navbar expand="lg" className="bg-body-tertiary">
                <Container>
                    <Navbar.Brand href="/"><BsBoxSeamFill /> Playlist Repo</Navbar.Brand>
                    <Navbar.Toggle aria-controls="basic-navbar-nav" />
                    <Navbar.Collapse>
                        <Nav variant="underline" activeKey={activeKey}>
                            <Nav.Item>
                                <Nav.Link href="/media" eventKey="media">Media</Nav.Link>
                            </Nav.Item>
                            <Nav.Item>
                                <Nav.Link href="/playlists" eventKey="playlists">Playlists</Nav.Link>
                            </Nav.Item>
                            <Nav.Item>
                                <Nav.Link href="/remotes" eventKey="remotes">Remote Playlists</Nav.Link>
                            </Nav.Item>
                        </Nav>
                    </Navbar.Collapse>
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
                </Container>
            </Navbar>
            <div id="tab-container">
                <Routes>
                    <Route path="/media" element={<MediaTab />} />
                    <Route path="/playlists" element={<PlaylistTab />} />
                    <Route path="/remotes" element={<RemoteTab />} />
                    <Route path="*" element={<HomePage />} />
                </Routes>
            </div>
        </div>
    );
};

export default App;   