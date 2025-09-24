import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { ThemeProvider } from 'react-bootstrap'
import { RefreshProvider } from "./components/RefreshContext";
import { EditProvider } from "./components/EditContext"
import { TaskProvider } from "./components/TaskContext"
import { BrowserRouter } from "react-router-dom";

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <BrowserRouter>
            <RefreshProvider>
                <TaskProvider>
                    <EditProvider>
                        <ThemeProvider dir="rtl">
                            <App />
                        </ThemeProvider>
                    </EditProvider>
                </TaskProvider>
            </RefreshProvider>
        </BrowserRouter>
    </StrictMode>,
)
