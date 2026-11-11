import { Alert, Badge, Button, Group, Paper, Stack, Table, Text, Title } from '@mantine/core'
import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import useSWR from 'swr'
import { WithGameMonitor } from '@Components/WithGameMonitor'

interface AiDetectionInfo {
  id: string
  challengeId: number
  challengeTitle: string
  teamId: number
  teamName: string
  participationId: number
  modelId: string
  seenAt: string
}

const AiDetection: FC = () => {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const isChinese = i18n.language.startsWith('zh')
  const { data, error } = useSWR<AiDetectionInfo[]>(
    id ? `/api/game/${id}/AiDetections` : null,
    async (url: string) => {
      const response = await fetch(url, { credentials: 'same-origin' })
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return response.json()
    },
    { refreshInterval: 15000 }
  )
  const { data: banned, mutate: refreshBans } = useSWR<number[]>(
    id ? `/api/game/${id}/AiBans` : null,
    async (url: string) => {
      const response = await fetch(url, { credentials: 'same-origin' })
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return response.json()
    },
    { refreshInterval: 15000 }
  )
  const [busy, setBusy] = useState<number | null>(null)
  const [actionError, setActionError] = useState('')

  const updateBan = async (event: AiDetectionInfo, lift: boolean) => {
    const question = lift
      ? `解除 ${event.teamName} 的 Agent 解题封禁？`
      : `确认因使用 Agent 解题封禁 ${event.teamName}？这会暂停该队伍的参赛资格。`
    if (!window.confirm(question)) return
    setBusy(event.participationId)
    setActionError('')
    try {
      const response = await fetch(`/api/game/${id}/AiBan/${event.participationId}`, {
        method: lift ? 'DELETE' : 'POST',
        credentials: 'same-origin',
        headers: lift ? undefined : { 'Content-Type': 'application/json' },
        body: lift ? undefined : JSON.stringify({ detectionId: event.id }),
      })
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      await refreshBans()
    } catch {
      setActionError('更新封禁状态失败')
    } finally {
      setBusy(null)
    }
  }

  return (
    <WithGameMonitor isLoading={!data && !error}>
      <Stack>
        <Group justify="space-between">
          <Title order={2}>{t('game.tab.monitor.ai_detection')}</Title>
          <Badge color="orange">{data?.length ?? 0}</Badge>
        </Group>
        <Alert color="orange">
          {isChinese
            ? '模型标识来自实例请求头，属于疑似 AI 使用线索，不能单独作为违规证据。共享实例只能定位到队伍。'
            : 'Model IDs come from instance request headers. They are review signals, not proof of a rule violation. Shared instances identify teams, not individual players.'}
        </Alert>
        {error && <Alert color="red">{isChinese ? '加载检测记录失败' : 'Could not load detection events'}</Alert>}
        {actionError && <Alert color="red">{actionError}</Alert>}
        <Paper withBorder p="md">
          {!data?.length ? (
            <Text c="dimmed">{isChinese ? '暂无检测记录' : 'No detections yet'}</Text>
          ) : (
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{isChinese ? '时间' : 'Time'}</Table.Th>
                  <Table.Th>{isChinese ? '队伍' : 'Team'}</Table.Th>
                  <Table.Th>{isChinese ? '题目' : 'Challenge'}</Table.Th>
                  <Table.Th>{isChinese ? '自报模型 / Agent' : 'Reported model / agent'}</Table.Th>
                  <Table.Th>{isChinese ? '处置' : 'Action'}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {data.map((event, index) => (
                  <Table.Tr key={`${event.id}-${index}`}>
                    <Table.Td>{dayjs(event.seenAt).format('YYYY-MM-DD HH:mm:ss')}</Table.Td>
                    <Table.Td>{event.teamName} (#{event.teamId})</Table.Td>
                    <Table.Td>{event.challengeTitle} (#{event.challengeId})</Table.Td>
                    <Table.Td><Text ff="monospace" size="sm">{event.modelId}</Text></Table.Td>
                    <Table.Td>
                      <Button size="xs" color={banned?.includes(event.participationId) ? 'gray' : 'red'}
                        loading={busy === event.participationId}
                        onClick={() => updateBan(event, banned?.includes(event.participationId) ?? false)}>
                        {banned?.includes(event.participationId) ? '解除封禁' : '使用 Agent 解题封禁'}
                      </Button>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </Paper>
      </Stack>
    </WithGameMonitor>
  )
}

export default AiDetection
