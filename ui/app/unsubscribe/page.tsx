import { Suspense } from "react"
import { UnsubscribeForm } from "@/components/notifications/unsubscribe-form"
export default function UnsubscribePage() {
  return <Suspense fallback={<p>Yükleniyor…</p>}><UnsubscribeForm /></Suspense>
}
