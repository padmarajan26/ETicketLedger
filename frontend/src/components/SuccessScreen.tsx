import  { useEffect, useState } from 'react'
import { pollTransaction } from '../services/api'
import type { CheckoutResponse } from '../types'
// @ts-ignore: Allow importing CSS side-effect in TSX without type declarations
import '../styles/SuccessScreen.css'

interface Props {
  result: CheckoutResponse
  onBack: () => void
}

interface DetailRow {
  label: string
  value: string
  isId?: boolean
  isStatus?: boolean
}

export default function SuccessScreen({ result, onBack }: Props) {
  const isQR = result.paymentMethod === 'QR'

  const [status, setStatus]   = useState<'Pending' | 'Confirmed'>(result.status)
  const [elapsed, setElapsed] = useState(0)

  useEffect(() => {
    if (!isQR || status === 'Confirmed') return

    const interval = setInterval(async () => {
      setElapsed(e => e + 1)
      try {
        const tx = await pollTransaction(result.transactionId)
        if (tx.status === 'Completed') {
          setStatus('Confirmed')
          clearInterval(interval)
        }
      } catch {
        // ignore transient poll errors
      }
    }, 1500)

    return () => clearInterval(interval)
  }, [isQR, status, result.transactionId])

  const confirmed = status === 'Confirmed'

  const details: DetailRow[] = [
    { label: 'Transaction ID', value: result.transactionId, isId: true },
    { label: 'Order ID',       value: result.orderId,       isId: true },
    { label: 'Amount',         value: `${result.totalAmount?.toLocaleString()} AED` },
    { label: 'Method',         value: result.paymentMethod },
    { label: 'Timestamp',      value: new Date(result.timestamp).toLocaleString() },
    { label: 'Status',         value: status, isStatus: true },
  ]

  const statusMessage = confirmed
    ? 'Your ticket has been booked successfully.'
    : isQR
      ? `Waiting for QR scan confirmation… (${elapsed}s)`
      : 'Processing your payment.'

  return (
    <div className="success-screen">
      <div
        className="success-card"
        style={{ border: `2px solid ${confirmed ? 'var(--color-success)' : 'var(--color-gold)'}` }}
      >
        {/* Icon */}
        <div className="success-card__icon">{confirmed ? '✅' : '⏳'}</div>

        {/* Status heading */}
        <div>
          <h1
            className="success-card__status-title"
            style={{ color: confirmed ? 'var(--color-success)' : 'var(--color-gold)' }}
          >
            {confirmed ? 'Booking Confirmed!' : 'Payment Pending…'}
          </h1>
          <p className="success-card__status-subtitle">{statusMessage}</p>
        </div>

        {/* Detail rows */}
        {details.map(({ label, value, isId, isStatus }) => (
          <div key={label} className="success-card__detail-row">
            <span className="success-card__detail-label">{label}</span>
            <span
              className={[
                'success-card__detail-value',
                isId     ? 'success-card__detail-value--id' : '',
                isStatus ? (confirmed
                  ? 'success-card__detail-value--status-confirmed'
                  : 'success-card__detail-value--status-pending') : '',
              ].filter(Boolean).join(' ')}
            >
              {value}
            </span>
          </div>
        ))}

        {/* Back */}
        <button className="success-card__back-btn" onClick={onBack}>
          ← Back to Tickets
        </button>
      </div>
    </div>
  )
}
