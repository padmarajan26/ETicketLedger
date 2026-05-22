import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
// @ts-ignore: Allow importing CSS side-effect in TSX without type declarations
import './index.css'

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
)
