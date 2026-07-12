import { useState } from 'react'
import { CheckCircle2, Loader2, PlayCircle, XCircle } from 'lucide-react'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
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
import { useRunEvalsMutation, useUsageSummaryQuery } from './dashboardApi'

const PROVIDERS = ['groq', 'gemini', 'mistral']

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{label}</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-2xl font-bold">{value}</p>
      </CardContent>
    </Card>
  )
}

export function DashboardPage() {
  const { data: usage, isLoading: usageLoading } = useUsageSummaryQuery()
  const [provider, setProvider] = useState(useAppSelector(selectDefaultProvider))
  const [runEvals, { data: report, isLoading: evalsLoading }] = useRunEvalsMutation()

  const onRunEvals = async () => {
    try {
      await runEvals(provider).unwrap()
    } catch {
      toast.error('Evals চালানো যায়নি')
    }
  }

  return (
    <div className="mx-auto max-w-5xl space-y-8">
      <h1 className="text-2xl font-bold">Dashboard</h1>

      {/* --- LLM usage / cost --- */}
      <section className="space-y-3">
        <h2 className="text-lg font-semibold">LLM ব্যবহার ও খরচ</h2>
        {usageLoading && <Loader2 className="size-5 animate-spin text-muted-foreground" />}
        {usage && (
          <>
            <div className="grid gap-4 sm:grid-cols-4">
              <Stat label="মোট খরচ" value={`$${usage.totalCostUsd.toFixed(4)}`} />
              <Stat label="মোট request" value={usage.totalRequests.toLocaleString()} />
              <Stat label="Input tokens" value={usage.totalInputTokens.toLocaleString()} />
              <Stat label="Output tokens" value={usage.totalOutputTokens.toLocaleString()} />
            </div>

            <div className="overflow-x-auto rounded-2xl border bg-card">
              <table className="w-full text-sm">
                <thead className="border-b text-left text-muted-foreground">
                  <tr>
                    <th className="px-4 py-2 font-medium">Provider</th>
                    <th className="px-4 py-2 font-medium">Model</th>
                    <th className="px-4 py-2 text-right font-medium">Requests</th>
                    <th className="px-4 py-2 text-right font-medium">Input</th>
                    <th className="px-4 py-2 text-right font-medium">Output</th>
                    <th className="px-4 py-2 text-right font-medium">Cost</th>
                  </tr>
                </thead>
                <tbody>
                  {usage.byModel.length === 0 && (
                    <tr>
                      <td colSpan={6} className="px-4 py-6 text-center text-muted-foreground">
                        এখনো কোনো LLM call হয়নি।
                      </td>
                    </tr>
                  )}
                  {usage.byModel.map((m) => (
                    <tr key={`${m.provider}-${m.model}`} className="border-b last:border-0">
                      <td className="px-4 py-2">{m.provider}</td>
                      <td className="px-4 py-2">{m.model}</td>
                      <td className="px-4 py-2 text-right">{m.requests.toLocaleString()}</td>
                      <td className="px-4 py-2 text-right">{m.inputTokens.toLocaleString()}</td>
                      <td className="px-4 py-2 text-right">{m.outputTokens.toLocaleString()}</td>
                      <td className="px-4 py-2 text-right">${m.costUsd.toFixed(4)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </section>

      {/* --- Evals --- */}
      <section className="space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h2 className="text-lg font-semibold">RAG Evals</h2>
          <div className="flex items-center gap-2">
            <Select value={provider} onValueChange={setProvider}>
              <SelectTrigger className="w-32">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {PROVIDERS.map((p) => (
                  <SelectItem key={p} value={p}>
                    {p}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button onClick={onRunEvals} disabled={evalsLoading} className="gap-2">
              {evalsLoading ? <Loader2 className="size-4 animate-spin" /> : <PlayCircle className="size-4" />}
              Run evals
            </Button>
          </div>
        </div>

        {report && (
          <div className="space-y-3">
            <p className="text-sm">
              Pass rate:{' '}
              <span className="font-bold">{Math.round(report.passRate * 100)}%</span>{' '}
              <span className="text-muted-foreground">
                ({report.passed}/{report.total})
              </span>
            </p>
            <div className="space-y-2">
              {report.results.map((r) => (
                <div key={r.id} className="flex items-start gap-2 rounded-xl border bg-card p-3 text-sm">
                  {r.passed ? (
                    <CheckCircle2 className="mt-0.5 size-4 shrink-0 text-green-600" />
                  ) : (
                    <XCircle className="mt-0.5 size-4 shrink-0 text-destructive" />
                  )}
                  <div className="min-w-0">
                    <p className="font-medium">{r.id}</p>
                    <p className={cn('truncate text-muted-foreground', !r.passed && 'text-destructive')}>
                      {r.passed ? r.answer : r.failReason}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </section>
    </div>
  )
}
