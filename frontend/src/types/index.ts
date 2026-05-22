// ── Ticket ────────────────────────────────────────────────────────────────────
export interface Ticket {
  id: number
  name: string
  description: string
  price: number
  totalQuota: number
  remainingQuota: number
  isActive: boolean
}

// ── Checkout ──────────────────────────────────────────────────────────────────
export interface CheckoutPayload {
  ticketId: number
  quantity: number
  customerName: string
  customerEmail: string
  paymentMethod: PaymentMethod
}

export type PaymentMethod = 'CreditCard' | 'QR'

export interface CheckoutResponse {
  orderId: string
  transactionId: string
  status: 'Confirmed' | 'Pending'
  totalAmount: number
  paymentMethod: PaymentMethod
  timestamp: string
  message: string
}

// ── Transaction ───────────────────────────────────────────────────────────────
export interface TransactionStatus {
  transactionId: string
  orderId: string
  status: 'Pending' | 'Completed' | 'Failed'
  amount: number
  paymentMethod: PaymentMethod
  createdAt: string
  completedAt: string | null
}

// ── Ledger ────────────────────────────────────────────────────────────────────
export interface LedgerBalance {
  totalDebits: number
  totalCredits: number
  isBalanced: boolean
}

// ── Tier helpers ──────────────────────────────────────────────────────────────
export type TierName = 'Gold' | 'Premium' | 'VIP'

export const TIER_COLORS: Record<string, string> = {
  Gold:    '#f59e0b',
  Premium: '#6366f1',
  VIP:     '#ec4899',
}

export const TIER_ICONS: Record<string, string> = {
  Gold:    '🥇',
  Premium: '💎',
  VIP:     '👑',
}

export const DEFAULT_COLOR = '#64748b'
export const DEFAULT_ICON  = '🎫'
