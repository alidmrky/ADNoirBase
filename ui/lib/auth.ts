import NextAuth from "next-auth"
import Credentials from "next-auth/providers/credentials"
import { z } from "zod"
import { apiClient } from "@/lib/api-client"

const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
})

interface LoginResponse {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
  userId: string
  email: string
  fullName: string
  roles: string[]
}

export const { handlers, auth, signIn, signOut } = NextAuth({
  providers: [
    Credentials({
      credentials: {
        email: { label: "Email", type: "email" },
        password: { label: "Password", type: "password" },
      },
      async authorize(credentials) {
        const parsed = loginSchema.safeParse(credentials)
        if (!parsed.success) return null

        try {
          const result = await apiClient.post<LoginResponse>(
            "/api/auth/login",
            parsed.data
          )

          const data = result.data
          return {
            id: data.userId,
            email: data.email,
            name: data.fullName,
            accessToken: data.accessToken,
            refreshToken: data.refreshToken,
            roles: data.roles,
            accessTokenExpiresAt: data.accessTokenExpiresAt,
          }
        } catch {
          return null
        }
      },
    }),
  ],
  callbacks: {
    async jwt({ token, user }) {
      // İlk login'de user gelir, sonraki çağrılarda token üzerinden devam eder
      if (user) {
        token.accessToken = (user as Record<string, unknown>).accessToken as string
        token.refreshToken = (user as Record<string, unknown>).refreshToken as string
        token.roles = (user as Record<string, unknown>).roles as string[]
        token.accessTokenExpiresAt = (user as Record<string, unknown>).accessTokenExpiresAt as string
      }
      return token
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken as string
      session.user.roles = token.roles as string[]
      return session
    },
  },
  pages: {
    signIn: "/login",
    error: "/login",
  },
  session: {
    strategy: "jwt",
  },
})
