import { useEffect, useState } from 'react'
import { Loader2, Upload, CheckCircle2, XCircle } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useUploadDocumentMutation, useDocumentStatusQuery } from './documentsApi'

export function DocumentsPage() {
  const [file, setFile] = useState<File | null>(null)
  const [docId, setDocId] = useState<string | null>(null)
  const [isDone, setIsDone] = useState(false)
  const [upload, { isLoading }] = useUploadDocumentMutation()

  // Completed/Failed হলে polling থামাও (pollingInterval 0)
  const { data: status } = useDocumentStatusQuery(docId ?? '', {
    skip: !docId,
    pollingInterval: isDone ? 0 : 1500,
  })

  useEffect(() => {
    if (status && (status.status === 'Completed' || status.status === 'Failed')) setIsDone(true)
  }, [status])

  const onUpload = async () => {
    if (!file) return
    const formData = new FormData()
    formData.append('file', file)
    try {
      const res = await upload(formData).unwrap()
      setIsDone(false)
      setDocId(res.documentId)
      toast.success('আপলোড হয়েছে — ingestion চলছে')
    } catch {
      toast.error('আপলোড ব্যর্থ হয়েছে')
    }
  }

  const percent =
    status && status.totalChunks > 0
      ? Math.round((status.processedChunks / status.totalChunks) * 100)
      : 0

  return (
    <div className="mx-auto max-w-2xl space-y-4">
      <h1 className="text-xl font-bold">Documents</h1>
      <p className="text-sm text-muted-foreground">
        .txt / .md আপলোড করুন → background-এ chunk → embed → Qdrant। শেষ হলে RAG-এ (method: qdrant) এই ডকুমেন্ট থেকে উত্তর পাবেন।
      </p>

      <Card>
        <CardContent className="flex flex-wrap items-center gap-3 pt-6">
          <input
            type="file"
            accept=".txt,.md"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            className="text-sm file:mr-3 file:rounded-md file:border-0 file:bg-secondary file:px-3 file:py-1.5 file:text-sm"
          />
          <Button onClick={onUpload} disabled={!file || isLoading} className="gap-2">
            {isLoading ? <Loader2 className="size-4 animate-spin" /> : <Upload className="size-4" />}
            আপলোড
          </Button>
        </CardContent>
      </Card>

      {status && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              {status.status === 'Completed' && <CheckCircle2 className="size-5 text-green-600" />}
              {status.status === 'Failed' && <XCircle className="size-5 text-destructive" />}
              {!isDone && <Loader2 className="size-5 animate-spin text-primary" />}
              {status.fileName} — {status.status}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
              <div
                className="h-full rounded-full bg-primary transition-all"
                style={{ width: `${status.status === 'Completed' ? 100 : percent}%` }}
              />
            </div>
            <p className="text-xs text-muted-foreground">
              {status.processedChunks}/{status.totalChunks} chunk
              {status.error && <span className="text-destructive"> — {status.error}</span>}
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
