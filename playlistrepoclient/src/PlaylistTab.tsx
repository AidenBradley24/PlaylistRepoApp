import React, { useEffect, useState } from "react";
import MediaView from "./View";

const PlaylistTab: React.FC = () => {

    return (
        <div>
            <h3>Playlist Name Here</h3>
            <MediaView path={`data/playlists/1/media`} />
        </div>
    );
};

export default PlaylistTab;