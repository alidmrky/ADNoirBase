import { auth } from "@/lib/auth"
import { redirect } from "next/navigation"
import { PreferencesPanel } from "@/components/notifications/preferences-panel"
export const instant = false
export default async function PreferencesPage() {
  const session = await auth()
  if (!session) redirect("/login")
  return <PreferencesPanel token={session.accessToken} />
}
