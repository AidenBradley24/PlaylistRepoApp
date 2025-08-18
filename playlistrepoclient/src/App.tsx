import React from "react";
import Tab from "react-bootstrap/Tab";
import Tabs from "react-bootstrap/Tabs";
import MediaView from "./View"; // your Media records table component
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
                    <MediaView />
                </Tab>
                <Tab eventKey="tab2" title="Tab 2">
                    <div className="placeholder">Placeholder content for Tab 2</div>
                </Tab>
                <Tab eventKey="tab3" title="Tab 3">
                    <div className="placeholder">Placeholder content for Tab 3</div>
                </Tab>
            </Tabs>
        </div>
    );
};

export default App;
