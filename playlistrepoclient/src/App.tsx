import React, { useState } from "react";
import MediaView from "./View"; // your Media records table component
import "./app.css";

const App: React.FC = () => {
    const [activeTab, setActiveTab] = useState<"media" | "tab2" | "tab3">("media");

    return (
        <div className="app-container">
            {/* Header TODO use get info */}
            <header className="app-header">
                My App
            </header>

            {/* Tabs */}
            <nav className="tab-bar">
                <button
                    className={`tab-button ${activeTab === "media" ? "active" : ""}`}
                    onClick={() => setActiveTab("media")}
                >
                    Media
                </button>
                <button
                    className={`tab-button ${activeTab === "tab2" ? "active" : ""}`}
                    onClick={() => setActiveTab("tab2")}
                >
                    Tab 2
                </button>
                <button
                    className={`tab-button ${activeTab === "tab3" ? "active" : ""}`}
                    onClick={() => setActiveTab("tab3")}
                >
                    Tab 3
                </button>
            </nav>

            {/* Tab Content */}
            <main className="tab-content">
                {activeTab === "media" && <MediaView />}
                {activeTab === "tab2" && (
                    <div className="placeholder">Placeholder content for Tab 2</div>
                )}
                {activeTab === "tab3" && (
                    <div className="placeholder">Placeholder content for Tab 3</div>
                )}
            </main>
        </div>
    );
};

export default App;
