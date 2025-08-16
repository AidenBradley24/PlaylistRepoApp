import { useEffect, useState } from 'react';
import './App.css';

interface Media {
    id: number
    title: string
}

function App() {
    const [medias, setMedias] = useState<Media[]>();

    useEffect(() => {
        populateData();
    }, []);

    const contents = medias === undefined
        ? <p><em>Loading... Please refresh once the ASP.NET backend has started. See <a href="https://aka.ms/jspsintegrationreact">https://aka.ms/jspsintegrationreact</a> for more details.</em></p>
        : <table className="table table-striped" aria-labelledby="tableLabel">
            <thead>
                <tr>
                    <th>Title</th>
                </tr>
            </thead>
            <tbody>
                {medias.map(media =>
                    <tr key={media.id}>
                        <td>{media.title}</td>
                    </tr>
                )}
            </tbody>
        </table>;

    return (
        <div>
            <h1 id="tableLabel">Weather forecast</h1>
            <p>This component demonstrates fetching data from the server.</p>
            {contents}
        </div>
    );

    async function populateData() {
        const response = await fetch('/data/media');
        if (response.ok) {
            const data = await response.json();
            setMedias(data);
        }
    }
}

export default App;