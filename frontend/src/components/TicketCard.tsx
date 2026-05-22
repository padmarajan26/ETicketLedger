import { TIER_COLORS, TIER_ICONS, DEFAULT_COLOR, DEFAULT_ICON } from '../types'
import type { Ticket } from '../types'
// @ts-ignore: Allow importing CSS side-effect in TSX without type declarations
import '../styles/TicketCard.css'

interface Props {
  ticket: Ticket
  onSelect: (ticket: Ticket) => void
}

export default function TicketCard({ ticket, onSelect }: Props) {
  const color   = TIER_COLORS[ticket.name] ?? DEFAULT_COLOR
  const icon    = TIER_ICONS[ticket.name]  ?? DEFAULT_ICON
  const soldOut = ticket.remainingQuota === 0
  const pctLeft = Math.round((ticket.remainingQuota / ticket.totalQuota) * 100)

  return (
    <div
      className={`ticket-card${soldOut ? ' ticket-card--sold-out' : ''}`}
      style={{ border: `2px solid ${soldOut ? 'var(--color-border)' : color}` }}
      onMouseEnter={e => {
        if (!soldOut) {
          (e.currentTarget as HTMLDivElement).style.boxShadow = `0 8px 32px ${color}33`
        }
      }}
      onMouseLeave={e => {
        (e.currentTarget as HTMLDivElement).style.boxShadow = ''
      }}
    >
      {/* Header */}
      <div className="ticket-card__header">
        <span className="ticket-card__icon">{icon}</span>
        <div>
          <h2 className="ticket-card__name" style={{ color }}>{ticket.name}</h2>
          <p className="ticket-card__description">{ticket.description}</p>
        </div>
      </div>

      {/* Price */}
      <div className="ticket-card__price" style={{ color }}>
        {ticket.price.toLocaleString()}{' '}
        <span className="ticket-card__currency">AED</span>
      </div>

      {/* Quota bar */}
      <div>
        <div className="ticket-card__quota-row">
          <span>Availability</span>
          <span className={`ticket-card__quota-count${soldOut ? ' ticket-card__quota-count--sold-out' : ''}`}>
            {soldOut ? 'Sold Out' : `${ticket.remainingQuota} / ${ticket.totalQuota}`}
          </span>
        </div>
        <div className="ticket-card__bar-track">
          <div
            className="ticket-card__bar-fill"
            style={{
              width: `${pctLeft}%`,
              background: pctLeft > 30 ? color : 'var(--color-danger)',
            }}
          />
        </div>
      </div>

      {/* CTA */}
      <button
        className={`ticket-card__btn ${soldOut ? 'ticket-card__btn--disabled' : 'ticket-card__btn--active'}`}
        style={soldOut ? undefined : { background: color }}
        onClick={() => { if (!soldOut) onSelect(ticket) }}
        disabled={soldOut}
      >
        {soldOut ? 'Sold Out' : 'Buy Now'}
      </button>
    </div>
  )
}
