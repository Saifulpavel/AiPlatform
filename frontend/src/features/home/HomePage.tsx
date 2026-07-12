import { Link } from 'react-router-dom'
import { BarChart3, FileUp, MessageSquare, Search } from 'lucide-react'
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useMeQuery } from '@/features/auth/authApi'

const features = [
  { to: '/chat', icon: MessageSquare, title: 'Chat', desc: 'মাল্টি-প্রোভাইডার AI চ্যাট (streaming)' },
  { to: '/rag', icon: Search, title: 'RAG', desc: 'নিজের ডকুমেন্ট থেকে grounded উত্তর' },
  { to: '/documents', icon: FileUp, title: 'Documents', desc: 'ডকুমেন্ট আপলোড ও ingestion' },
  { to: '/dashboard', icon: BarChart3, title: 'Dashboard', desc: 'LLM খরচ/টোকেন + RAG evals' },
]

export function HomePage() {
  const { data: user } = useMeQuery()
  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold">স্বাগতম{user ? `, ${user.fullName}` : ''}</h1>
        <p className="text-muted-foreground">কোথায় যেতে চান?</p>
      </div>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {features.map((f) => (
          <Link key={f.to} to={f.to}>
            <Card className="h-full gap-2 transition hover:border-primary hover:shadow-md">
              <CardHeader>
                <f.icon className="mb-1 size-6 text-primary" />
                <CardTitle className="text-lg">{f.title}</CardTitle>
                <CardDescription>{f.desc}</CardDescription>
              </CardHeader>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  )
}
