import { Alert, Badge, Group, Paper, Stack, Table, Text, Title } from '@mantine/core'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import useSWR from 'swr'
import { WithGameMonitor } from '@Components/WithGameMonitor'

interface AiDetectionInfo {
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
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {data.map((event, index) => (
                  <Table.Tr key={`${event.participationId}-${event.challengeId}-${event.modelId}-${index}`}>
                    <Table.Td>{dayjs(event.seenAt).format('YYYY-MM-DD HH:mm:ss')}</Table.Td>
                    <Table.Td>{event.teamName} (#{event.teamId})</Table.Td>
                    <Table.Td>{event.challengeTitle} (#{event.challengeId})</Table.Td>
                    <Table.Td><Text ff="monospace" size="sm">{event.modelId}</Text></Table.Td>
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
