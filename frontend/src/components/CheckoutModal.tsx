import React, { useState } from 'react'
import { checkout } from '../services/api'
import { TIER_COLORS, DEFAULT_COLOR } from '../types'
import type { Ticket, PaymentMethod, CheckoutResponse } from '../types'
import '../styles/CheckoutModal.css'

interface Props {
  ticket: Ticket
  onSuccess: (result: CheckoutResponse) => void
  onClose: () => void
}

interface FormState {
  customerName: string
  customerEmail: string
  quantity: number
  paymentMethod: PaymentMethod
}

interface PaymentOption {
  value: PaymentMethod
  label: string
  sub: string
}

const PAYMENT_OPTIONS: PaymentOption[] = [
  { value: 'CreditCard', label: '💳 Credit Card', sub: 'Instant' },
  { value: 'QR',         label: '📱 QR Code',     sub: '~8s delay' },
]

const QUANTITIES = [1, 2, 3, 4, 5] as const

export default function CheckoutModal({ ticket, onSuccess, onClose }: Props) {
  const color = TIER_COLORS[ticket.name] ?? DEFAULT_COLOR

  const [form, setForm] = useState<FormState>({
    customerName:  '',
    customerEmail: '',
    quantity:      1,
    paymentMethod: 'CreditCard',
  })
  const [loading, setLoading] = useState(false)
  const [error, setError]     = useState<string | null>(null)

  const total = ticket.price * form.quantity

  const setField = <K extends keyof FormState>(field: K, value: FormState[K]) =>
    setForm(prev => ({ ...prev, [field]: value }))

  const handleSubmit = async () => {
    setError(null)
    if (!form.customerName.trim() || !form.customerEmail.trim()) {
      setError('Please fill in all fields.')
      return
    }
    setLoading(true)
    try {
      const res = await checkout({
        ticketId:      ticket.id,
        quantity:      form.quantity,
        customerName:  form.customerName,
        customerEmail: form.customerEmail,
        paymentMethod: form.paymentMethod,
      })
      onSuccess(res)
    } catch (err: unknown) {
      const axiosError = err as { response?: { data?: { error?: string; 0?: string } } }
      const msg =
        axiosError.response?.data?.error ??
        axiosError.response?.data?.[0] ??
        'Checkout failed. Please try again.'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  const handleOverlayClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (e.target === e.currentTarget) onClose()
  }

  return (
    <div className="modal-overlay" onClick={handleOverlayClick}>
      <div className="modal" style={{ border: `2px solid ${color}` }}>

        {/* Header */}
        <div className="modal__header">
          <h2 className="modal__title" style={{ color }}>
            Checkout — {ticket.name}
          </h2>
          <button className="modal__close" onClick={onClose} aria-label="Close">✕</button>
        </div>

        {/* Name */}
        <div className="modal__field">
          <label className="modal__label">Full Name</label>
          <input
            className="modal__input"
            type="text"
            placeholder="Jane Smith"
            value={form.customerName}
            onChange={e => setField('customerName', e.target.value)}
          />
        </div>

        {/* Email */}
        <div className="modal__field">
          <label className="modal__label">Email</label>
          <input
            className="modal__input"
            type="email"
            placeholder="jane@example.com"
            value={form.customerEmail}
            onChange={e => setField('customerEmail', e.target.value)}
          />
        </div>

        {/* Quantity */}
        <div className="modal__field">
          <label className="modal__label">Quantity</label>
          <div className="modal__qty-row">
            {QUANTITIES.map(n => (
              <button
                key={n}
                className="modal__qty-btn"
                style={{
                  border:     `1.5px solid ${form.quantity === n ? color : 'var(--color-border)'}`,
                  background: form.quantity === n ? color : 'var(--color-bg)',
                  color:      form.quantity === n ? '#0f172a' : 'var(--color-text)',
                }}
                onClick={() => setField('quantity', n)}
              >
                {n}
              </button>
            ))}
          </div>
        </div>

        {/* Payment method */}
        <div className="modal__field">
          <label className="modal__label">Payment Method</label>
          <div className="modal__payment-row">
            {PAYMENT_OPTIONS.map(({ value, label, sub }) => (
              <button
                key={value}
                className="modal__payment-btn"
                style={{
                  border:     `1.5px solid ${form.paymentMethod === value ? color : 'var(--color-border)'}`,
                  background: form.paymentMethod === value ? `${color}18` : 'var(--color-bg)',
                }}
                onClick={() => setField('paymentMethod', value)}
              >
                <div className="modal__payment-btn-label">{label}</div>
                <div className="modal__payment-btn-sub">{sub}</div>
              </button>
            ))}
          </div>
        </div>

        {/* Total */}
        <div className="modal__total">
          <span className="modal__total-label">Total</span>
          <span className="modal__total-amount" style={{ color }}>
            {total.toLocaleString()} AED
          </span>
        </div>

        {/* Error */}
        {error && <div className="modal__error">{error}</div>}

        {/* Submit */}
        <button
          className={`modal__submit ${loading ? 'modal__submit--loading' : 'modal__submit--active'}`}
          style={loading ? undefined : { background: color }}
          onClick={handleSubmit}
          disabled={loading}
        >
          {loading ? 'Processing…' : `Pay ${total.toLocaleString()} AED`}
        </button>
      </div>
    </div>
  )
}
