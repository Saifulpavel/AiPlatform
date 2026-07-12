import { useState } from 'react'
import { Loader2, Search } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useAppSelector } from '@/app/hooks'
import { selectDefaultProvider } from '@/features/settings/uiSlice'
import { useRagMutation } from './ragApi'

const METHODS = ['keyword', 'embedding', 'qdrant', 'hybrid']
const PROVIDERS = ['groq', 'gemini', 'mistral']

export function RagPage() {
  const [question, setQuestion] = useState('')
  const [method, setMethod] = useState('embedding')
  const [provider, setProvider] = useState(useAppSelector(selectDefaultProvider))
  const [rag, { data, isLoading }] = useRagMutation()

  const ask = async () => {
    const q = question.trim()
    if (!q) return
    try {
      await rag({ question: q, method, provider }).unwrap()
    } catch {
      toast.error('উত্তর আনা যায়নি')
    }
  }

  return (
    <div className="mx-auto max-w-3xl space-y-4">
      <h1 className="text-xl font-bold">RAG</h1>

      <div className="flex flex-wrap gap-2">
        <Select value={method} onValueChange={setMethod}>
          <SelectTrigger className="w-40"><SelectValue /></SelectTrigger>
          <SelectContent>
            {METHODS.map((m) => <SelectItem key={m} value={m}>{m}</SelectItem>)}
          </SelectContent>
        </Select>
        <Select value={provider} onValueChange={setProvider}>
          <SelectTrigger className="w-36"><SelectValue /></SelectTrigger>
          <SelectContent>
            {PROVIDERS.map((p) => <SelectItem key={p} value={p}>{p}</SelectItem>)}
          </SelectContent>
        </Select>
      </div>

      <Textarea
        value={question}
        onChange={(e) => setQuestion(e.target.value)}
        placeholder="আপনার প্রশ্ন লিখুন... (knowledge base + আপলোড করা ডকুমেন্ট থেকে উত্তর)"
        rows={3}
      />
      <Button onClick={ask} disabled={isLoading || !question.trim()} className="gap-2">
        {isLoading ? <Loader2 className="size-4 animate-spin" /> : <Search className="size-4" />}
        জিজ্ঞাসা করুন
      </Button>

      {data && (
        <div className="space-y-4">
          <Card>
            <CardHeader><CardTitle className="text-base">উত্তর</CardTitle></CardHeader>
            <CardContent className="whitespace-pre-wrap text-sm">{data.answer}</CardContent>
          </Card>

          {data.sources.length > 0 && (
            <div className="space-y-2">
              <p className="text-sm font-medium text-muted-foreground">সূত্র ({data.sources.length})</p>
              {data.sources.map((s, i) => (
                <div key={i} className="rounded-lg border bg-muted/40 p-3 text-sm">
                  <div className="mb-1 flex items-center gap-2 text-xs text-muted-foreground">
                    <span className="rounded bg-primary/10 px-1.5 py-0.5 font-medium text-primary">[{i + 1}]</span>
                    {s.source && <span className="rounded bg-secondary px-1.5 py-0.5">{s.source}</span>}
                    {s.score != null && <span>score: {s.score.toFixed(3)}</span>}
                  </div>
                  {s.text}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
