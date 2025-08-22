import React from "react";
import Tab from "react-bootstrap/Tab";
import Tabs from "react-bootstrap/Tabs";
import MediaTab from "./MediaTab";
import PlaylistTab from "./PlaylistTab";
import "bootstrap/dist/css/bootstrap.min.css"; // include bootstrap styles
import "./app.css";

const App: React.FC = () => {
    return (
        <div className="app-container">
            {/* Header */}
            <header className="app-header">My App</header>

            {/* Tabs from react-bootstrap */}
            <Tabs
                defaultActiveKey="media"
                id="app-tabs"
                className="mb-3"
                fill
            >
                <Tab eventKey="media" title="Media">
                    <MediaTab></MediaTab>
                </Tab>
                <Tab eventKey="playlists" title="Playlists">
                    <PlaylistTab></PlaylistTab>
                </Tab>
                <Tab eventKey="remotes" title="Remotes">
                    <div className="placeholder">Placeholder content for Tab 3</div>
                </Tab>
            </Tabs>
        </div>
    );
};

export default App;
