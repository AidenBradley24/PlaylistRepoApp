import React, { useState, useCallback, useRef, useEffect } from "react";
import { useTasks } from "./TaskContext";
import "./FileUpload.css";

export function useFileUpload(uploadUrl: string) {
    const { invokeTask } = useTasks();

    async function uploadFiles(files: File[], callback?: (taskRecord: any) => void) {
        const formData = new FormData();
        files.forEach((file) => formData.append("files", file));

        // call invokeTask with the POST promise
        await invokeTask(
            `Uploading ${files.length} file(s)`,
            fetch(uploadUrl, { method: "POST", body: formData }),
            callback
        );
    }

    return { uploadFiles };
}

export function useOpenFileDialog(uploadUrl: string) {
    const { uploadFiles } = useFileUpload(uploadUrl);

    function openFileDialog(callback?: (taskRecord: any) => void) {
        const input = document.createElement("input");
        input.type = "file";
        input.multiple = true;

        input.onchange = () => {
            if (input.files) {
                uploadFiles(Array.from(input.files), callback);
            }
        };

        input.click();
    }

    return { openFileDialog };
}

interface DragAndDropUploaderProps {
    uploadUrl: string;
}

export const DragAndDropUploader: React.FC<DragAndDropUploaderProps> = ({ uploadUrl }) => {
    const [isDragging, setIsDragging] = useState(false);
    const dragCounter = useRef(0);
    const { uploadFiles } = useFileUpload(uploadUrl);

    const isFileDrag = (event: DragEvent) =>
        Array.from(event.dataTransfer?.types ?? []).includes("Files");

    const handleDrop = useCallback(
        async (event: DragEvent) => {
            event.preventDefault();
            event.stopPropagation();
            dragCounter.current = 0;
            setIsDragging(false);

            const files = event.dataTransfer?.files
                ? Array.from(event.dataTransfer.files)
                : [];
            if (files.length === 0) return;

            // One upload operation = one task
            uploadFiles(files);
        },
        [uploadFiles]
    );

    useEffect(() => {
        const handleDragEnter = (event: DragEvent) => {
            if (!isFileDrag(event)) return;
            event.preventDefault();
            event.stopPropagation();
            dragCounter.current += 1;
            setIsDragging(true);
        };

        const handleDragLeave = (event: DragEvent) => {
            if (!isFileDrag(event)) return;
            event.preventDefault();
            event.stopPropagation();
            dragCounter.current -= 1;
            if (dragCounter.current === 0) {
                setIsDragging(false);
            }
        };

        const handleDragOver = (event: DragEvent) => {
            if (!isFileDrag(event)) return;
            event.preventDefault();
            event.stopPropagation();
        };

        const handleWindowDrop = (event: DragEvent) => {
            if (!isFileDrag(event)) return;
            handleDrop(event);
        };

        window.addEventListener("dragenter", handleDragEnter);
        window.addEventListener("dragleave", handleDragLeave);
        window.addEventListener("dragover", handleDragOver);
        window.addEventListener("drop", handleWindowDrop);

        return () => {
            window.removeEventListener("dragenter", handleDragEnter);
            window.removeEventListener("dragleave", handleDragLeave);
            window.removeEventListener("dragover", handleDragOver);
            window.removeEventListener("drop", handleWindowDrop);
        };
    }, [handleDrop]);

    return (
        <>
            {isDragging && (
                <div className="file-upload-overlay">
                    <div className="file-upload-box">
                        <p>Drop files to upload</p>
                    </div>
                </div>
            )}
        </>
    );
};
