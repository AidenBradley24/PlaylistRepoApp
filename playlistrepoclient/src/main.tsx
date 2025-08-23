import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { ThemeProvider } from 'react-bootstrap'

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <ThemeProvider dir="rtl">
            <App />
        </ThemeProvider>
    </StrictMode>,
)
