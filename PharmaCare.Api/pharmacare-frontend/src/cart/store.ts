import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { ProductResponse, ProductSaleUnit } from '@/types/api'

export interface CartItem { product: ProductResponse; saleUnit: ProductSaleUnit; quantity: number }
interface CartState {
  items: CartItem[]; branchId: string | null; prescriptionId: string | null
  add: (product: ProductResponse, quantity?: number, saleUnit?: ProductSaleUnit) => void
  setQuantity: (productId: string, saleUnitId: string, quantity: number) => void
  remove: (productId: string, saleUnitId: string) => void
  setBranch: (branchId: string | null) => void
  setPrescription: (prescriptionId: string | null) => void
  clear: () => void
  count: () => number
  subtotal: () => number
}

export const useCartStore = create<CartState>()(persist((set, get) => ({
  items: [], branchId: null, prescriptionId: null,
  add: (product, quantity = 1, selectedUnit) => set((state) => {
    const saleUnit = selectedUnit ?? product.saleUnits.find((unit) => unit.isDefault) ?? product.saleUnits[0]
    if (!saleUnit) return state
    const found = state.items.find((item) => item.product.id === product.id && item.saleUnit.id === saleUnit.id)
    return { items: found
      ? state.items.map((item) => item.product.id === product.id && item.saleUnit.id === saleUnit.id ? { product, saleUnit, quantity: item.quantity + quantity } : item)
      : [...state.items, { product, saleUnit, quantity }] }
  }),
  setQuantity: (productId, saleUnitId, quantity) => set((state) => ({ items: quantity <= 0 ? state.items.filter((item) => item.product.id !== productId || item.saleUnit.id !== saleUnitId) : state.items.map((item) => item.product.id === productId && item.saleUnit.id === saleUnitId ? { ...item, quantity } : item) })),
  remove: (productId, saleUnitId) => set((state) => ({ items: state.items.filter((item) => item.product.id !== productId || item.saleUnit.id !== saleUnitId) })),
  setBranch: (branchId) => set({ branchId }),
  setPrescription: (prescriptionId) => set({ prescriptionId }),
  clear: () => set({ items: [], branchId: null, prescriptionId: null }),
  count: () => get().items.reduce((sum, item) => sum + item.quantity, 0),
  subtotal: () => get().items.reduce((sum, item) => sum + item.saleUnit.salePrice * item.quantity, 0),
}), { name: 'pharmacare-cart-v4' }))
