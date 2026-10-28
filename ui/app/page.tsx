import { redirect } from "next/navigation"
import { auth } from "@/lib/auth"

// The landing route only decides where to send the visitor, and that decision
// reads the session cookie. There is nothing to prerender, so let it block
// rather than asking Next.js for a static shell it can never produce.
export const instant = false

export default async function HomePage() {
  const session = await auth()

  if (session) {
    redirect("/dashboard")
  } else {
    redirect("/login")
  }
}
