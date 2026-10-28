import type { Metadata } from "next"
import { redirect } from "next/navigation"
import { auth } from "@/lib/auth"
import { RolesPanel } from "@/components/roles/roles-panel"

export const instant = false
export const metadata: Metadata = { title: "Roller" }
export default async function RolesPage() {
  const session = await auth()
  if (!session) redirect("/login")
  if (!session.user.roles.includes("ADMIN")) redirect("/dashboard")
  return <RolesPanel token={session.accessToken} />
}
