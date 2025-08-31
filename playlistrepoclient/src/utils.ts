export function formatMillisecondsToHHMMSS(milliseconds: number) {
    const totalSeconds = Math.floor(milliseconds / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

export async function download(link: string, suggestedFileName?: string) {
    const response = await fetch(link);
    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);

    // Try to parse filename from Content-Disposition
    let fileName = suggestedFileName;
    if (!fileName) {
        const disposition = response.headers.get("Content-Disposition");
        if (disposition && disposition.includes("filename=")) {
            // Regex handles filename="..." or filename=...
            const match = disposition.match(/filename\*?=(?:UTF-8''|")?([^\";]+)/i);
            if (match && match[1]) {
                fileName = decodeURIComponent(match[1]);
            }
        }
    }

    // fallback if still no filename
    if (!fileName) {
        fileName = "download";
    }

    const linkElement = document.createElement("a");
    linkElement.href = url;
    linkElement.setAttribute("download", fileName);
    document.body.appendChild(linkElement);
    linkElement.click();
    linkElement.parentNode?.removeChild(linkElement);
}
