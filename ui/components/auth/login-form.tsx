"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { signIn } from "next-auth/react"
import { z } from "zod"
import { FormField } from "@/components/auth/form-field"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"

const loginSchema = z.object({
  email: z.string().email("Invalid email address"),
  password: z.string().min(1, "Password is required"),
})

type LoginInput = z.infer<typeof loginSchema>
type FieldErrors = Partial<Record<keyof LoginInput, string>>

export function LoginForm() {
  const router = useRouter()
  const [errors, setErrors] = useState<FieldErrors>({})
  const [serverError, setServerError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setServerError(null)

    const formData = new FormData(e.currentTarget)
    const raw = { email: formData.get("email"), password: formData.get("password") }

    const parsed = loginSchema.safeParse(raw)
    if (!parsed.success) {
      const fieldErrors: FieldErrors = {}
      parsed.error.issues.forEach((err) => {
        const key = err.path[0] as keyof LoginInput
        fieldErrors[key] = err.message
      })
      setErrors(fieldErrors)
      return
    }

    setErrors({})
    setLoading(true)

    const result = await signIn("credentials", {
      email: parsed.data.email,
      password: parsed.data.password,
      redirect: false,
    })

    setLoading(false)

    if (result?.error) {
      setServerError("Invalid email or password.")
    } else {
      router.push("/dashboard")
      router.refresh()
    }
  }

  return (
    <Card>
      <CardContent className="pt-6">
        <form onSubmit={handleSubmit} className="space-y-4">
          <FormField
            id="email"
            label="Email"
            type="email"
            placeholder="you@example.com"
            autoComplete="email"
            disabled={loading}
            error={errors.email}
          />

          <FormField
            id="password"
            label="Password"
            type="password"
            placeholder="••••••••"
            autoComplete="current-password"
            disabled={loading}
            error={errors.password}
          />

          {serverError && (
            <p className="text-sm text-destructive">{serverError}</p>
          )}

          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? "Signing in..." : "Sign in"}
          </Button>

          <p className="text-center text-sm text-muted-foreground">
            Don&apos;t have an account?{" "}
            <a href="/register" className="underline hover:text-foreground">
              Register
            </a>
          </p>
        </form>
      </CardContent>
    </Card>
  )
}
