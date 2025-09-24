import React, { createContext, useContext, useState, useEffect, useRef } from "react";
import Toast from "react-bootstrap/Toast";
import ToastContainer from "react-bootstrap/ToastContainer";
import ProgressBar from "react-bootstrap/ProgressBar";
import { BsCheckCircle, BsXCircle } from "react-icons/bs";

const TaskContext = createContext<TaskProviderType | undefined>(undefined);

type Task = {
    name: string;
    guid: string | null; // tasks with no guid cannot update
    progress: TaskProgress | null;
    callback?: (taskRecord: Task) => void;
};

type TaskProgress = {
    progress: number;
    status: string;
    isCompleted: boolean;
    isError: boolean;
};

type TaskProviderType = {
    invokeTask: (name: string, taskFunc: Promise<Response>, callback?: (taskRecord: Task) => void) => Promise<void>;
    updateTask: (guid: string) => Promise<void>;
    removeTask: (guid: string) => void;
    removeTaskAt: (index: number) => void;
};

export const TaskProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [tasks, setTasks] = useState<Task[]>([]);
    const tasksRef = useRef<Task[]>([]);
    tasksRef.current = tasks; // always keep ref in sync

    async function invokeTask(name: string, taskFunc: Promise<Response>, callback?: (taskRecord: Task) => void) {
        const response = await taskFunc;

        if (response.status !== 202) {
            console.error("failed to start task: " + name);
            const errorText = (await response.text()) ?? response.statusText;
            const task = { name, guid: null, progress: { progress: -2, status: errorText, isCompleted: false, isError: true } };
            setTasks((prev) => [...prev, task]);
            return;
        }

        const guid = await response.json() as string;
        const task = { name, guid, progress: null, callback: callback } as Task;
        setTasks((prev) => [...prev, task]);
    }

    async function updateTask(guid: string) {
        const response = await fetch(`api/service/status/${guid}`);
        const progress = (await response.json()) as TaskProgress;
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
            await Promise.all(currentTasks.map((task) => { if (task.guid) updateTask(task.guid) }));
        }, 1000);

        return () => clearInterval(interval);
    }, []);

    return (
        <TaskContext.Provider value={{ invokeTask, updateTask, removeTask, removeTaskAt }}>
            {children}
            <ToastContainer position="bottom-end">
                {tasks.map((task, index) => (
                    <Toast key={index} onClose={() => removeTaskAt(index)}>
                        <Toast.Header>{task.name}</Toast.Header>
                        <Toast.Body>
                            <div className="d-flex align-items-center gap-2">
                                {task.progress?.isCompleted && (
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
                        </Toast.Body>
                    </Toast>
                ))}
            </ToastContainer>
        </TaskContext.Provider>
    );
};

export const useTasks = () => useContext(TaskContext)!;
