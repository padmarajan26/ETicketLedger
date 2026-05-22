import axios from 'axios'
import type {
  Ticket,
  CheckoutPayload,
  CheckoutResponse,
  TransactionStatus,
  LedgerBalance,
} from '../types'

const api = axios.create({
  baseURL: (import.meta as any).env.VITE_API_URL || 'http://localhost:5000',
  headers: { 'Content-Type': 'application/json' },
})

export const getTickets = (): Promise<Ticket[]> =>
  api.get<Ticket[]>('/api/tickets').then(r => r.data)

export const checkout = (payload: CheckoutPayload): Promise<CheckoutResponse> =>
  api.post<CheckoutResponse>('/api/orders/checkout', payload).then(r => r.data)

export const pollTransaction = (transactionId: string): Promise<TransactionStatus> =>
  api.get<TransactionStatus>(`/api/orders/transactions/${transactionId}`).then(r => r.data)

export const getLedgerBalance = (): Promise<LedgerBalance> =>
  api.get<LedgerBalance>('/api/ledger/balance').then(r => r.data)
