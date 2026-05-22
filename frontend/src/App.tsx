import { useEffect, useState } from 'react'
import { getTickets } from './services/api'
import TicketCard from './components/TicketCard'
import CheckoutModal from './components/CheckoutModal'
import SuccessScreen from './components/SuccessScreen'
import type { Ticket, CheckoutResponse } from './types'
// @ts-ignore: Allow importing CSS side-effect in TSX without type declarations
import './styles/App.css'

export default function App() {
  const [tickets, setTickets]   = useState<Ticket[]>([])
  const [loading, setLoading]   = useState(true)
  const [error, setError]       = useState<string | null>(null)
  const [selected, setSelected] = useState<Ticket | null>(null)
  const [result, setResult]     = useState<CheckoutResponse | null>(null)

  const fetchTickets = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await getTickets()
      setTickets(data)
    } catch {
      setError('Could not load tickets. Is the API running?')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void fetchTickets() }, [])

  const handleSuccess = (res: CheckoutResponse) => {
    setSelected(null)
    setResult(res)
    void fetchTickets()
  }

  const handleBack = () => {
    setResult(null)
    void fetchTickets()
  }

  if (result) {
    return <SuccessScreen result={result} onBack={handleBack} />
  }

  return (
    <div>
      {/* Header */}
      <header className="app-header">
        <div className="app-header__brand">
          <span className="app-header__logo">🎫</span>
          <div>
            <h1 className="app-header__title">ETicketLedger</h1>
            <p className="app-header__subtitle">E-Ticketing &amp; Payment Platform</p>
          </div>
        </div>
        <button className="app-header__refresh" onClick={() => void fetchTickets()}>
          ↻ Refresh
        </button>
      </header>

      {/* Main */}
      <main className="app-main">
        <div className="app-hero">
          <h2 className="app-hero__title">Choose Your Experience</h2>
          <p className="app-hero__subtitle">Select a tier and complete your purchase in seconds.</p>
        </div>

        {loading && <div className="app-loading">Loading tickets…</div>}

        {error && <div className="app-error">{error}</div>}

        {!loading && !error && (
          <div className="ticket-grid">
            {tickets.map(t => (
              <TicketCard key={t.id} ticket={t} onSelect={setSelected} />
            ))}
          </div>
        )}
      </main>

      {/* Checkout modal */}
      {selected && (
        <CheckoutModal
          ticket={selected}
          onSuccess={handleSuccess}
          onClose={() => setSelected(null)}
        />
      )}
    </div>
  )
}
