import React, { createContext, useContext, useState, useEffect, useRef } from "react";
import Toast from "react-bootstrap/Toast";
import ToastContainer from "react-bootstrap/ToastContainer";
import ProgressBar from "react-bootstrap/ProgressBar";
import { BsCheckCircle, BsExclamationTriangle, BsXCircle } from "react-icons/bs";

const TaskContext = createContext<TaskProviderType | undefined>(undefined);

type Task = {
    name: string;
    guid: string | null; // tasks with no guid cannot update
    progress: TaskProgress | null;
    callback?: (taskRecord: Task) => void;
    warning?: string;
};

type TaskProgress = {
    progress: number;
    status: string;
    isCompleted: boolean;
    isError: boolean;
};

type TaskProviderType = {
    invokeTask: (name: string, taskFunc: Promise<Response>, callback?: (taskRecord: Task) => void) => Promise<void>;
    showWarning: (name: string, message: string) => void;
    updateTask: (guid: string) => Promise<void>;
    removeTask: (guid: string) => void;
    removeTaskAt: (index: number) => void;
};

async function responseError(response: Response): Promise<string> {
    const text = await response.text();
    try {
        const problem = JSON.parse(text) as { title?: string; detail?: string; errors?: Record<string, string[]> };
        const errors = Object.entries(problem.errors ?? {})
            .flatMap(([field, messages]) => messages.map((message) => field ? `${field}: ${message}` : message));
        const detail = errors.join("; ") || problem.detail || problem.title;
        return `HTTP ${response.status}: ${detail || response.statusText || "Request failed"}`;
    } catch {
        return `HTTP ${response.status}: ${text || response.statusText || "Request failed"}`;
    }

}

function errorMessage(error: unknown): string {
    return error instanceof Error ? error.message : "An unexpected error occurred.";
}

export const TaskProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [tasks, setTasks] = useState<Task[]>([]);
    const tasksRef = useRef<Task[]>([]);
    tasksRef.current = tasks; // always keep ref in sync

    function showWarning(name: string, message: string) {
        setTasks((prev) => [...prev, { name, guid: null, progress: null, warning: message }]);
    }

    async function invokeTask(name: string, taskFunc: Promise<Response>, callback?: (taskRecord: Task) => void) {
        try {
            const response = await taskFunc;
            if (response.status !== 202) throw new Error(await responseError(response));
            const guid = await response.json() as string;
            const task = { name, guid, progress: null, callback: callback } as Task;
            setTasks((prev) => [...prev, task]);
        } catch (error) {
            const task = { name, guid: null, progress: { progress: -2, status: `Could not start task: ${errorMessage(error)}`, isCompleted: true, isError: true } };
            setTasks((prev) => [...prev, task]);
        }
    }

    async function updateTask(guid: string) {
        let progress: TaskProgress;
        try {
            const response = await fetch(`api/service/status/${guid}`);
            if (!response.ok) throw new Error(await responseError(response));
            progress = (await response.json()) as TaskProgress;
        } catch (error) {
            progress = { progress: -2, status: `Could not check task status: ${errorMessage(error)}`, isCompleted: true, isError: true };
        }
        setTasks((oldTasks) => {
            const index = oldTasks.findIndex((t) => t.guid === guid);
            if (index === -1) return oldTasks;
            if (oldTasks[index].progress?.isCompleted) return oldTasks;
            const newTasks = [...oldTasks];
            newTasks[index] = { ...newTasks[index], progress };
            if (progress.isCompleted && newTasks[index].callback) newTasks[index].callback(newTasks[index]);
            return newTasks;
        });
    }

    function removeTask(guid: string) {
        setTasks((prev) => prev.filter((t) => t.guid !== guid));
    }

    function removeTaskAt(index: number) {
        setTasks((prev) => prev.filter((_, i) => i !== index));
    }

    useEffect(() => {
        const interval = setInterval(async () => {
            const currentTasks = tasksRef.current;
            await Promise.all(currentTasks.map((task) => task.guid && !task.progress?.isCompleted ? updateTask(task.guid) : undefined));
        }, 1000);

        return () => clearInterval(interval);
    }, []);

    return (
        <TaskContext.Provider value={{ invokeTask, showWarning, updateTask, removeTask, removeTaskAt }}>
            {children}
            <ToastContainer position="bottom-end">
                {tasks.map((task, index) => (
                    <Toast key={index} onClose={() => removeTaskAt(index)}>
                        <Toast.Header>{task.name}</Toast.Header>
                        <Toast.Body>
                            {task.warning ? (
                                <div className="d-flex align-items-start gap-2">
                                    <BsExclamationTriangle className="text-warning flex-shrink-0" size={20} />
                                    <p className="mb-0">{task.warning}</p>
                                </div>
                            ) : (
                            <>
                            <div className="d-flex align-items-center gap-2">
                                {task.progress?.isCompleted && !task.progress.isError && (
                                    <BsCheckCircle className="text-success flex-shrink-0" size={20} />
                                )}
                                {task.progress?.isError && (
                                    <BsXCircle className="text-danger flex-shrink-0" size={20} />
                                )}
                                <ProgressBar
                                    className="flex-grow-1"
                                    animated={
                                        task.progress != null &&
                                        task.progress.progress >= 0 &&
                                        task.progress.progress < 100
                                    }
                                    now={
                                        task.progress?.isError
                                            ? 100 // make the bar "full" in red for error
                                            : task.progress?.progress ?? 0
                                    }
                                    variant={
                                        task.progress?.isError
                                            ? "danger"
                                            : task.progress?.progress === 100
                                                ? "success"
                                                : "primary"
                                    }
                                />
                            </div>
                            <p className="mb-0">{task.progress?.status}</p>
                            </>
                            )}
                        </Toast.Body>
                    </Toast>
                ))}
            </ToastContainer>
        </TaskContext.Provider>
    );
};

export const useTasks = () => useContext(TaskContext)!;
