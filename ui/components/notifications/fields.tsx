"use client"
import type { ReactNode, SelectHTMLAttributes } from "react"
import { Button } from "@/components/ui/button"

export function Field({ label, children }: { label: string; children: ReactNode }) {
  return <label className="grid gap-1.5 text-sm font-medium">{label}{children}</label>
}
export function Choice(props: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select {...props} className={"h-9 w-full rounded-md border bg-background px-3 text-sm " + (props.className ?? "")} />
}
export function Feedback({ error, message }: { error?: string; message?: string }) {
  return <>{error && <p role="alert" className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</p>}{message && <p role="status" className="rounded-md bg-muted p-3 text-sm">{message}</p>}</>
}
export function Pagination({ page, totalPages, onPage }: { page: number; totalPages: number; onPage: (page: number) => void }) {
  return <div className="flex items-center justify-end gap-3 pt-3 text-sm">
    <Button variant="outline" disabled={page <= 1} onClick={() => onPage(page - 1)}>Önceki</Button>
    <span>{page} / {Math.max(1, totalPages)}</span>
    <Button variant="outline" disabled={page >= totalPages} onClick={() => onPage(page + 1)}>Sonraki</Button>
  </div>
}

