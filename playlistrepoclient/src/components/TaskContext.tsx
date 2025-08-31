import React, { createContext, useContext, useState, useEffect, useRef } from "react";
import Toast from "react-bootstrap/Toast";
import ToastContainer from "react-bootstrap/ToastContainer";
import ProgressBar from "react-bootstrap/ProgressBar";
import { BsCheckCircle, BsXCircle } from "react-icons/bs";

const TaskContext = createContext<TaskProviderType | undefined>(undefined);

type Task = {
    name: string;
    guid: string;
    progress: TaskProgress | null;
    isComplete: boolean;
    callback?: (taskRecord: Task) => void; 
};

type TaskProgress = {
    progress: number;
    status: string;
};

type TaskProviderType = {
    invokeTask: (name: string, taskFunc: Promise<Response>, callback?: (taskRecord: Task) => void) => Promise<void>;
    updateTask: (guid: string) => Promise<void>;
    removeTask: (guid: string) => void;
};

export const TaskProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [tasks, setTasks] = useState<Task[]>([]);
    const tasksRef = useRef<Task[]>([]);
    tasksRef.current = tasks; // always keep ref in sync

    async function invokeTask(name: string, taskFunc: Promise<Response>, callback?: (taskRecord: Task) => void) {
        const response = await taskFunc;
        const guid = await response.json() as string;
        const task = { name, guid, progress: null, isComplete: false, callback: callback } as Task;
        setTasks((prev) => [...prev, task]);
    }

    async function updateTask(guid: string) {
        const response = await fetch(`api/service/status/${guid}`);
        const progress = (await response.json()) as TaskProgress;
        setTasks((oldTasks) => {
            const index = oldTasks.findIndex((t) => t.guid === guid);
            if (index === -1) return oldTasks;
            if (oldTasks[index].isComplete) return oldTasks;
            const newTasks = [...oldTasks];
            const isComplete = progress.progress === 100;
            newTasks[index] = { ...newTasks[index], progress, isComplete };
            if (isComplete && newTasks[index].callback) newTasks[index].callback(newTasks[index]);
            return newTasks;
        });
    }

    function removeTask(guid: string) {
        setTasks((prev) => prev.filter((t) => t.guid !== guid));
    }

    useEffect(() => {
        const interval = setInterval(async () => {
            const currentTasks = tasksRef.current;
            await Promise.all(currentTasks.map((task) => updateTask(task.guid)));
        }, 1000);

        return () => clearInterval(interval);
    }, []);

    return (
        <TaskContext.Provider value={{ invokeTask, updateTask, removeTask }}>
            {children}
            <ToastContainer position="bottom-end">
                {tasks.map((task) => (
                    <Toast key={task.guid} onClose={() => removeTask(task.guid)}>
                        <Toast.Header>{task.name}</Toast.Header>
                        <Toast.Body>
                            <div className="d-flex align-items-center gap-2">
                                {task.progress?.progress === 100 && (
                                    <BsCheckCircle className="text-success flex-shrink-0" size={20} />
                                )}
                                {task.progress?.progress === -2 && (
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
                                        task.progress?.progress === -2
                                            ? 100 // make the bar "full" in red for error
                                            : task.progress?.progress ?? 0
                                    }
                                    variant={
                                        task.progress?.progress === -2
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

export const useTasks = () => useContext(TaskContext);
