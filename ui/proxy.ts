// Next.js 16 renamed the "middleware" file convention to "proxy".
// Auth.js re-exports its handler here to gate every matched route.
export { auth as proxy } from "@/lib/auth"

export const config = {
  matcher: [
    "/((?!api|_next/static|_next/image|favicon.ico|.*\\.png$).*)",
  ],
}
