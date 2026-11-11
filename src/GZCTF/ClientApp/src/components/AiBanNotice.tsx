import { Button, Center, FileInput, Modal, Select, Stack, Text, Title } from '@mantine/core'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import useSWR from 'swr'
import { useUser } from '@Hooks/useUser'

interface ActiveAiBan {
  gameId: number
  gameTitle: string
  appealSubmittedAt?: string | null
}

export const AiBanNotice = () => {
  const { i18n } = useTranslation()
  const zh = i18n.language.startsWith('zh')
  const { user } = useUser()
  const { data, mutate } = useSWR<ActiveAiBan[]>(
    user ? '/api/game/AiBans' : null,
    async (url: string) => {
      const response = await fetch(url, { credentials: 'same-origin' })
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return response.json()
    },
    { refreshInterval: 5000, revalidateOnFocus: true }
  )
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [message, setMessage] = useState('')
  const [selectedGame, setSelectedGame] = useState<string | null>(null)
  const ban = data?.find((item) => String(item.gameId) === selectedGame) ?? data?.[0]

  const submit = async () => {
    if (!ban || !file) return
    setUploading(true)
    setMessage('')
    try {
      const form = new FormData()
      form.append('file', file)
      const response = await fetch(`/api/game/${ban.gameId}/AiAppeal`, {
        method: 'POST',
        credentials: 'same-origin',
        body: form,
      })
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      setFile(null)
      setMessage(zh ? '申诉材料已提交' : 'Appeal submitted')
      await mutate()
    } catch {
      setMessage(zh ? '提交失败，请检查 PDF 格式和文件大小后重试' : 'Upload failed. Check the PDF and try again.')
    } finally {
      setUploading(false)
    }
  }

  return (
    <Modal opened={!!ban} onClose={() => {}} fullScreen withCloseButton={false} closeOnEscape={false}
      closeOnClickOutside={false} zIndex={10000}>
      {ban && (
        <Center mih="85vh">
          <Stack maw={640} w="100%" align="center" gap="lg">
            <Title order={1} c="red" ta="center">{zh ? '使用 Agent 解题，已被封禁' : 'Banned for using an Agent to solve challenges'}</Title>
            <Text ta="center">{zh ? `${ban.gameTitle} 的参赛资格已暂停。申诉材料会提交给比赛管理员审核。` :
              `Your participation in ${ban.gameTitle} is suspended. The administrators will review your appeal.`}</Text>
            {(data?.length ?? 0) > 1 && <Select w="100%" label={zh ? '选择比赛' : 'Select game'}
              value={String(ban.gameId)} onChange={setSelectedGame}
              data={data?.map((item) => ({ value: String(item.gameId), label: item.gameTitle })) ?? []} />}
            <FileInput w="100%" label={zh ? '申诉材料（PDF，最多 20 MiB）' : 'Appeal (PDF, up to 20 MiB)'} accept="application/pdf,.pdf"
              value={file} onChange={setFile} clearable />
            <Button color="red" disabled={!file || file.size > 20 * 1024 * 1024} loading={uploading}
              onClick={submit}>{zh ? '提交申诉' : 'Submit appeal'}</Button>
            {ban.appealSubmittedAt && <Text c="dimmed">{zh ? '已收到申诉材料，可以重新提交' : 'Appeal received. You may replace it.'}</Text>}
            {message && <Text role="status">{message}</Text>}
          </Stack>
        </Center>
      )}
    </Modal>
  )
}
