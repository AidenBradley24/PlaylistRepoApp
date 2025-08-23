import React, { useEffect, useState } from "react";
import MediaView from "./View";

const MediaTab: React.FC = () => {

    return (
        <div>
            <h3>Media</h3>
            <MediaView path="data/media" pageSize={20} />
        </div>
    );
};

export default MediaTab;